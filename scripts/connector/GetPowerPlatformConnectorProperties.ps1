#Requires -Version 7.0
#Requires -Modules @{ ModuleName = "Az.Accounts"; ModuleVersion = "5.0.0" }
[CmdletBinding()]
param ()

begin {
    $PowerAppsApiVersion = "2025-04-01"
    $PowerAppsAudience = "https://service.powerapps.com/"
    [ValidateNotNull()]
    [psobject]$PowerPlatformEnvironment = & (
        Join-Path -Resolve (
            Join-Path -Resolve (Join-Path -Resolve $PSScriptRoot "..") "env"
        ) "GetEnvironmentProperties.ps1"
    ) |
    Select-Object -First 1
    $PowerAppsApiUrl = "https://api.powerapps.com/providers/Microsoft.PowerApps/apis?api-version=${PowerAppsApiVersion}&`$filter=environment eq '$($PowerPlatformEnvironment.environmentName)'"
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Verbose "GET $PowerAppsApiUrl"
    }
    $PowerAppsConnectorsResponse = Invoke-RestMethod -Authentication OAuth `
        -Token ((Get-AzAccessToken -ResourceUrl $PowerAppsAudience -AsSecureString).Token) `
        -Method Get `
        -Uri $PowerAppsApiUrl `
        -WebSession $PowerPlatformWebSession `
        -Verbose:$false
    [psobject[]]$PowerAppsConnectorsArray = $PowerAppsConnectorsResponse.value |
    ForEach-Object {
        Add-Member -PassThru -InputObject $_.properties `
            -NotePropertyName "apiName" `
            -NotePropertyValue $_.name |
        Add-Member -PassThru -Force `
            -NotePropertyName "environmentName" `
            -NotePropertyValue $PowerPlatformEnvironment.environmentName
    } |
    Sort-Object -Property "isCustomApi", "tier", "publisher", "apiName" |
    Out-GridView -PassThru -Title "Select Power Platform Connector"
}

process {
    $PowerAppsConnectorsArray | Write-Output
}