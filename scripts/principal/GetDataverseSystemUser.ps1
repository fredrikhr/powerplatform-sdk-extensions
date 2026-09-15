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
    [ValidateSet("EntraObjectId", "UserPrincipalName", "EmailAddress", "ApplicationId", "EntityId")]
    [string]$SearchMode,
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$SearchInput
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

    $DataverseApiSelectExpression = "systemuserid,fullname,domainname,internalemailaddress,azureactivedirectoryobjectid,applicationid"
}

process {
    $DataverseApiKeyExpression = $null
    switch ($SearchMode) {
        "EntraObjectId" {
            $DataverseApiKeyExpression = "azureactivedirectoryobjectid=${SearchInput}"
            break
        }
        "EntityId" {
            $DataverseApiKeyExpression = $SearchInput
            break
        }
    }
    $DataverseApiFilterExpression = $null
    switch ($SearchMode) {
        "UserPrincipalName" {
            $DataverseApiFilterExpression = "domainname eq '${SearchInput}'"
            break
        }
        "EmailAddress" {
            $DataverseApiFilterExpression = "internalemailaddress eq '${SearchInput}'"
            break
        }
        "ApplicationId" {
            $DataverseApiFilterExpression = "applicationid eq '${SearchInput}'"
            break
        }
    }

    $DataverseSystemUserRecord = $null
    if ($DataverseApiKeyExpression) {
        $DataverseApiUri = New-Object uri $DataverseApiBase, "systemusers(${DataverseApiKeyExpression})?`$select=${DataverseApiSelectExpression}"
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "GET $DataverseApiUri"
        }
        $DataverseApiResponse = Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
            -Method Get -Uri $DataverseApiUri `
            -Headers $ODataHeaders `
            -WebSession $PowerPlatformWebSession `
            -Verbose:$false
        $DataverseSystemUserRecord = $DataverseApiResponse
    }
    else {
        $DataverseApiUri = New-Object uri $DataverseApiBase, "systemusers?`$select=${DataverseApiSelectExpression}&`$filter=$($DataverseApiFilterExpression ?? "*")"
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
                $DataverseSystemUserRecord = $DataverseApiResponse.value
                | Select-Object -First 1
                break
            }
            default {
                $DataverseSystemUserRecord = $DataverseApiResponse.value
                | Out-GridView -PassThru -Title "Select Dataverse System User"
                | Select-Object -First 1
            }
        }
    }

    $DataverseSystemUserRecord
    | Add-Member -PassThru -Force -NotePropertyMembers @{
        pwrplatf_environmentName = $PowerPlatformEnvironment.environmentName
        dataverse_instanceUrl    = $DataverseInstanceUri.ToString()
    }
    | Write-Output
}