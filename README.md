## Blazor Components in PHP (PeachPie)

This project is an experiment to implement Blazor support in PHP on Peachpie.

It can render _interactive components_ (see ``Counter``), with the caveat that I used reflection to expose internal functionality.

This will only work with .NET 8 RC1, since there are changes to the method signatures in RC 2.

## Background

Rendering Razor View partials in PHP is already supported by Peachpie. This extends the support to the more modern Razor component model - and possibly hosting interactive components.

Peachpie is a PHP compiler and runtime for .NET. Enabling you to run your PHP code on .NET, and interop with code written for .NET/C#.

## Sample

In ``index.php``:

```php
<?php

echo Peachpie\AspNetCore\HttpContextExtension::Component<BlazorClassLib\RenderMessage>([
    "Message" => "Hello from the Render Message component!"
]);
```

Where ``RenderMessage`` is the Razor component.

## Scenario

Embedding interactive Razor components in PHP apps running on .NET  - including Wordpress.NET

That way you will not need to use Razor Views as an intermediary for rendering the components.

## Projects

* ``Server`` - The hosting web app
* ``Website`` - The PHP part of the app
* ``BlazorExtensions`` - The glue that makes Blazor in PHP possible
* ``BlazorClassLib`` - Contains the Blazor components

## Issues with interactive components

I have based this loosely on the [BlazorMinimalApiTest](https://github.com/marinasundstrom/BlazorMinimalApiTest) project, it enables interactive components through Minimal API endpoints - both running on Server, and in WebAssembly.

However, PHP requires you to get the rendered - the right rendered that supports emitting code for the rendering modes.

The ``HtmlRenderer`` only emits static HTMl - and it doesn't support interactivity.

One possible solution would be if the .NET Team exposed the functionality of ``EndpointHtmlRenderer``.

In the meantime, I have done a Reflection hack. This might break with future versions as the APIs are internal.

## WebAssembly support

If everything works, you would simply add a Blazor WebAssembly standalone project, and reference it from the ``Server`` project. The set the appropriate render modes.

## Issue with casting PHP parameters to the right CLR type

The component might take a parameter of type ``int`` but Peachpie is casting it to ``long``.

Perhaps special logics is needed.
