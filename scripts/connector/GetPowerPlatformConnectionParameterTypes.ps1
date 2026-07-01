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
            -NotePropertyValue $_.name
    }
    [System.Collections.Generic.HashSet[string]]$PowerAppsConnectionParameterTypes =
    New-Object "System.Collections.Generic.HashSet[string]" `
        -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
}

process {
    foreach ($PowerAppsConnectorProperties in $PowerAppsConnectorsArray) {
        foreach ($PowerAppsConnectionParameterProperty in $PowerAppsConnectorProperties.connectionParameters.PSObject.Properties) {
            $PowerAppsConnectionParameter = $PowerAppsConnectionParameterProperty.Value
            $PowerAppsConnectionParameterType = $PowerAppsConnectionParameter.type
            [void]($PowerAppsConnectionParameterTypes.Add($PowerAppsConnectionParameterType))
        }
    }
    foreach ($PowerAppsConnectionParameterSet in $PowerAppsConnectorProperties.connectionParameterSets.values) {
        foreach ($PowerAppsConnectionParameterProperty in $PowerAppsConnectionParameterSet.parameters.PSObject.Properties) {
            $PowerAppsConnectionParameterType = $PowerAppsConnectionParameter.type
            [void]($PowerAppsConnectionParameterTypes.Add($PowerAppsConnectionParameterType))
        }
    }
    $PowerAppsConnectionParameterTypes |
    Sort-Object | Write-Output
}