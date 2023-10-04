## Blazor Components in PHP (PeachPie)

This project is an experiment to implement Blazor support in PHP on Peachpie.

Currently, you can only render static Razor components. Read section below.

## Background

Rendering Razor View partials in PHP is already supported by Peachpie. This extends the support to the more modern Razor component model - and possibly hosting interactive components.

Peachpie is a PHP compiler and runtime for .NET. Enabling you to run your PHP code on .NET, and interop with code written for .NET/C#.

### Scenario

Embedding interactive Razor components in PHP apps running on .NET  - including Wordpress.NET

That way you will not need to use Razor Views as an intermediary for rendering the components.

## Projects

* ``Server`` - The hosting web app
* ``Website`` - The PHP part of the app
* ``BlazorExtensions`` - The glue that makes Blazor in PHP possible
* ``BlazorClassLib`` - Contains the Blazor components

## Issues with interactive components

I have based this loosely on the [BlazorMinimalApiTes](https://github.com/marinasundstrom/BlazorMinimalApiTest) project, it enables interactive components through Minimal API endpoints - both running on Server, and in WebAssembly.

However, PHP requires you to get the rendered - the right rendered that supports emitting code for the rendering modes.

The ``HtmlRenderer`` only emits static HTMl - and it doesn't support interactivity.

One possible solution would be if the .NET Team opened up the ``EndpointHtmlRenderer`. (make it public)
In preparation, the code for this has been added, but it is commented out.

If this is done, then it is just a matter of uncommenting and recompiling.

## WebAssembly support

If everything works, you would simply add a Blazor WebAssembly standalone project, and reference it from the ``Server`` project. The set the appropriate render modes.
