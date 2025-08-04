// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
public class CmdletExtensionsTests
{
    [Test]
    public void IsWhatIf_NullCmdlet()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            default(PSCmdlet)!.IsWhatIf();
        });
    }

    [Test]
    public void GetCurrentPath_NullCmdlet()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            default(PSCmdlet)!.GetCurrentPath();
        });
    }

    [Test]
    public void WriteHost_NullCmdlet()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            default(PSCmdlet)!.WriteHost("");
        });
    }

    [Test]
    public void WriteHost_Null()
    {
        var cmdlet = new TestCommand();

        // TODO: Figure out how to test this properly.
        //
        // For now, just exercise the code path.  If a NotImplementedException
        // is thrown, the code path extended past this module's code and into
        // the PowerShell runtim

        Should.Throw<NotImplementedException>(() =>
        {
            cmdlet.WriteHost(null);
        });
    }

    private class TestCommand : PSCmdlet { }
}
