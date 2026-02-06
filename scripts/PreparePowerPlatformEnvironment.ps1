#Requires -Version 7.0
#Requires -Modules @{ ModuleName = "Az.Accounts"; ModuleVersion = "5.0.0" }
[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [ValidateNotNull()]
    [uri]$DataverseUri,
    [Parameter(Mandatory = $true)]
    [ValidateNotNull()]
    [cultureinfo]$CultureInfo,
    [Parameter(Mandatory = $false)]
    [string]$TimeZoneName,
    [Parameter(Mandatory = $true)]
    [string]$PhoneCountryCode
)

begin {
    $CultureLcid = $CultureInfo.LCID
    $CultureRegionInfo = New-Object System.Globalization.RegionInfo $CultureLcid -ErrorAction Stop
    $CurrencyIsoCode = $CultureRegionInfo.ISOCurrencySymbol

    [ValidateNotNull()]
    $TimeZoneInfo = if ($TimeZoneName) {
        [System.TimeZoneInfo]::FindSystemTimeZoneById($TimeZoneName)
    }
    else {
        [System.TimeZoneInfo]::GetSystemTimeZones() |
        Out-GridView -Title "Select Time Zone" -PassThru |
        Select-Object -First 1
    }

    $DataverseTokenAudience = $DataverseUri.GetLeftPart([System.UriPartial]::Authority)
    [void](Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -ErrorAction Stop)
    [uri]$DataverseApiUrl = New-Object uri $DataverseUri, "/api/data/v9.2/`$metadata"
    $DataverseHeaders = @{
        "OData-Version"    = "4.0"
        "OData-MaxVersion" = "4.01"
        "Prefer"           = "odata.include-annotations=*"
        "Content-Type"     = "application/json; charset=utf-8"
    }
}

