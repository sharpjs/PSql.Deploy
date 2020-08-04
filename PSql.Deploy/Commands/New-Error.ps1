<#
    Part of: PSqlDeploy - Simple PowerShell Cmdlets for SQL Server Database Deployment
    https://github.com/sharpjs/PSqlDeploy

    Copyright (C) 2020 Jeffrey Sharp

    Permission to use, copy, modify, and distribute this software for any
    purpose with or without fee is hereby granted, provided that the above
    copyright notice and this permission notice appear in all copies.

    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
    WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
    MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
    ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
    WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
    ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
    OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
#>

function New-Error {
    <#
    .SYNOPSIS
        Creates an ErrorRecord object.

    .DESCRIPTION
        Inspired by this StackOverflow answer:
        https://stackoverflow.com/a/39949027/142138
    #>
    [CmdletBinding()]
    param(
        # An error message or exception.
        [Parameter(Mandatory, Position=0)]
        [object] $Error,

        # A developer-defined identifier for the error.
        [Parameter()]
        [string] $Id,

        # The category to which the error belongs.
        [Parameter()]
        [System.Management.Automation.ErrorCategory] $Category = 'NotSpecified',

        # The object being processed when the error occurred.
        [Parameter()]
        [object] $TargetObject,

        # The name of the object being processed when the error occurred.
        [Parameter()]
        [string] $TargetName,

        # The type of the object being processed when the error occurred.
        [Parameter()]
        [string] $TargetType,

        # The name of the activity that caused the error
        [Parameter()]
        [string] $Activity,

        # The reason why the error occurred.
        [Parameter()]
        [string] $Reason
    )

    # Output will look like this:
    # {Error}
    #     + CategoryInfo          : {Category}: ({TargetName}:{TargetType}) [{Activity}], {Reason}
    #     + FullyQualifiedErrorId : {Id}

    $Record = New-Object System.Management.Automation.ErrorRecord $Error, $Id, $Category, $TargetObject
    $Record.CategoryInfo.TargetName = $TargetName
    $Record.CategoryInfo.TargetType = $TargetType
    $Record.CategoryInfo.Activity   = $Activity
    $Record.CategoryInfo.Reason     = $Reason
    $Record
}
