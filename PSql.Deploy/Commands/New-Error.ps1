using namespace System.Management.Automation

<#
    Copyright 2022 Jeffrey Sharp

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
    [OutputType([System.Management.Automation.ErrorRecord])]
    param(
        # Exception or error message.
        #
        # This parameter populates the ErrorRecord properties:
        # - Exception
        # - CategoryInfo.Reason (exception type name without namespace)
        [Parameter(Mandatory, Position = 0)]
        [Alias("Error")]
        [Exception] $Exception,

        # Developer-defined identifier for the error.
        #
        # This parameter populates the ErrorRecord property:
        # - FullyQualifiedErrorId
        [Parameter()]
        [string] $Id,

        # Category that best describes the error.
        #
        # This parameter populates the ErrorRecord property:
        # - CategoryInfo.Category
        [Parameter()]
        [System.Management.Automation.ErrorCategory] $Category = 'NotSpecified',

        # Object being processed when the error occurred.
        #
        # This parameter populates the ErrorRecord properties:
        # - TargetObject
        # - CategoryInfo.TargetName (the result of .ToString() on the target)
        # - CategoryInfo.TargetType (target type name without namespace)
        [Parameter()]
        [object] $TargetObject,

        # Textual description activity in progress when the error occurred.
        #
        # This parameter populates the ErrorRecord property:
        # - CategoryInfo.Activity
        [Parameter()]
        [string] $Activity
    )

    process {
        # In PS 5.1 and earlier, output looked like:
        #
        # {Exception.Message}
        #     + CategoryInfo          : {Category}: ({TargetName}:{TargetType}) [{Activity}], {Reason}
        #     + FullyQualifiedErrorId : {Id}
        #
        # In PS 7.0 and later, it can look like any of these:
        #
        # {Activity}: {Exception.Message}
        # {Category}: {Exception.Message}
        # {Reason}: {Exception.Message}
        # {Exception.GetType().Name}: {Exception.Message}

        $Record = [ErrorRecord]::new($Exception, $Id, $Category, $TargetObject)
        $Record.CategoryInfo.Activity = $Activity
        $Record
    }
}
