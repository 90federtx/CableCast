using System.IO;
using CableCast.Services;
using Xunit;

namespace CableCast.Tests;

public class LogServiceTests
{
    [Fact]
    public void PersistentLogsRedactRegisteredSerialsAndPersonalIdentifiers()
    {
        var directory = Path.Combine(Path.GetTempPath(), "cablecast-log-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var log = new LogService(directory);
            log.RegisterSensitiveValue("DEVICE12345");
            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            log.Info($"serial=DEVICE12345 email=user@example.com address=192.168.1.25 ipv6=2001:db8::1 path={profile}");

            var saved = File.ReadAllText(Directory.GetFiles(directory, "*.log").Single());
            Assert.DoesNotContain("DEVICE12345", saved);
            Assert.DoesNotContain("user@example.com", saved);
            Assert.DoesNotContain("192.168.1.25", saved);
            Assert.DoesNotContain("2001:db8::1", saved);
            Assert.DoesNotContain(profile, saved);
            Assert.Contains("[identificativo nascosto]", saved);
            Assert.Contains("[email nascosta]", saved);
            Assert.Contains("[indirizzo rete nascosto]", saved);
            Assert.Contains("%USERPROFILE%", saved);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LogMessagesAreBounded()
    {
        var log = new LogService();
        log.Raw(new string('x', 20000));

        Assert.True(log.Snapshot().Single().Message.Length <= 16385);
    }
}
