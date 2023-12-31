﻿using System.Diagnostics.CodeAnalysis;
using System.Buffers;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http.HttpResults;
//using Microsoft.AspNetCore.Components.Endpoints.Rendering;
using Pchp.Core;
using Peachpie.AspNetCore.Web;

namespace Peachpie.AspNetCore
{
    /// <summary>
    /// Extension methods to <see cref="HttpContext"/>.
    /// </summary>
    public static class HttpContextExtension
    {
        public static string Component<TComponent>(this Context phpContext,PhpArray parameters = null)
            where TComponent : Microsoft.AspNetCore.Components.IComponent
        {
            var output = new StringWriter();
            RenderComponentAsync<TComponent>(phpContext, output, parameters).Wait();
            return output.GetStringBuilder().ToString();
        }

        public static async Task RenderComponentAsync<TComponent>(this Context phpContext, TextWriter textWriter, PhpArray parameters = null) 
            where TComponent : Microsoft.AspNetCore.Components.IComponent
        {
            var httpContext = phpContext.GetHttpContext() ?? throw new ArgumentException(nameof(phpContext));

            var serviceProvider = httpContext.RequestServices;

            ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            var componentType = typeof(TComponent);
            var componentParameters = ((IDictionary<IntStringKey, PhpValue>)parameters).ToDictionary(x => x.Key.String, x => x.Value.ToClr());

            await using var htmlRenderer = new HtmlRenderer(serviceProvider, loggerFactory);

            /*

            await htmlRenderer.Dispatcher.InvokeAsync(async () =>
            {
                var @params = ParameterView.FromDictionary(componentParameters);
                var output = await htmlRenderer.RenderComponentAsync<TComponent>(@params);

                output.WriteHtmlTo(textWriter);
            });

            */

            bool preventStreamingRendering = false;

            var ehrType = Type.GetType("Microsoft.AspNetCore.Components.Endpoints.EndpointHtmlRenderer, Microsoft.AspNetCore.Components.Endpoints");

            var initializeStreamingRenderingFraming = ehrType.GetMethod("InitializeStreamingRenderingFraming");
            //var markAsAllowingEnhancedNavigation = ehrType.GetMethod("MarkAsAllowingEnhancedNavigation", BindingFlags.Public | BindingFlags.Static);

            var prerenderComponentAsync = ehrType.GetMethod("PrerenderComponentAsync", new Type [] {
                typeof(HttpContext),
                typeof(Type),
                typeof(IComponentRenderMode),
                typeof(ParameterView),
                typeof(bool)
            });

            var sendStreamingUpdatesAsync = ehrType.GetMethod("SendStreamingUpdatesAsync");

            var endpointHtmlRenderer = (Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure.StaticHtmlRenderer)httpContext.RequestServices.GetRequiredService(ehrType);

            await endpointHtmlRenderer.Dispatcher.InvokeAsync(async () =>
            {
                var isErrorHandler = httpContext.Features.Get<IExceptionHandlerFeature>() is not null;

                initializeStreamingRenderingFraming.Invoke(endpointHtmlRenderer, new object[] { httpContext, /* isErrorHandler */ });
                //endpointHtmlRenderer.InitializeStreamingRenderingFraming(httpContext, isErrorHandler);

                //markAsAllowingEnhancedNavigation.Invoke(null, new object[] { httpContext });
                //EndpointHtmlRenderer.MarkAsAllowingEnhancedNavigation(httpContext);

                // We could pool these dictionary instances if we wanted, and possibly even the ParameterView
                // backing buffers could come from a pool like they do during rendering.
                var hostParameters = ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    { "ComponentType" /* nameof(RazorComponentEndpointHost.ComponentType) */, componentType },
                    { "ComponentParameters" /* nameof(RazorComponentEndpointHost.ComponentParameters) */, componentParameters },
                });

                // Matches MVC's MemoryPoolHttpResponseStreamWriterFactory.DefaultBufferSize
                var defaultBufferSize = 16 * 1024;
                await using var writer = new HttpResponseStreamWriter(httpContext.Response.Body, Encoding.UTF8, defaultBufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared);
                //using var bufferWriter = new BufferWriter(writer);

                // Note that we don't set any interactive rendering mode for the top-level output from a RazorComponentResult,
                // because you never want to serialize the invocation of RazorComponentResultHost. Instead, that host
                // component takes care of switching into your desired render mode when it produces its own output.
                
                /*
                var c = await endpointHtmlRenderer.PrerenderComponentAsync(
                    httpContext,
                    Type.GetType("Microsoft.AspNetCore.Components.Endpoints.RazorComponentEndpointHost, Microsoft.AspNetCore.Components.Endpoints"), /* typeof(RazorComponentEndpointHost),
                    null,
                    hostParameters,
                    waitForQuiescence: preventStreamingRendering)
                */

                var c = (ValueTask<Microsoft.AspNetCore.Html.IHtmlAsyncContent>)prerenderComponentAsync.Invoke(endpointHtmlRenderer, new object [] {
                    httpContext,
                    Type.GetType("Microsoft.AspNetCore.Components.Endpoints.RazorComponentEndpointHost, Microsoft.AspNetCore.Components.Endpoints"), /* typeof(RazorComponentEndpointHost), */
                    null,
                    hostParameters,
                    preventStreamingRendering
                });
                    
                var htmlContent = await c;
                //var htmlContent = (EndpointHtmlRenderer.PrerenderedComponentHtmlContent)(await c);

                Type pchc = Type.GetType("Microsoft.AspNetCore.Components.Endpoints.EndpointHtmlRenderer.PrerenderedComponentHtmlContent, Microsoft.AspNetCore.Components.Endpoints");

                // Importantly, we must not yield this thread (which holds exclusive access to the renderer sync context)
                // in between the first call to htmlContent.WriteTo and the point where we start listening for subsequent
                // streaming SSR batches (inside SendStreamingUpdatesAsync). Otherwise some other code might dispatch to the
                // renderer sync context and cause a batch that would get missed.
                htmlContent.WriteTo(writer, HtmlEncoder.Default); // Don't use WriteToAsync, as per the comment above

                /*
                if (!htmlContent.QuiescenceTask.IsCompletedSuccessfully)
                {
                    //await endpointHtmlRenderer.SendStreamingUpdatesAsync(httpContext, htmlContent.QuiescenceTask, writer);

                    await (Task)sendStreamingUpdatesAsync.Invoke(endpointHtmlRenderer, new object [] {
                        httpContext, htmlContent.QuiescenceTask, writer
                    });
                }
                */

                // Invoke FlushAsync to ensure any buffered content is asynchronously written to the underlying
                // response asynchronously. In the absence of this line, the buffer gets synchronously written to the
                // response as part of the Dispose which has a perf impact.
                await writer.FlushAsync();
            });
        }
    }
}