process {
    Format-List -InputObject $CultureInfo -Property "Name", "DisplayName", "EnglishName", "LCID", "IetfLanguageTag", "TwoLetterISOLanguageName", "ThreeLetterISOLanguageName", "ThreeLetterWindowsLanguageName"

    [uri]$DataverseRequestUri = New-Object uri $DataverseApiUrl, "WhoAmI"
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Verbose "GET $DataverseRequestUri"
    }
    $WhoAmIResponse = Invoke-RestMethod -Authentication OAuth `
        -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
        -Method Get `
        -Uri $DataverseRequestUri `
        -Headers $DataverseHeaders `
        -SessionVariable "DataverseWebSession" `
        -Verbose:$false
    [ValidateNotNullOrEmpty()][string]$OrganizationId = $WhoAmIResponse.OrganizationId

    Write-Host "Checking whether requsted LCID $CultureLcid is provisioned in Dataverse"
    [uri]$DataverseRequestUri = New-Object uri $DataverseApiUrl, "RetrieveProvisionedLanguages()"
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Verbose "GET $DataverseRequestUri"
    }
    $LanguagesResponse = Invoke-RestMethod -Authentication OAuth `
        -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
        -Method Get `
        -Uri $DataverseRequestUri `
        -Headers $DataverseHeaders `
        -WebSession $DataverseWebSession `
        -Verbose:$false
    $LanguagesResponse | Format-List -Property "RetrieveProvisionedLanguages"
    [ValidateNotNullOrEmpty()][int[]]$ProvisionedLanguages = $LanguagesResponse.RetrieveProvisionedLanguages
    if ($CultureLcid -notin $ProvisionedLanguages) {
        Write-Host "Provisioning LCID $CultureLcid in Dataverse"
        [uri]$DataverseRequestUri = New-Object uri $DataverseApiUrl, "ProvisionLanguageAsync()"
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "POST $DataverseRequestUri"
        }
        $ProvisionLangaugeResponse = Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
            -Method Post `
            -Uri $DataverseRequestUri `
            -Headers $DataverseHeaders `
            -Body (ConvertTo-Json -InputObject @{ Language = $CultureLcid }) `
            -WebSession $DataverseWebSession `
            -Verbose:$false
        [ValidateNotNullOrEmpty()][string]$ProvisionLangaugeOperationId = $ProvisionLangaugeResponse.AsyncOperationId
        [uri]$DataverseRequestUri = New-Object uri $DataverseApiUrl, "asyncoperations(${ProvisionLangaugeOperationId})?`$select=asyncoperationid,name,operationtype,messagename,message,friendlymessage,errorcode,statecode,statuscode,createdon,startedon,modifiedon,completedon,executiontimespan"
        do {
            [void](Start-Sleep -Seconds 5)
            if ($VerbosePreference -ne 'SilentlyContinue') {
                Write-Verbose "GET $DataverseRequestUri"
            }
            $ProvisionLanguageOperationRecord = Invoke-RestMethod -Authentication OAuth `
                -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
                -Method Get `
                -Uri $DataverseRequestUri `
                -Headers $DataverseHeaders `
                -WebSession $DataverseWebSession `
                -Verbose:$false
            $ProvisionLanguageOperationRecord | Format-List -Property "*", @{ N = "timetocompletion"; E = { if ($_.completedon) { $_.completedon - $_.startedon } else { $null } } }, @{ N = "timesincecreation"; E = { ($_.completedon ?? [datetime]::UtcNow) - $_.createdon } }
        } until ($ProvisionLanguageOperationRecord.statecode -eq 3) # statecode 3 = Completed
        Write-Host "Provisioning complete"
    }

    Write-Host "Updating Organization Settings"
    $DataverseRequestUri = New-Object uri $DataverseApiUrl, "organizations(${OrganizationId})"
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Verbose "PATCH $DataverseRequestUri"
    }
    [void](
        Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
            -Method Patch `
            -Uri $DataverseRequestUri `
            -Headers $DataverseHeaders `
            -Body (ConvertTo-Json -InputObject @{
                localeid           = $CultureLcid
                defaultcountrycode = $PhoneCountryCode
            } -Depth 20) `
            -WebSession $DataverseWebSession `
            -Verbose:$false
    )

    Write-Host "Determining appropriate user settings"
    $DataverseRequestUri = New-Object uri $DataverseApiUrl, "timezonedefinitions?`$select=timezonecode,userinterfacename,standardname&`$filter=standardname eq '$($TimeZoneInfo.Id)'&`$top=1"
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Verbose "GET $DataverseRequestUri"
    }
    $TimeZoneDefinitionResponse = Invoke-RestMethod -Authentication OAuth `
        -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
        -Method Get `
        -Uri $DataverseRequestUri `
        -Headers $DataverseHeaders `
        -WebSession $DataverseWebSession `
        -Verbose:$false
    $TimeZoneDefinitionResponse.value | Format-List -Property "timezonedefinitionid", "standardname", "userinterfacename", "timezonecode"
    [ValidateNotNull()]$TimeZoneDefinition = $TimeZoneDefinitionResponse.value |
    Select-Object -First 1
    [int]$TimeZoneCode = $TimeZoneDefinition.timezonecode

    $DataverseRequestUri = New-Object uri $DataverseApiUrl, "transactioncurrencies?`$select=transactioncurrencyid,isocurrencycode,currencyname,currencysymbol&`$filter=isocurrencycode eq '${CurrencyIsoCode}'&`$top=1"
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Verbose "GET $DataverseRequestUri"
    }
    $CurrenciesResponse = Invoke-RestMethod -Authentication OAuth `
        -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
        -Method Get `
        -Uri $DataverseRequestUri `
        -Headers $DataverseHeaders `
        -WebSession $DataverseWebSession `
        -Verbose:$false
    $CurrenciesResponse.value | Format-List -Property "transactioncurrencyid", "currencyname", "currencysymbol", "isocurrencycode"
    [ValidateNotNull()]$CurrencyDefinition = $CurrenciesResponse.value |
    Select-Object -First 1
    [ValidateNotNullOrEmpty()][string]$CurrencyId = $CurrencyDefinition.transactioncurrencyid
    $CurrencyRefId = "/transactioncurrencies(${CurrencyId})"

    $DataverseRequestUri = New-Object uri $DataverseApiUrl, "usersettingscollection?`$select=systemuserid&`$filter=systemuserid_systemuser/azureactivedirectoryobjectid ne null"
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Verbose "GET $DataverseRequestUri"
    }
    $UserSettingsCollectionResponse = Invoke-RestMethod -Authentication OAuth `
        -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
        -Method Get `
        -Uri $DataverseRequestUri `
        -Headers $DataverseHeaders `
        -WebSession $DataverseWebSession `
        -Verbose:$false
    [ValidateNotNull()][PSObject[]]$UserSettingsReferences = $UserSettingsCollectionResponse.value

    Write-Host "Updating user settings for $($UserSettingsReferences.Length) user$(if ($UserSettingsReferences.Length -ne 1) { "s" })"
    $UserSettingsPatchBody = ConvertTo-Json -InputObject @{
        localeid                           = $CultureLcid
        defaultcountrycode                 = $PhoneCountryCode
        timezonecode                       = $TimeZoneCode
        "transactioncurrencyid@odata.bind" = $CurrencyRefId
        isdefaultcountrycodecheckenabled   = if ($PhoneCountryCode) { $true } else { $false }
    }
    foreach ($UserSettingsReferenceRecord in $UserSettingsReferences) {
        $DataverseRequestUri = New-Object uri $DataverseApiUrl, "usersettingscollection($($UserSettingsReferenceRecord.systemuserid))"
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "PATCH $DataverseRequestUri"
        }
        [void](
            Invoke-RestMethod -Authentication OAuth `
                -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
                -Method Patch `
                -Uri $DataverseRequestUri `
                -Headers $DataverseHeaders `
                -Body $UserSettingsPatchBody `
                -WebSession $DataverseWebSession `
                -Verbose:$false
        )
    }
}