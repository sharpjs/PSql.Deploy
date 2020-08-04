<#
    Part of: PSqlDeploy - Simple PowerShell Cmdlets for SQL Server Database Deployment
    https://github.com/sharpjs/PSqlDeploy

    Copyright (C) 2017 Jeffrey Sharp

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

function New-SqlMigrationTarget {
    <#
    .SYNOPSIS
        Creates an object that represents a target database of a SQL migration.
    #>
    param (
        # Name of the database server.  Must be a valid hostname or IP address, with an optional instance suffix (ex: "10.12.34.56\DEV").  A dot (".") may be used to specify a local server.  The default value is ".".
        [Parameter(Position = 0, ValueFromPipelineByPropertyName)]
        [string] $Server = ".",

        # Name of the target database.
        [Parameter(Mandatory, Position = 1, ValueFromPipelineByPropertyName)]
        [string] $Database,

        # Credential to use when connecting to the server.  If not provided, integrated authentication is used.
        [Parameter(Position = 2, ValueFromPipelineByPropertyName)]
        [System.Management.Automation.Credential()]
        [PSCredential] $Credential = [PSCredential]::Empty,

        # Name of the connecting application.  The default value is "Migration".
        [Parameter(ValueFromPipelineByPropertyName)]
        [string] $ApplicationName = "Migration",

        # Time to wait for a connection to be established.  The default value is 15 seconds.
        [Parameter(ValueFromPipelineByPropertyName)]
        [string] $TimeoutSeconds = 15,

        # Do not encrypt data sent over the network connection.
        # WARNING: Using this option is a security risk.
        [Parameter(ValueFromPipelineByPropertyName)]
        [switch] $NoEncryption,

        # Do not validate the server's identity when using an encrypted connection.
        # WARNING: Using this option is a security risk.
        [Parameter(ValueFromPipelineByPropertyName)]
        [switch] $TrustServerCertificate = $true # TODO: $false
    )

    New-SqlConnectionInfo @PSBoundParameters
}
