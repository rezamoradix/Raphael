using Raphael.Core;

namespace Raphael.Tests.Core;

public class MimeTests
{
    [Theory]
    [InlineData("photo.png", ImageFormat.Png)]
    [InlineData("PHOTO.PNG", ImageFormat.Png)]
    [InlineData("https://cdn.example.com/a/b.webp?x=1", ImageFormat.WebP)]
    [InlineData("photo.jpg", ImageFormat.Jpeg)]
    [InlineData("photo.jpeg", ImageFormat.Jpeg)]
    [InlineData("no-extension", ImageFormat.Jpeg)]
    public void Detect_MapsExtensionToFormat(string uri, ImageFormat expected)
    {
        Assert.Equal(expected, Mime.Detect(uri));
    }

    [Theory]
    [InlineData(ImageFormat.Jpeg, "image/jpeg")]
    [InlineData(ImageFormat.Png, "image/png")]
    [InlineData(ImageFormat.WebP, "image/webp")]
    public void ToContentType_ReturnsMimeType(ImageFormat format, string expected)
    {
        Assert.Equal(expected, format.ToContentType());
    }
}
