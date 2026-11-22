namespace KioskGuard.Service.Models;

public sealed record AllowlistRules(
    List<string> Urls,
    List<string> BilibiliUpIds);

public sealed record BilibiliUpRule(string UpId)
{
    public string SpaceUrl => $"https://space.bilibili.com/{UpId}";
}
