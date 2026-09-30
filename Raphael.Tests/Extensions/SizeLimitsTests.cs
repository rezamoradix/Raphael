using Raphael.Extensions;
using Raphael.Models;

namespace Raphael.Tests.Extensions;

/// <summary>Upscale=false: a requested size that would ENLARGE the picture is dropped, so a small source is served at its
/// own size instead of a bigger, no-sharper file.</summary>
public class SizeLimitsTests
{
    private static RequestQueries Query(int? width, int? height, bool? upscale = false, int? cropW = null, int? cropH = null, int? cropX = null, int? cropY = null)
        => new() { Width = width, Height = height, Upscale = upscale, CropWidth = cropW, CropHeight = cropH, CropX = cropX, CropY = cropY };

    [Fact]
    public void A_request_bigger_than_the_source_is_dropped()
    {
        var q = Query(900, 1200);
        q.LimitToSource(300, 400);
        Assert.Null(q.Width);
        Assert.Null(q.Height);
    }

    [Fact]
    public void A_request_smaller_than_the_source_is_kept()
    {
        var q = Query(900, 1200);
        q.LimitToSource(3000, 4000);
        Assert.Equal((900, 1200), (q.Width, q.Height));
    }

    [Fact]
    public void The_fit_decides_not_either_side_alone()
    {
        // 300x200 into 900x1200: fits by scaling 3x (width) - an enlargement even though the box is taller than wide.
        var q = Query(900, 1200);
        q.LimitToSource(300, 200);
        Assert.Null(q.Width);

        // 3000x600 into 900x1200: scale 0.3 (width is the limit) - a shrink, kept.
        var kept = Query(900, 1200);
        kept.LimitToSource(3000, 600);
        Assert.Equal(900, kept.Width);
    }

    [Fact]
    public void Width_only_and_height_only_requests_are_judged_on_their_own_side()
    {
        var wide = Query(800, null);
        wide.LimitToSource(400, 300);
        Assert.Null(wide.Width);

        var tall = Query(null, 500);
        tall.LimitToSource(2000, 1000);
        Assert.Equal(500, tall.Height);
    }

    [Fact]
    public void The_size_is_measured_after_the_crop_because_the_crop_runs_first()
    {
        // A big photo cropped to 300x400: a 900x1200 request would enlarge THAT, so it is dropped.
        var cropped = Query(900, 1200, cropW: 300, cropH: 400, cropX: 0, cropY: 0);
        cropped.LimitToSource(4000, 6000);
        Assert.Null(cropped.Width);

        // The same photo cropped to 3000x4000 can be shrunk to 900x1200 just fine.
        var largeCrop = Query(900, 1200, cropW: 3000, cropH: 4000, cropX: 0, cropY: 0);
        largeCrop.LimitToSource(4000, 6000);
        Assert.Equal(900, largeCrop.Width);
    }

    [Fact]
    public void A_crop_running_past_the_edge_is_measured_as_what_is_actually_left()
    {
        var q = Query(900, 1200, cropW: 3000, cropH: 4000, cropX: 3900, cropY: 5900); // only 100x100 px remain in the corner
        q.LimitToSource(4000, 6000);
        Assert.Null(q.Width);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    public void Without_Upscale_false_nothing_changes(bool? upscale)
    {
        var q = Query(900, 1200, upscale);
        q.LimitToSource(300, 400);
        Assert.Equal((900, 1200), (q.Width, q.Height)); // the old behaviour: enlarge when asked
    }

    [Fact]
    public void No_size_requested_is_a_no_op()
    {
        var q = Query(null, null);
        q.LimitToSource(300, 400);
        Assert.Null(q.Width);
    }
}
