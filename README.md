# Raphael

A lightweight real-time image processing pipeline for ASP.NET Core. Process images via URL parameters — resize, crop, watermark, and apply color transformations through a single HTTP endpoint. Built with SkiaSharp.

## Usage

Register Raphael in your application:

```csharp
builder.Services.AddRaphael(builder.Configuration);

var app = builder.Build();

app.AddRaphaelRoutes();
```

Images can then be processed through the `/_raphael` endpoint:

```
/_raphael?url=https://example.com/image.jpg&width=800
```

Multiple processing options can be combined:

```
/_raphael?url=https://example.com/image.jpg&width=800&height=600
```

The processing pipeline discovers available effects automatically and applies them according to the configured effect order.

## Effects

Raphael currently includes:

- **Resize** - Resize image to specified dimensions
- **Crop** - Crop image to specified area
- **Color Matrix** - Apply color transformation using a color matrix
- **Watermark** - Add text or image watermark

Effects are extensible through the `IEffect` interface and can be discovered automatically using the `Effect` attribute.

## Configuration

Raphael supports configuration for:

- Effect order
- Effect mappings
- Default image quality
- Maximum image dimensions
- Output image format
- Allowed image sources

## Why Raphael?

Raphael is designed to make image processing simple to integrate into web applications. Instead of creating individual image processing endpoints, applications can use a single configurable pipeline driven by URL parameters.

## Built With

- .NET
- ASP.NET Core
- SkiaSharp

## License

MIT