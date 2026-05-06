#Requires -Version 7.0
#Requires -Modules @{ ModuleName = "Az.Accounts"; ModuleVersion = "5.0.0" }
[CmdletBinding()]
param ()

begin {
    $PowerAppsApiVersion = "2025-04-01"
    $PowerAppsAudience = "https://service.powerapps.com/"
    [psobject[]]$PowerAppsConnectionsArray = & (
        Join-Path -Resolve $PSScriptRoot "GetPowerPlatformConnectionProperties.ps1"
    )
}

process {
    foreach ($PowerAppsConnectionProperties in $PowerAppsConnectionsArray) {
        Write-Host "Connector: $($PowerAppsConnectionProperties.api.properties.displayName)"
        Write-Host "Connection: $($PowerAppsConnectionProperties.connectionName)"
        [string]$PowerAppsConnectionDisplayName = Read-Host -Prompt "New Display Name (current: $($PowerAppsConnectionProperties.displayName))"
        if (-not $PowerAppsConnectionDisplayName) { continue }
        $PowerAppsApiUrl = "https://api.powerapps.com/providers/Microsoft.PowerApps/apis/$($PowerAppsConnectionProperties.api.name)/connections/$($PowerAppsConnectionProperties.connectionName)?api-version=${PowerAppsApiVersion}&`$filter=environment eq '$($PowerPlatformEnvironment.environmentName)'"
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "PATCH $PowerAppsApiUrl"
        }
        $PowerAppsConnectionResponse = Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $PowerAppsAudience -AsSecureString).Token) `
            -Method Patch -Uri $PowerAppsApiUrl `
            -Headers @{ "Content-Type" = "application/json; charset=utf-8" } `
            -Body (ConvertTo-Json -Depth 10 -InputObject @{
                properties = @{
                    displayName = $PowerAppsConnectionDisplayName
                }
            }) `
            -WebSession $PowerPlatformWebSession `
            -Verbose:$false
        $PowerAppsConnectionPropertiesUpdated = Add-Member -PassThru `
            -InputObject $PowerAppsConnectionResponse.properties `
            -NotePropertyName "connectionName" `
            -NotePropertyValue $PowerAppsConnectionResponse.name
        Write-Output $PowerAppsConnectionPropertiesUpdated
    }
}