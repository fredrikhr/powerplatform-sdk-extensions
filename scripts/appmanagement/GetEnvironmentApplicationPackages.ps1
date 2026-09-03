#Requires -Version 7.0
#Requires -Modules @{ ModuleName = "Az.Accounts"; ModuleVersion = "5.0.0" }
[CmdletBinding()]
param ()

begin {
    $PpApiVersion = "2024-10-01"
    $PpApiAudience = "https://api.powerplatform.com"
    [ValidateNotNull()]
    [psobject]$PowerPlatformEnvironment = & (
        Join-Path -Resolve (
            Join-Path -Resolve (Join-Path -Resolve $PSScriptRoot "..") "env"
        ) "GetEnvironmentProperties.ps1"
    ) |
    Select-Object -First 1
}

process {
    $PpEnvId = $PowerPlatformEnvironment.environmentName
    $PpApiUrl = "https://api.powerplatform.com/appmanagement/environments/${PpEnvId}/applicationPackages?api-version=${PpApiVersion}"
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Verbose "GET $PpApiUrl"
    }
    [ValidateNotNull()]
    $PpAppPackagesResponse = Invoke-RestMethod -Authentication OAuth `
        -Token ((Get-AzAccessToken -ResourceUrl $PpApiAudience -AsSecureString -ErrorAction 'Stop').Token) `
        -Method Get -Uri $PpApiUrl `
        -SessionVariable "PowerPlatformWebSession" `
        -Verbose:$false
    $PpAppPackagesResponse.value |
    Out-GridView -PassThru -Title "Select Power Platform Application Package" |
    ForEach-Object {
        Add-Member -PassThru -Force -InputObject $_ `
            -NotePropertyName "environmentName" `
            -NotePropertyValue $PpEnvId
    } |
    Write-Output
}