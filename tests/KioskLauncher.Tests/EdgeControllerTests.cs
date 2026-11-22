using KioskLauncher;

namespace KioskLauncher.Tests;

public class EdgeControllerTests
{
    [Fact]
    public void BuildArguments_Uses_Normal_Window_And_StartUrl()
    {
        var args = EdgeController.BuildArguments("https://example.com/portal");

        Assert.DoesNotContain("--kiosk", args);
        Assert.DoesNotContain("--edge-kiosk-type=", args);
        Assert.Contains("--start-maximized", args);
        Assert.Contains("https://example.com/portal", args);
        Assert.Contains("--no-first-run", args);
    }

    [Fact]
    public void BuildArguments_Quotes_Url_With_Spaces()
    {
        var args = EdgeController.BuildArguments("https://example.com/a b");

        Assert.Contains("\"https://example.com/a b\"", args);
    }
}
