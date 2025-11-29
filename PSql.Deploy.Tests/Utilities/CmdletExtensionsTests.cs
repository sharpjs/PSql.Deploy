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
    [TestCase("",                          false)]
    [TestCase("-WhatIf ([switch] $false)", false)]
    [TestCase("-WhatIf ([switch] $true)",  true )]
    [TestCase("-WhatIf $false",            false)]
    [TestCase("-WhatIf $true",             true )]
    [TestCase("-WhatIf Other",             false)]
    public void IsWhatIf_ByParameter(string arg, bool expected)
    {
        var (output, exception) = Execute(
            $"""
            Test-CmdletExtensions -Case IsWhatIf {arg}
            """
        );

        exception.ShouldBeNull();

        output.ShouldHaveSingleItem().ShouldNotBeNull()
            .BaseObject.ShouldBe(expected);
    }

    [Test]
    [TestCase("$false",  false)]
    [TestCase("$true",   true )]
    [TestCase("'Other'", false)]
    public void IsWhatIf_ByPreference(string value, bool expected)
    {
        var (output, exception) = Execute(
            $"""
            $WhatIfPreference = {value}
            Test-CmdletExtensions -Case IsWhatIf
            """
        );

        exception.ShouldBeNull();

        output.ShouldHaveSingleItem().ShouldNotBeNull()
            .BaseObject.ShouldBe(expected);
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
    public void GetCurrentPath()
    {
        var (output, exception) = Execute(
            "Test-CmdletExtensions -Case GetCurrentPath"
        );

        exception.ShouldBeNull();

        output.ShouldHaveSingleItem().ShouldNotBeNull()
            .BaseObject.ShouldBe(TestContext.CurrentContext.TestDirectory);
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
        var (output, exception) = Execute(
            "Test-CmdletExtensions -Case WriteHost -Message $null"
        );

        exception.ShouldBeNull();

        output.ShouldHaveSingleItem().ShouldNotBeNull()
            .BaseObject.ShouldBe(new PSInformation(""));
    }

    [Test]
    public void WriteHost_NotNull()
    {
        var (output, exception) = Execute(
            "Test-CmdletExtensions -Case WriteHost -Message Foo"
        );

        exception.ShouldBeNull();

        output.ShouldHaveSingleItem().ShouldNotBeNull()
            .BaseObject.ShouldBe(new PSInformation("Foo"));
    }

    private static (IReadOnlyList<PSObject?>, Exception?) Execute(string command)
    {
        return ScriptExecutor.Execute(command);
    }
}
