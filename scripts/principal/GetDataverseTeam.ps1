#Requires -Version 7.0
#Requires -Modules @{ ModuleName = "Az.Accounts"; ModuleVersion = "5.0.0" }
[CmdletBinding()]
param (
    [Parameter(Mandatory = $false)]
    [AllowNull()][AllowEmptyString()]
    [string]$EnvironmentId,
    [Parameter(Mandatory = $false)]
    [AllowNull()]
    [uri]$DataverseUri,
    [Parameter(Mandatory = $false)]
    [AllowNull()][AllowEmptyString()]
    [string]$SolutionUniqueName,
    [Parameter(Mandatory = $true)]
    [ValidateSet("EntraObjectId", "EmailAddress", "EntityId")]
    [string]$SearchMode,
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$SearchInput,
    [Parameter(Mandatory = $false)]
    [ValidateSet("Owners", "MembersAndGuests")]
    [string]$MembershipType = "MembersAndGuests"
)

begin {
    $PowerPlatformEnvironment = $null
    if ($DataverseUri) {
        $DataverseMetadataInfo = [PSCustomObject]@{
            instanceUrl    = $DataverseUri
            instanceApiUrl = $DataverseUri
            version        = "9.2"
        }
    }
    else {
        $PowerPlatformEnvironmentParams = @{}
        if ($EnvironmentId) {
            $PowerPlatformEnvironmentParams["EnvironmentId"] = $EnvironmentId
        }
        [ValidateNotNull()]
        [psobject]$PowerPlatformEnvironment = & (
            Join-Path -Resolve (
                Join-Path -Resolve (Join-Path -Resolve $PSScriptRoot "..") "env"
            ) "GetEnvironmentProperties.ps1"
        ) @PowerPlatformEnvironmentParams |
        Select-Object -First 1
        [ValidateNotNull()][psobject]$DataverseMetadataInfo = $PowerPlatformEnvironment.linkedEnvironmentMetadata
    }
    [ValidateNotNull()][uri]$DataverseInstanceUri = $DataverseMetadataInfo.instanceUrl
    [string]$DataverseTokenAudience = $DataverseInstanceUri.GetLeftPart([System.UriPartial]::Authority)
    [ValidateNotNull()][uri]$DataverseApiRootUri = $DataverseMetadataInfo.instanceApiUrl
    [uri]$DataverseApiBase = New-Object uri $DataverseApiRootUri, "/api/data/v$($DataverseMetadataInfo.version)/`$metadata"
    [hashtable]$ODataHeaders = @{
        "Accept"           = "application/json"
        "OData-Version"    = "4.0"
        "OData-MaxVersion" = "4.01"
        "Prefer"           = "odata.include-annotations=*"
    }

    if (-not $PowerPlatformEnvironment) {
        $DataverseApiUri = New-Object uri $DataverseApiBase, "RetrieveCurrentOrganization(AccessType=@AccessType)?@AccessType=Microsoft.Dynamics.CRM.EndpointAccessType'Default'"
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "GET $DataverseApiUri"
        }
        $DataverseApiResponse = Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
            -Method Get -Uri $DataverseApiUri `
            -Headers $ODataHeaders `
            -WebSession $PowerPlatformWebSession `
            -Verbose:$false
        $PowerPlatformEnvironment = [PSCustomObject]@{
            environmentName = $DataverseApiResponse.Detail.EnvironmentId
        }
    }

    $DataverseApiSelectExpression = "teamid,teamtype,name,emailaddress,azureactivedirectoryobjectid,membershiptype"

    $DataverseTeamMembershipTypeValue = 0
    switch ($MembershipType) {
        "MembersAndGuests" {
            $DataverseTeamMembershipTypeValue = 0
            break
        }
        "Members" {
            $DataverseTeamMembershipTypeValue = 1
            break
        }
        "Owners" {
            $DataverseTeamMembershipTypeValue = 2
            break
        }
        "Guests" {
            $DataverseTeamMembershipTypeValue = 3
            break
        }
    }
}

process {
    $DataverseApiKeyExpression = $null
    switch ($SearchMode) {
        "EntraObjectId" {
            $DataverseApiKeyExpression = "azureactivedirectoryobjectid=${SearchInput},membershiptype=${DataverseTeamMembershipTypeValue}"
            break
        }
        "EntityId" {
            $DataverseApiKeyExpression = $SearchInput
            break
        }
    }
    $DataverseApiFilterExpression = $null
    switch ($SearchMode) {
        "EmailAddress" {
            $DataverseApiFilterExpression = "emailaddress eq '${SearchInput}'"
            break
        }
    }

    $DataverseTeamRecord = $null
    if ($DataverseApiKeyExpression) {
        $DataverseApiUri = New-Object uri $DataverseApiBase, "teams(${DataverseApiKeyExpression})?`$select=${DataverseApiSelectExpression}"
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "GET $DataverseApiUri"
        }
        $DataverseApiResponse = Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
            -Method Get -Uri $DataverseApiUri `
            -Headers $ODataHeaders `
            -WebSession $PowerPlatformWebSession `
            -Verbose:$false
        $DataverseTeamRecord = $DataverseApiResponse
    }
    else {
        $DataverseApiUri = New-Object uri $DataverseApiBase, "teams?`$select=${DataverseApiSelectExpression}&`$filter=$($DataverseApiFilterExpression ?? "*")"
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "GET $DataverseApiUri"
        }
        $DataverseApiResponse = Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
            -Method Get -Uri $DataverseApiUri `
            -Headers $ODataHeaders `
            -WebSession $PowerPlatformWebSession `
            -Verbose:$false
        switch ($DataverseApiResponse.value.Length) {
            0 { break }
            1 {
                $DataverseTeamRecord = $DataverseApiResponse.value
                | Select-Object -First 1
                break
            }
            default {
                $DataverseTeamRecord = $DataverseApiResponse.value
                | Out-GridView -PassThru -Title "Select Dataverse Team"
                | Select-Object -First 1
            }
        }
    }

    $DataverseTeamRecord
    | Add-Member -PassThru -Force -NotePropertyMembers @{
        pwrplatf_environmentName = $PowerPlatformEnvironment.environmentName
        dataverse_instanceUrl    = $DataverseInstanceUri.ToString()
    }
    | Write-Output
}