using System;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddServerComponents()
    .AddWebAssemblyComponents();

// Adds a default in-memory implementation of IDistributedCache.
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
});

builder.Services.AddPhp(options =>
{
    //
});

var app = builder.Build();

app.UseBlazorWebJS();

app.UseBlazorWebAssemblyRenderMode();

//// sample usage of URL rewrite:
//var options = new RewriteOptions()
//    .AddRewrite(@"^rule/(\w+)", "index.php?word=$1", skipRemainingRules: true);

//app.UseRewriter(options);

// enable session:
app.UseSession();

// enable .php files from compiled assembly:
var contentPath = ResolveContentPath();

app.UsePhp("/", rootPath: contentPath);
app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(contentPath) });

//
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapBlazorHub();

app.Run();

/// <summary>
/// Gets location of website project content.
/// In development, we use the original website project location.
/// Otherwise, content files are published to the current working directory.
/// </summary>
/// <returns></returns>
static string ResolveContentPath()
{
    var devcontent = Path.GetFullPath("../Website");
    if (Directory.Exists(devcontent))
    {
        return devcontent;
    }

    return Directory.GetCurrentDirectory();
}