#Requires -Version 7.0
#Requires -Modules @{ ModuleName = "Az.Accounts"; ModuleVersion = "5.0.0" }
[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [ValidateNotNull()]
    [uri]$DataverseUri
)

begin {
    $DataverseTokenAudience = $DataverseUri.GetLeftPart([System.UriPartial]::Authority)
    [void](Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -ErrorAction Stop)
    [uri]$DataverseApiUrl = New-Object uri $DataverseUri, "/api/data/v9.2/`$metadata"
    $DataverseHeaders = @{
        "OData-Version"    = "4.0"
        "OData-MaxVersion" = "4.01"
        "Prefer"           = "odata.include-annotations=*"
        "Content-Type"     = "application/json; charset=utf-8"
    }
    [ValidateNotNull()]$OrganizationSettings = ConvertFrom-Json (
        Get-Content -LiteralPath (
            Join-Path $PSScriptRoot "DataverseOrganizationLocaleSettings.json"
        ) -Raw -Encoding utf8NoBOM
    )
    [ValidateNotNull()][pscustomobject]$UserSettingsTemplate = ConvertFrom-Json (
        Get-Content -LiteralPath (
            Join-Path $PSScriptRoot "DataverseUserSettings.json"
        ) -Raw -Encoding utf8NoBOM
    )
}

