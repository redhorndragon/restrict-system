using KioskLauncher;

namespace KioskLauncher.Tests;

public class EdgeControllerTests
{
    [Fact]
    public void BuildArguments_Contains_Kiosk_Flags_And_StartUrl()
    {
        var args = EdgeController.BuildArguments("https://example.com/portal");

        Assert.Contains("--kiosk", args);
        Assert.Contains("https://example.com/portal", args);
        Assert.Contains("--edge-kiosk-type=fullscreen", args);
        Assert.Contains("--no-first-run", args);
    }

    [Fact]
    public void BuildArguments_Quotes_Url_With_Spaces()
    {
        var args = EdgeController.BuildArguments("https://example.com/a b");

        Assert.Contains("\"https://example.com/a b\"", args);
    }
}
