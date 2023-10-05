<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Document</title>
    <base href="/" />
</head>
<body>

    <?php

    echo Peachpie\AspNetCore\HttpContextExtension::Component<BlazorClassLib\RenderMessage>([
        "Message" => "Hello from the Render Message component!"
    ]);

    ?>

    <?php
    echo Peachpie\AspNetCore\HttpContextExtension::Component<BlazorClassLib\Counter>([
        "IncrementBy" => 2
    ]);
    ?>

    <script src="_framework/blazor.web.js"></script>
    
</body>
</html>

<?php