process {
    [uri]$DataverseRequestUri = New-Object uri $DataverseApiUrl, "WhoAmI"
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Verbose "URI $DataverseRequestUri"
    }
    $WhoAmIResponse = Invoke-RestMethod -Authentication OAuth `
        -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
        -Method Get `
        -Uri $DataverseRequestUri `
        -Headers $DataverseHeaders `
        -SessionVariable "DataverseWebSession" `
        -Verbose:($VerbosePreference -ne 'SilentlyContinue')
    [ValidateNotNullOrEmpty()][string]$OrganizationId = $WhoAmIResponse.OrganizationId

    [uri]$DataverseRequestUri = New-Object uri $DataverseApiUrl, "RetrieveProvisionedLanguages()"
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Verbose "URI $DataverseRequestUri"
    }
    $LanguagesResponse = Invoke-RestMethod -Authentication OAuth `
        -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
        -Method Get `
        -Uri $DataverseRequestUri `
        -Headers $DataverseHeaders `
        -WebSession $DataverseWebSession `
        -Verbose:($VerbosePreference -ne 'SilentlyContinue')
    $LanguagesResponse | Format-List -Property "RetrieveProvisionedLanguages"
    [ValidateNotNullOrEmpty()][int[]]$ProvisionedLanguages = $LanguagesResponse.RetrieveProvisionedLanguages
    if ($OrganizationSettings.localeid -notin $ProvisionedLanguages) {
        [uri]$DataverseRequestUri = New-Object uri $DataverseApiUrl, "ProvisionLanguageAsync()"
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "URI $DataverseRequestUri"
        }
        $ProvisionLangaugeResponse = Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
            -Method Post `
            -Uri $DataverseRequestUri `
            -Headers $DataverseHeaders `
            -Body (ConvertTo-Json -InputObject @{ Language = $OrganizationSettings.localeid }) `
            -WebSession $DataverseWebSession `
            -Verbose:($VerbosePreference -ne 'SilentlyContinue')
        [ValidateNotNullOrEmpty()][string]$ProvisionLangaugeOperationId = $ProvisionLangaugeResponse.AsyncOperationId
        [uri]$DataverseRequestUri = New-Object uri $DataverseApiUrl, "asyncoperations(${ProvisionLangaugeOperationId})?`$select=asyncoperationid,name,operationtype,messagename,message,friendlymessage,errorcode,statecode,statuscode,createdon,startedon,modifiedon,completedon,executiontimespan"
        do {
            [void](Start-Sleep -Seconds 5)
            if ($VerbosePreference -ne 'SilentlyContinue') {
                Write-Verbose "URI $DataverseRequestUri"
            }
            $ProvisionLanguageOperationRecord = Invoke-RestMethod -Authentication OAuth `
                -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
                -Method Get `
                -Uri $DataverseRequestUri `
                -Headers $DataverseHeaders `
                -WebSession $DataverseWebSession `
                -Verbose:($VerbosePreference -ne 'SilentlyContinue')
            $ProvisionLanguageOperationRecord | Format-List -Property "*", @{ N = "timetocompletion"; E = { if ($_.completedon) { $_.completedon - $_.startedon } else { $null } } }, @{ N = "timesincecreation"; E = { if ($_.completedon) { $_.completedon - $_.createdon } else { $null } } }
        } until ($ProvisionLanguageOperationRecord.statecode -eq 3) # statecode 3 = Completed
        [uri]$DataverseRequestUri = New-Object uri $DataverseApiUrl, "RetrieveProvisionedLanguages()"
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "URI $DataverseRequestUri"
        }
        $LanguagesResponse = Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
            -Method Get `
            -Uri $DataverseRequestUri `
            -Headers $DataverseHeaders `
            -WebSession $DataverseWebSession `
            -Verbose:($VerbosePreference -ne 'SilentlyContinue')
        $LanguagesResponse | Format-List -Property "RetrieveProvisionedLanguages"
    }

    $DataverseRequestUri = New-Object uri $DataverseApiUrl, "organizations(${OrganizationId})"
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Verbose "URI $DataverseRequestUri"
    }
    [void](
        Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
            -Method Patch `
            -Uri $DataverseRequestUri `
            -Headers $DataverseHeaders `
            -Body (ConvertTo-Json -InputObject $OrganizationSettings -Depth 20) `
            -WebSession $DataverseWebSession `
            -Verbose:($VerbosePreference -ne 'SilentlyContinue')
    )

    $DataverseRequestUri = New-Object uri $DataverseApiUrl, "timezonedefinitions?`$select=timezonecode,userinterfacename,standardname&`$filter=standardname eq '$($UserSettingsTemplate.timezonename)'&`$top=1"
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Verbose "URI $DataverseRequestUri"
    }
    $TimeZoneDefinitionResponse = Invoke-RestMethod -Authentication OAuth `
        -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
        -Method Get `
        -Uri $DataverseRequestUri `
        -Headers $DataverseHeaders `
        -WebSession $DataverseWebSession `
        -Verbose:($VerbosePreference -ne 'SilentlyContinue')
    $TimeZoneDefinitionResponse.value | Format-List -Property "*"
    [ValidateNotNull()]$TimeZoneDefinition = $TimeZoneDefinitionResponse.value |
    Select-Object -First 1
    [int]$TimeZoneCode = $TimeZoneDefinition.timezonecode
    $UserSettingsTemplate.PSObject.Properties.Remove("timezonename")
    [void](Add-Member -InputObject $UserSettingsTemplate -NotePropertyName "timezonecode" -NotePropertyValue $TimeZoneCode)

    $DataverseRequestUri = New-Object uri $DataverseApiUrl, "transactioncurrencies?`$select=transactioncurrencyid,isocurrencycode,currencyname,currencysymbol&`$filter=isocurrencycode eq '$($UserSettingsTemplate.currencycode)'&`$top=1"
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Verbose "URI $DataverseRequestUri"
    }
    $CurrenciesResponse = Invoke-RestMethod -Authentication OAuth `
        -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
        -Method Get `
        -Uri $DataverseRequestUri `
        -Headers $DataverseHeaders `
        -WebSession $DataverseWebSession `
        -Verbose:($VerbosePreference -ne 'SilentlyContinue')
    $CurrenciesResponse.value | Format-List -Property "*"
    [ValidateNotNull()]$CurrencyDefinition = $CurrenciesResponse.value |
    Select-Object -First 1
    [ValidateNotNullOrEmpty()][string]$CurrencyId = $CurrencyDefinition.transactioncurrencyid
    $UserSettingsTemplate.PSObject.Properties.Remove("currencycode")
    [void](Add-Member -InputObject $UserSettingsTemplate -NotePropertyName "transactioncurrencyid@odata.bind" -NotePropertyValue "/transactioncurrencies(${CurrencyId})")

    $DataverseRequestUri = New-Object uri $DataverseApiUrl, "usersettingscollection?`$select=systemuserid&`$filter=systemuserid_systemuser/azureactivedirectoryobjectid ne null"
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Verbose "URI $DataverseRequestUri"
    }
    $UserSettingsCollectionResponse = Invoke-RestMethod -Authentication OAuth `
        -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
        -Method Get `
        -Uri $DataverseRequestUri `
        -Headers $DataverseHeaders `
        -WebSession $DataverseWebSession `
        -Verbose:($VerbosePreference -ne 'SilentlyContinue')
    [ValidateNotNullOrEmpty()][PSObject[]]$UserSettingsReferences = $UserSettingsCollectionResponse.value
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Verbose "Updating $($UserSettingsReferences.Length) user settings"
    }
    $UserSettingsPatchBody = ConvertTo-Json -InputObject $UserSettingsTemplate
    foreach ($UserSettingsReferenceRecord in $UserSettingsReferences) {
        $DataverseRequestUri = New-Object uri $DataverseApiUrl, "usersettingscollection($($UserSettingsReferenceRecord.systemuserid))"
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "URI $DataverseRequestUri"
        }
        [void](
            Invoke-RestMethod -Authentication OAuth `
                -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
                -Method Patch `
                -Uri $DataverseRequestUri `
                -Headers $DataverseHeaders `
                -Body $UserSettingsPatchBody `
                -WebSession $DataverseWebSession `
                -Verbose:($VerbosePreference -ne 'SilentlyContinue')
        )
    }
}