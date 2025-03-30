using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PSql.Deploy;

[TestFixture]
public class ProcessInfoTests
{
    private static ProcessInfo Info => ProcessInfo.Instance;

    [Test]
    public void MachineName_Get()
    {
        var a = Info.MachineName;
        var b = Info.MachineName;

        a.ShouldBe(Environment.MachineName);
        a.ShouldBeSameAs(b);
    }

    [Test]
    public void ProcessorCount_Get()
    {
        Info.ProcessorCount.ShouldBe(Environment.ProcessorCount);
    }

    [Test]
    public void OSDescription_Get()
    {
        var a = Info.OSDescription;
        var b = Info.OSDescription;

        a.ShouldBe(RuntimeInformation.OSDescription);
        a.ShouldBeSameAs(b);
    }

    [Test]
    public void OSArchitecture_Get()
    {
        Info.OSArchitecture.ShouldBe(RuntimeInformation.OSArchitecture);
    }

    [Test]
    public void UserName_Get()
    {
        var a = Info.UserName;
        var b = Info.UserName;

        a.ShouldBe(Environment.UserName);
        a.ShouldBeSameAs(b);
    }

    [Test]
    public void FrameworkDescription_Get()
    {
        var a = Info.FrameworkDescription;
        var b = Info.FrameworkDescription;

        a.ShouldBe(RuntimeInformation.FrameworkDescription);
        a.ShouldBeSameAs(b);
    }

    [Test]
    public void ProcessId_Get()
    {
        Info.ProcessId.ShouldBe(Process.GetCurrentProcess().Id);
    }

    [Test]
    public void ProcessName_Get()
    {
        var a = Info.ProcessName;
        var b = Info.ProcessName;

        a.ShouldBe(Process.GetCurrentProcess().ProcessName);
        a.ShouldBeSameAs(b);
    }

    [Test]
    public void ProcessArchitecture_Get()
    {
        Info.ProcessArchitecture.ShouldBe(RuntimeInformation.ProcessArchitecture);
    }
}
