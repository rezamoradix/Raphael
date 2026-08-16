# Raphael

<p align="center">
  <img
    width="256"
    height="256"
    alt="Raphael"
    src="https://github.com/user-attachments/assets/fcf6da03-6d00-4d1c-bc8a-5b7e7d7f788a"
  />
</p>

**Raphael** is a lightweight real-time image processing pipeline for ASP.NET Core, built with .NET and SkiaSharp.

It provides a simple HTTP endpoint for resizing, cropping, watermarking, transforming, and processing images through query parameters.

## Usage

Register Raphael in your application:

```csharp
builder.Services.AddRaphael(builder.Configuration);

var app = builder.Build();

app.AddRaphaelRoutes();
```

Images can then be processed through the `/_raphael` endpoint:

```text
/_raphael?url=https://example.com/image.jpg&width=800
```

Multiple processing options can be combined:

```text
/_raphael?url=https://example.com/image.jpg&width=800&height=600
```

The processing pipeline discovers available effects automatically and applies them according to the configured effect order.

## Effects

Raphael currently includes:

* Resize
* Crop
* Color Matrix
* Watermark

Effects are extensible through the `IEffect` interface and can be discovered automatically using the `Effect` attribute.

## Configuration

Raphael supports configuration for:

* Effect order
* Effect mappings
* Default image quality
* Maximum image dimensions
* Output image format
* Allowed image sources

## Why Raphael?

Raphael is designed to make image processing simple to integrate into web applications. Instead of creating individual image processing endpoints, applications can use a single configurable pipeline driven by URL parameters.

## Built With

* .NET
* ASP.NET Core
* SkiaSharp

## License

MIT
