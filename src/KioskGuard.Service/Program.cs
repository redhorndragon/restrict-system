using KioskGuard.Config;
using KioskGuard.Service;
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;

var configPath = Environment.GetEnvironmentVariable("KIOSKGUARD_CONFIG")
                 ?? @"C:\ProgramData\KioskGuard\config.json";
var logDir = Path.GetDirectoryName(configPath) ?? @"C:\ProgramData\KioskGuard";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.EventLog("KioskGuard", manageEventSource: true)
    .WriteTo.File(Path.Combine(logDir, "kioskguard-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddWindowsService(options => options.ServiceName = "KioskGuard");

    builder.Services.AddSingleton(new ConfigStore(configPath));
    builder.Services.AddSingleton(new SemaphoreSlim(1, 1));
    builder.Services.AddSingleton<BilibiliUpResolver>();
    builder.Services.AddSingleton<AllowlistSource>();
    builder.Services.AddSingleton<BrowserPolicyWriter>();
    builder.Services.AddSingleton<ProcessGuard>();
    builder.Services.AddHostedService<Worker>();

    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog(Log.Logger);

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "服务启动失败。");
}
finally
{
    Log.CloseAndFlush();
}
