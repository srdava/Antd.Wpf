using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Xunit;

namespace Antd.Wpf.Tests;

[Trait("Category", "PerformanceObservation")]
public sealed class StartupProbeTests
{
    [Theory]
    [InlineData("empty", 0, 0)]
    [InlineData("controls", 200, 1)]
    public void StartupProbe_RecordsStagesAndIsolatesEmptyAssemblyLoads(string mode, int controls, int dictionaries)
    {
        string output = NewOutput();
        long launch = Stopwatch.GetTimestamp();
        (int exitCode, string error) = RunProbe(mode, output);
        long exit = Stopwatch.GetTimestamp();
        Assert.True(exitCode == 0, error);
        string[] lines = File.ReadAllLines(output);
        Assert.Equal(2, lines.Length);
        string[] values = lines[1].Split(',');
        Assert.Equal(11, values.Length);
        Assert.Equal(mode, values[0]);
        Assert.Equal(controls, int.Parse(values[1], CultureInfo.InvariantCulture));
        Assert.True(int.Parse(values[2], CultureInfo.InvariantCulture) > 0);
        Assert.Equal(Stopwatch.Frequency, long.Parse(values[3], CultureInfo.InvariantCulture));
        long startup = long.Parse(values[4], CultureInfo.InvariantCulture);
        long loaded = long.Parse(values[5], CultureInfo.InvariantCulture);
        Assert.InRange(startup, launch, exit);
        Assert.InRange(loaded, startup, exit);
        Assert.InRange(long.Parse(values[6], CultureInfo.InvariantCulture), loaded, exit);
        Assert.InRange(long.Parse(values[7], CultureInfo.InvariantCulture), loaded, exit);
        Assert.Contains(values[8], new[] { "True", "False" });
        Assert.True(int.Parse(values[9], CultureInfo.InvariantCulture) >= controls);
        Assert.Equal(dictionaries, int.Parse(values[10], CultureInfo.InvariantCulture));
        string[] assemblies = File.ReadAllLines(Path.ChangeExtension(output, "assemblies.csv"));
        if (mode == "empty") { Assert.DoesNotContain(assemblies, line => line.EndsWith(",Antd.Wpf", StringComparison.Ordinal)); }
        else { Assert.Contains("ready,Antd.Wpf", assemblies); }
        Assert.Contains(File.ReadAllLines(Path.ChangeExtension(output, "environment.txt")), line => line.StartsWith("CLR=", StringComparison.Ordinal));
    }

    [Fact]
    public void StartupProbe_InvalidModeExitsWithoutOpeningNormalGallery()
    {
        string output = NewOutput();
        (int exitCode, string error) = RunProbe("unknown", output);
        Assert.Equal(2, exitCode);
        Assert.Contains("empty|controls", error);
        Assert.False(File.Exists(output));
    }

    [Fact]
    public void StartupProbe_UnwritableOutputExitsWithFailure()
    {
        string output = Path.GetDirectoryName(NewOutput())!;
        (int exitCode, string error) = RunProbe("empty", output);
        Assert.Equal(1, exitCode);
        Assert.False(string.IsNullOrWhiteSpace(error));
        Assert.True(Directory.Exists(output));
    }

    private static string NewOutput()
    {
        string directory = Path.Combine(Path.GetTempPath(), "antd startup probe", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "sample.csv");
    }

    private static (int ExitCode, string Error) RunProbe(string mode, string output)
    {
#if NETFRAMEWORK
        const string target = "net48";
        const string file = "Antd.Wpf.Gallery.exe";
#else
        const string target = "net8.0-windows";
        const string file = "Antd.Wpf.Gallery.dll";
#endif
        string gallery = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../samples/Antd.Wpf.Gallery/bin/Release", target, file));
        var start = new ProcessStartInfo
        {
            FileName = target == "net48" ? gallery : "dotnet",
            Arguments = (target == "net48" ? string.Empty : "\"" + gallery + "\" ") + "--measure-startup " + mode + " \"" + output + "\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        using Process process = Process.Start(start) ?? throw new InvalidOperationException("Could not start Gallery probe.");
        var error = process.StandardError.ReadToEndAsync();
        var stdout = process.StandardOutput.ReadToEndAsync();
        if (!process.WaitForExit(15000))
        {
            process.Kill();
            process.WaitForExit();
            throw new TimeoutException("Gallery startup probe did not exit.");
        }
        _ = stdout.GetAwaiter().GetResult();
        return (process.ExitCode, error.GetAwaiter().GetResult());
    }
}
