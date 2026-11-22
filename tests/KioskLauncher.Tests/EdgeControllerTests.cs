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
        Assert.Contains("--edge-kiosk-type=public-browsing", args);
        Assert.Contains("--no-first-run", args);
        Assert.Contains("--kiosk-idle-timeout-minutes=0", args);
    }

    [Fact]
    public void BuildArguments_Quotes_Url_With_Spaces()
    {
        var args = EdgeController.BuildArguments("https://example.com/a b");

        Assert.Contains("\"https://example.com/a b\"", args);
    }
}
