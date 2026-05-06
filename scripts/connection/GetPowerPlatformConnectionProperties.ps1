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
    $PowerAppsApiUrl = "https://api.powerapps.com/providers/Microsoft.PowerApps/connections?api-version=${PowerAppsApiVersion}&`$filter=environment eq '$($PowerPlatformEnvironment.environmentName)'&`$expand=api"
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Verbose "GET $PowerAppsApiUrl"
    }
    $PowerAppsConnectionsResponse = Invoke-RestMethod -Authentication OAuth `
        -Token ((Get-AzAccessToken -ResourceUrl $PowerAppsAudience -AsSecureString).Token) `
        -Method Get `
        -Uri $PowerAppsApiUrl `
        -WebSession $PowerPlatformWebSession `
        -Verbose:$false
    [psobject[]]$PowerAppsConnectionsArray = $PowerAppsConnectionsResponse.value |
    ForEach-Object {
        Add-Member -PassThru -InputObject $_.properties `
            -NotePropertyName "connectionName" `
            -NotePropertyValue $_.name
    } |
    Sort-Object -Property @{
        Expression = { $_.api.properties.isCustomApi }
    }, @{
        Expression = { $_.api.properties.tier }
    }, @{
        Expression = { $_.api.properties.publisher }
    }, @{
        Expression = { $_.api.name }
    }, "connectionName" |
    Out-GridView -PassThru -Title "Select Power Platform Connection"
}

process {
    $PowerAppsConnectionsArray | Write-Output
}