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
        $ConnectionConsentPayload = ConvertTo-Json -Depth 50 `
            -InputObject @{ redirectUrl = "https://httpbin.org/anything" }
        $PowerAppsApiUrl = "https://api.powerapps.com/providers/Microsoft.PowerApps/apis/$($PowerAppsConnectionProperties.api.name)/connections/$($PowerAppsConnectionProperties.connectionName)/getConsentLink?api-version=${PowerAppsApiVersion}&`$filter=environment eq '$($PowerAppsConnectionProperties.environment.name)'"
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "POST $PowerAppsApiUrl"
        }
        $PowerAppsConsentResponse = Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $PowerAppsAudience -AsSecureString).Token) `
            -Method Post -Uri $PowerAppsApiUrl `
            -Headers @{ "Content-Type" = "application/json; charset=utf-8" } `
            -Body $ConnectionConsentPayload `
            -WebSession $PowerPlatformWebSession `
            -Verbose:$false
        Write-Host ""
        Write-Host -ForegroundColor White "Consent Link"
        Write-Host "Open the following link in your browser to grant consent for the created connection."
        Write-Host "consentLink: $($PowerAppsConsentResponse.consentLink)"
        [void](Read-Host -Prompt "Press ENTER after consenting to continue")
        $PowerAppsApiUrl = "https://api.powerapps.com/providers/Microsoft.PowerApps/apis/$($PowerAppsConnectionProperties.api.name)/connections/$($PowerAppsConnectionProperties.connectionName)?api-version=${PowerAppsApiVersion}&`$filter=environment eq '$($PowerAppsConnectionProperties.environment.name)'"
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "GET $PowerAppsApiUrl"
        }
        $PowerAppsConnectionConsentedResponse = Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $PowerAppsAudience -AsSecureString).Token) `
            -Method Put -Uri $PowerAppsApiUrl `
            -Headers @{ "Content-Type" = "application/json; charset=utf-8" } `
            -Body $ConnectionCreatePayload `
            -WebSession $PowerPlatformWebSession `
            -Verbose:$false
        $PowerAppsConnectionProperties = Add-Member -PassThru `
            -InputObject $PowerAppsConnectionConsentedResponse.properties `
            -NotePropertyName "connectionName" `
            -NotePropertyValue $PowerAppsConnectionConsentedResponse.name
        Write-Output $PowerAppsConnectionProperties
    }
}