using CableCast.Models;
using CableCast.Services;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace CableCast.Tests;

public class ScrcpyLifecycleTests
{
    [Fact]
    public void DisposeWithoutProcessLeavesStoppedState()
    {
        using var runner = new ScrcpyRunner(new LogService());
        var states = new List<MirrorState>();
        runner.StateChanged += states.Add;

        runner.Dispose();

        Assert.Equal(MirrorState.Stopped, runner.State);
        Assert.Empty(states);
    }

    [Fact]
    public void SerialArgumentIsRedactedFromLog()
    {
        Assert.Equal("--serial=[nascosto]", ScrcpyRunner.RedactSerialArgument("--serial=PRIVATE123"));
        Assert.Equal("--max-fps=60", ScrcpyRunner.RedactSerialArgument("--max-fps=60"));
    }

    [Fact]
    public void StartFailureLeavesNoActiveProcess()
    {
        var log = new LogService();
        using var runner = new ScrcpyRunner(log, psi =>
        {
            psi.FileName = Path.Combine(Path.GetTempPath(), "missing-cablecast-scrcpy.exe");
            return new Process { StartInfo = psi, EnableRaisingEvents = true };
        });

        var device = new AdbDevice("USB123", "device", "Pixel", null, null);
        runner.Start(device, new MirrorSettings());

        Assert.Equal(MirrorState.Stopped, runner.State);
    }
}
