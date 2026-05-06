#Requires -Version 7.0
#Requires -Modules @{ ModuleName = "Az.Accounts"; ModuleVersion = "5.0.0" }
[CmdletBinding()]
param (
    [Parameter()]
    [string]$ConnectionId = [guid]::NewGuid().ToString("N"),
    [Parameter()]
    [switch]$Consent
)

begin {
    $PowerAppsApiVersion = "2025-04-01"
    $PowerAppsAudience = "https://service.powerapps.com/"
    [ValidateNotNull()]
    [psobject]$PowerAppsConnectorProperties = & (
        Join-Path (
            Join-Path (Join-Path -Resolve $PSScriptRoot "..") "connector"
        ) "GetPowerPlatformConnectorProperties.ps1"
    ) |
    Select-Object -First 1
}

process {
    Write-Host "Connector: $($PowerAppsConnectorProperties.displayName)"
    Write-Host "Connector ID: $($PowerAppsConnectorProperties.apiName)"

    $ConnectionProperties = New-Object pscustomobject
    Write-Host "Connection ID: ${ConnectionId}"

    $ConnectionParameterSet = $null
    if ($PowerAppsConnectorProperties.connectionParameterSets) {
        $ConnectionParameterSetChoices = New-Object 'System.Collections.ObjectModel.Collection[System.Management.Automation.Host.ChoiceDescription]'
        $ConnectionParameterSetChoices.Add((
                New-Object System.Management.Automation.Host.ChoiceDescription `
                    -ArgumentList "&0 <none>", "Unspecified connection parameter set"
            ))
        foreach ($ConnectionParameterSetDefinition in $PowerAppsConnectorProperties.connectionParameterSets.values) {
            $ConnectionParameterSetLabel = $ConnectionParameterSetDefinition.uiDefinition.displayName ??
            $ConnectionParameterSetDefinition.name
            if ($ConnectionParameterSetChoices.Count -lt 10) {
                $ConnectionParameterSetLabel = "&$($ConnectionParameterSetChoices.Count) ${ConnectionParameterSetLabel}"
            }
            $ConnectionParameterSetDescription = $ConnectionParameterSetDefinition.uiDefinition.description
            $ConnectionParameterSetChoices.Add((
                    New-Object System.Management.Automation.Host.ChoiceDescription `
                        -ArgumentList $ConnectionParameterSetLabel, $ConnectionParameterSetDescription
                ))
        }
        $ConnectionParameterSetChoiceIdx = $Host.UI.PromptForChoice(
            $PowerAppsConnectorProperties.connectionParameterSets.uiDefinition.displayName ??
            "Connection parameter set",
            $PowerAppsConnectorProperties.connectionParameterSets.uiDefinition.description ??
            "Select connection parameter set to use",
            $ConnectionParameterSetChoices,
            0
        )
        if ($ConnectionParameterSetChoiceIdx -gt 0) {
            $ConnectionParameterSet = $PowerAppsConnectorProperties.connectionParameterSets.values[$ConnectionParameterSetChoiceIdx - 1]
        }
    }
    $ConnectionParameterProperties = if ($ConnectionParameterSet) {
        $ConnectionParameterSet.parameters.PSObject.Properties
    }
    else {
        $PowerAppsConnectorProperties.connectionParameters.PSObject.Properties
    }
    $ConnectionParameterValues = New-Object pscustomobject
    foreach ($ConnectionParameterProperty in $ConnectionParameterProperties) {
        $ConnectionParameterName = $ConnectionParameterProperty.Name
        $ConnectionParameterDef = $ConnectionParameterProperty.Value
        $ConnectionParameterType = $ConnectionParameterDef.type
        [psobject[]]$AllowedValues = $ConnectionParameterDef.uiDefinition.constraints.allowedValues ??
        $ConnectionParameterDef.allowedValues
        $ConnectionParameterLabel = $ConnectionParameterDef.uiDefinition.displayName ??
        $ConnectionParameterName
        $ConnectionParameterDescription = $ConnectionParameterDef.uiDefinition.description ??
        "Type: ${ConnectionParameterType}"
        $ConnectionParameterTooltip = $ConnectionParameterDef.uiDefinition.tooltip
        $ConnectionParameterMessage = if ($ConnectionParameterTooltip) {
            "${ConnectionParameterDescription}`n${ConnectionParameterTooltip}"
        }
        else {
            $ConnectionParameterDescription
        }
        [bool]$ConnectionParameterHasDefault = $null -ne $ConnectionParameterDef.PSObject.Properties["defaultValue"]
        $ConnectionParameterDefaultValue = $ConnectionParameterDef.defaultValue
        [bool]$ConnectionParameterRequired = $false
        [void]([bool]::TryParse($ConnectionParameterDef.uiDefinition.constraints.required, [ref]$ConnectionParameterRequired))
        $ConnectionParameterValue = $null
        if ($AllowedValues) {
            $PromptChoices = New-Object 'System.Collections.ObjectModel.Collection[System.Management.Automation.Host.ChoiceDescription]'
            $PromptChoiceDefaultIdx = -1
            if (-not $ConnectionParameterRequired) {
                $PromptChoiceHelp = "Omit connection parameter"
                $PromptChoiceLabel = "<unspecified>"
                $PromptChoiceDescr = New-Object System.Management.Automation.Host.ChoiceDescription `
                    -ArgumentList $PromptChoiceLabel, $PromptChoiceHelp
                $PromptChoices.Add($PromptChoiceDescr)
                $PromptChoiceDefaultIdx = 0
            }
            foreach ($AllowedValueDef in $AllowedValues) {
                $PromptChoiceHelp = $AllowedValueDef.text
                $PromptChoiceLabel = $AllowedValueDef.name ??
                $AllowedValueDef.value
                $PromptChoiceDescr = New-Object System.Management.Automation.Host.ChoiceDescription `
                    -ArgumentList $PromptChoiceLabel, $PromptChoiceHelp
                if ($ConnectionParameterHasDefault -and $AllowedValueDef.value -eq $ConnectionParameterDefaultValue) {
                    $PromptChoiceDefaultIdx = $PromptChoices.Count
                }
                $PromptChoices.Add($PromptChoiceDescr)
            }
            $PromptChoiceIdx = $Host.UI.PromptForChoice(
                $ConnectionParameterLabel,
                "${ConnectionParameterMessage}`n${ConnectionParameterName}",
                $PromptChoices,
                $PromptChoiceDefaultIdx
            )
            if (-not $ConnectionParameterRequired) {
                $PromptChoiceIdx -= 1
            }
            if ($PromptChoiceIdx -lt 0) { continue }
            else {
                $ConnectionParameterValue = $AllowedValues[$PromptChoiceIdx].value
                Write-Host "${ConnectionParameterName}: ${ConnectionParameterValue}"
            }
        }
        elseif ($ConnectionParameterType -eq "bool") {
            if ($ConnectionParameterHasDefault -and $ConnectionParameterDefaultValue -is [string]) {
                $ConnectionParameterDefaultBoolValue = $false
                if ([bool]::TryParse($ConnectionParameterDefaultValue, [ref]$ConnectionParameterDefaultBoolValue)) {
                    $ConnectionParameterDefaultValue = $ConnectionParameterDefaultBoolValue
                    Remove-Variable "ConnectionParameterDefaultBoolValue"
                }
            }
            $PromptChoices = New-Object 'System.Collections.ObjectModel.Collection[System.Management.Automation.Host.ChoiceDescription]'
            $PromptChoiceDefaultIdx = -1
            if (-not $ConnectionParameterRequired) {
                $PromptChoiceHelp = "Omit connection parameter"
                $PromptChoiceLabel = "&Ignore"
                $PromptChoiceDescr = New-Object System.Management.Automation.Host.ChoiceDescription `
                    -ArgumentList $PromptChoiceLabel, $PromptChoiceHelp
                $PromptChoices.Add($PromptChoiceDescr)
                $PromptChoiceDefaultIdx = 0
            }
            $PromptChoiceHelp = "Specify value as false"
            $PromptChoiceLabel = "&No"
            $PromptChoiceDescr = New-Object System.Management.Automation.Host.ChoiceDescription `
                -ArgumentList $PromptChoiceLabel, $PromptChoiceHelp
            if ($ConnectionParameterHasDefault -and $false -eq $ConnectionParameterDefaultValue) {
                $PromptChoiceDefaultIdx = $PromptChoices.Count
            }
            $PromptChoices.Add($PromptChoiceDescr)
            $PromptChoiceHelp = "Specify value as true"
            $PromptChoiceLabel = "&Yes"
            $PromptChoiceDescr = New-Object System.Management.Automation.Host.ChoiceDescription `
                -ArgumentList $PromptChoiceLabel, $PromptChoiceHelp
            if ($ConnectionParameterHasDefault -and $true -eq $ConnectionParameterDefaultValue) {
                $PromptChoiceDefaultIdx = $PromptChoices.Count
            }
            $PromptChoices.Add($PromptChoiceDescr)
            $PromptChoiceIdx = $Host.UI.PromptForChoice(
                $ConnectionParameterLabel,
                "${ConnectionParameterMessage}`n${ConnectionParameterName}",
                $PromptChoices,
                $PromptChoiceDefaultIdx
            )
            if (-not $ConnectionParameterRequired) {
                $PromptChoiceIdx -= 1
            }
            if ($PromptChoiceIdx -lt 0) { continue }
            $ConnectionParameterValue = $PromptChoiceIdx -eq 1
        }
        elseif ($ConnectionParameterType -eq "gatewaySetting") {
            Write-Host ""
            Write-Host -ForegroundColor White $ConnectionParameterLabel
            Write-Host $ConnectionParameterMessage
            $PowerAppsApiUrl = "https://api.powerapps.com/providers/Microsoft.PowerApps/gatewayClusters?api-version=${PowerAppsApiVersion}&`$filter=environment eq '$($PowerAppsConnectorProperties.environmentName)' and apiName eq '$($PowerAppsConnectorProperties.apiName)'"
            if ($VerbosePreference -ne 'SilentlyContinue') {
                Write-Verbose "GET $PowerAppsApiUrl"
            }
            $PowerAppsGatewaysResponse = Invoke-RestMethod -Authentication OAuth `
                -Token ((Get-AzAccessToken -ResourceUrl $PowerAppsAudience -AsSecureString).Token) `
                -Method Get `
                -Uri $PowerAppsApiUrl `
                -WebSession $PowerPlatformWebSession `
                -Verbose:$false
            [psobject]$PowerAppsGatewayClusterProperties = $PowerAppsGatewaysResponse.value |
            ForEach-Object {
                Add-Member -PassThru -InputObject $_.properties `
                    -NotePropertyName "gatewayClusterName" `
                    -NotePropertyValue $_.name |
                Add-Member -PassThru `
                    -NotePropertyName "gatewayClusterId" `
                    -NotePropertyValue $_.id
            } |
            Sort-Object -Property "displayName", "gatewayClusterName" |
            Out-GridView -PassThru -Title "Select Power Platform Gateway Cluster" |
            Select-Object -First 1
            if (-not $PowerAppsGatewayClusterProperties) {
                Write-Host "${ConnectionParameterName}: "
                continue
            }
            $ConnectionParameterValue = @{
                id = $PowerAppsGatewayClusterProperties.gatewayClusterId
            }
            Write-Host "${ConnectionParameterName}: $($PowerAppsGatewayClusterProperties.gatewayClusterName)"
        }
        else {
            $ConnectionParameterValueSpecified = $false
            $ConnectionParameterTypeSupported = switch ($ConnectionParameterType) {
                "int" { $true; break; }
                "string" { $true; break; }
                "securestring" { $true; break; }
                default { $false; break; }
            }
            if (-not $ConnectionParameterTypeSupported) { continue }
            $PromptChoices = New-Object 'System.Collections.ObjectModel.Collection[System.Management.Automation.Host.ChoiceDescription]'
            $PromptChoiceDefaultIdx = -1
            if (-not $ConnectionParameterRequired) {
                $PromptChoiceHelp = "Omit connection parameter"
                $PromptChoiceLabel = "&Ignore"
                $PromptChoiceDescr = New-Object System.Management.Automation.Host.ChoiceDescription `
                    -ArgumentList $PromptChoiceLabel, $PromptChoiceHelp
                $PromptChoiceDefaultIdx = $PromptChoices.Count
                $PromptChoices.Add($PromptChoiceDescr)
            }
            if ($ConnectionParameterHasDefault) {
                $PromptChoiceHelp = "Use default value: ${ConnectionParameterDefaultValue}"
                $PromptChoiceLabel = "&Default"
                $PromptChoiceDescr = New-Object System.Management.Automation.Host.ChoiceDescription `
                    -ArgumentList $PromptChoiceLabel, $PromptChoiceHelp
                $PromptChoiceDefaultIdx = $PromptChoices.Count
                $PromptChoices.Add($PromptChoiceDescr)
            }
            $PromptChoiceHelp = "Prompt for user specified value"
            $PromptChoiceLabel = "&Prompt"
            $PromptChoiceDescr = New-Object System.Management.Automation.Host.ChoiceDescription `
                -ArgumentList $PromptChoiceLabel, $PromptChoiceHelp
            if ($PromptChoiceDefaultIdx -eq -1) {
                $PromptChoiceDefaultIdx = $PromptChoices.Count
            }
            if ($ConnectionParameterType -eq "string" -or $ConnectionParameterType -eq "securestring") {
                $PromptChoices.Add($PromptChoiceDescr)
                $PromptChoiceHelp = "Prompt for a multi-line user specified value"
                $PromptChoiceLabel = "Prompt &multi-line"
                $PromptChoiceDescr = New-Object System.Management.Automation.Host.ChoiceDescription `
                    -ArgumentList $PromptChoiceLabel, $PromptChoiceHelp
                $PromptChoices.Add($PromptChoiceDescr)
                $PromptChoiceHelp = "Specify value from contents of a text file"
                $PromptChoiceLabel = "&File"
                $PromptChoiceDescr = New-Object System.Management.Automation.Host.ChoiceDescription `
                    -ArgumentList $PromptChoiceLabel, $PromptChoiceHelp
                $PromptChoices.Add($PromptChoiceDescr)
            }
            $PromptChoiceIdx = $Host.UI.PromptForChoice(
                $ConnectionParameterLabel,
                "${ConnectionParameterMessage}`n${ConnectionParameterName}",
                $PromptChoices,
                $PromptChoiceDefaultIdx
            )
            $PromptChoiceDescr = $PromptChoices |
            Select-Object -Index $PromptChoiceIdx
            if ($PromptChoiceDescr.Label -eq "&Ignore") { continue }
            elseif ($PromptChoiceDescr.Label -eq "&Default") {
                if ($ConnectionParameterType -is [int] -and $ConnectionParameterDefaultValue -is [string]) {
                    $ConnectionParameterDefaultValue = [int]::Parse(
                        $ConnectionParameterDefaultValue,
                        [System.Globalization.NumberStyles]::Integer
                    )
                }
                $ConnectionParameterValue = $ConnectionParameterDefaultValue
                $ConnectionParameterValueSpecified = $true
                Write-Host "${ConnectionParameterName}: ${ConnectionParameterValue}"
            }
            elseif ($PromptChoiceDescr.Label -eq "&Prompt") {
                switch ($ConnectionParameterType) {
                    "int" {
                        $ConnectionParameterValue = [int]::Parse(
                            (Read-Host -Prompt $ConnectionParameterName),
                            [System.Globalization.NumberStyles]::Integer
                        )
                        break
                    }
                    "string" {
                        $ConnectionParameterValue = Read-Host `
                            -Prompt $ConnectionParameterName
                        break
                    }
                    "securestring" {
                        $ConnectionParameterValue = Read-Host -MaskInput `
                            -Prompt $ConnectionParameterName
                        break
                    }
                }
            }
            elseif ($PromptChoiceDescr.Label -eq "Prompt &multi-line") {
                $ConnectionParameterReadHostArgs = @{
                    Prompt    = "#"
                    MaskInput = ($ConnectionParameterType -eq "securestring")
                }
                $ConnectionParameterValue = ""
                Write-Host "${ConnectionParameterName}: (multi-line input completes with two consecutive empty lines)"
                $ConnectionParameterEmptyLines = 0
                do {
                    $ConnectionParameterValueLine = Read-Host @ConnectionParameterReadHostArgs
                    if ($ConnectionParameterValueLine) {
                        if ($ConnectionParameterEmptyLines -gt 0) {
                            $ConnectionParameterValue += "`n"
                        }
                        $ConnectionParameterEmptyLines = 0
                        if ($ConnectionParameterValue) {
                            $ConnectionParameterValue += "`n${ConnectionParameterValueLine}"
                        }
                        else {
                            $ConnectionParameterValue = $ConnectionParameterValueLine
                        }
                    }
                    else {
                        $ConnectionParameterEmptyLines += 1
                    }
                } until ($ConnectionParameterEmptyLines -eq 2)
            }
            elseif ($PromptChoiceDescr.Label -eq "&File") {
                $ConnectionParameterValuePath = Read-Host `
                    -Prompt "${ConnectionParameterName} <"
                [string[]]$ConnectionParameterValueLines = Get-Content `
                    -Path $ConnectionParameterValuePath `
                    -ErrorAction ([System.Management.Automation.ActionPreference]::Stop)
                $ConnectionParameterValue = (
                    $ConnectionParameterValueLines -join "`n"
                ).Trim()
            }
        }

        if ($ConnectionParameterSet) {
            $ConnectionParameterValues = Add-Member -PassThru `
                -InputObject $ConnectionParameterValues `
                -NotePropertyName $ConnectionParameterName `
                -NotePropertyValue @{ value = $ConnectionParameterValue }
        }
        else {
            $ConnectionParameterValues = Add-Member -PassThru `
                -InputObject $ConnectionParameterValues `
                -NotePropertyName $ConnectionParameterName `
                -NotePropertyValue $ConnectionParameterValue
        }
    }

    if ($ConnectionParameterProperties.Count -gt 0) {
        Write-Host ""
    }
    $ConnectionDisplayName = Read-Host -Prompt "Connection Display Name"
    if ($ConnectionDisplayName) {
        $ConnectionProperties = Add-Member -PassThru `
            -InputObject $ConnectionProperties `
            -NotePropertyName "displayName" `
            -NotePropertyValue $ConnectionDisplayName
    }
    if ($ConnectionParameterSet) {
        $ConnectionProperties = Add-Member -PassThru `
            -InputObject $ConnectionProperties `
            -NotePropertyName "connectionParametersSet" `
            -NotePropertyValue @{
            name   = $ConnectionParameterSet.name
            values = $ConnectionParameterValues
        }
    }
    else {
        $ConnectionProperties = Add-Member -PassThru `
            -InputObject $ConnectionProperties `
            -NotePropertyName "connectionParameters" `
            -NotePropertyValue $ConnectionParameterValues
    }
    $ConnectionProperties = Add-Member -PassThru `
        -InputObject $ConnectionProperties `
        -NotePropertyName "environment" `
        -NotePropertyValue @{ name = $PowerAppsConnectorProperties.environmentName }

    $ConnectionCreatePayload = ConvertTo-Json -Depth 50 `
        -InputObject @{ properties = $ConnectionProperties }
    $PowerAppsApiUrl = "https://api.powerapps.com/providers/Microsoft.PowerApps/apis/$($PowerAppsConnectorProperties.apiName)/connections/${ConnectionId}?api-version=${PowerAppsApiVersion}&`$filter=environment eq '$($PowerAppsConnectorProperties.environmentName)'"
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Host ""
        Write-Verbose "PUT $PowerAppsApiUrl"
    }
    $PowerAppsConnectionCreateResponse = Invoke-RestMethod -Authentication OAuth `
        -Token ((Get-AzAccessToken -ResourceUrl $PowerAppsAudience -AsSecureString).Token) `
        -Method Put -Uri $PowerAppsApiUrl `
        -Headers @{ "Content-Type" = "application/json; charset=utf-8" } `
        -Body $ConnectionCreatePayload `
        -WebSession $PowerPlatformWebSession `
        -Verbose:$false
    $PowerAppsConnectionProperties = Add-Member -PassThru `
        -InputObject $PowerAppsConnectionCreateResponse.properties `
        -NotePropertyName "connectionName" `
        -NotePropertyValue $PowerAppsConnectionCreateResponse.name
    if (-not $Consent) {
        Write-Output -InputObject $PowerAppsConnectionProperties
        return
    }
    $ConnectionConsentPayload = ConvertTo-Json -Depth 50 `
        -InputObject @{ redirectUrl = "https://httpbin.org/anything" }
    $PowerAppsApiUrl = "https://api.powerapps.com/providers/Microsoft.PowerApps/apis/$($PowerAppsConnectorProperties.apiName)/connections/${ConnectionId}/getConsentLink?api-version=${PowerAppsApiVersion}&`$filter=environment eq '$($PowerAppsConnectorProperties.environmentName)'"
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
    $PowerAppsApiUrl = "https://api.powerapps.com/providers/Microsoft.PowerApps/apis/$($PowerAppsConnectorProperties.apiName)/connections/${ConnectionId}?api-version=${PowerAppsApiVersion}&`$filter=environment eq '$($PowerAppsConnectorProperties.environmentName)'"
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
    Write-Output -InputObject $PowerAppsConnectionProperties
}