using System.Diagnostics;

namespace ApiApp.Realtime;

/// <summary>Джерело власних span-ів (трасування). Підключається до OpenTelemetry в Program.cs; у Application Insights span видно всередині запиту.</summary>
public static class Telemetry
{
    public const string SourceName = "ApiApp.Realtime";

    public static readonly ActivitySource Source = new(SourceName);
}
