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
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet(
        "UserByUpn",
        "UserByEntraObjectId",
        "UserByEmailAddress",
        "UserByDataverseEntityId",
        "GroupMembersByEntraGroupId",
        "GroupOwnersByEntraGroupId",
        "ApplicationByClientId",
        "ServicePrincipalByEntraObjectId"
    )]
    [string]$PrincipalType,
    [Parameter(Mandatory = $true, Position = 1)]
    [string]$PrincipalId
)

begin {
    $DataversePrincipalParams = @{
        EnvironmentId = $EnvironmentId
        DataverseUri  = $DataverseUri
        SearchInput   = $PrincipalId
    }
    [string]$DataversePrincipalScriptName = $null
    [string]$DataversePrincipalEntitySetName = $null
    [string]$DataversePrincipalPrimaryAttributeName = $null
    switch ($PrincipalType) {
        "UserByUpn" {
            $DataversePrincipalScriptName = "GetDataverseSystemUser.ps1"
            $DataversePrincipalEntitySetName = "systemusers"
            $DataversePrincipalPrimaryAttributeName = "systemuserid"
            $DataversePrincipalParams["SearchMode"] = "UserPrincipalName"
            break
        }
        "UserByEntraObjectId" {
            $DataversePrincipalScriptName = "GetDataverseSystemUser.ps1"
            $DataversePrincipalEntitySetName = "systemusers"
            $DataversePrincipalPrimaryAttributeName = "systemuserid"
            $DataversePrincipalParams["SearchMode"] = "EntraObjectId"
            break
        }
        "UserByEmailAddress" {
            $DataversePrincipalScriptName = "GetDataverseSystemUser.ps1"
            $DataversePrincipalEntitySetName = "systemusers"
            $DataversePrincipalPrimaryAttributeName = "systemuserid"
            $DataversePrincipalParams["SearchMode"] = "EmailAddress"
            break
        }
        "UserByDataverseEntityId" {
            $DataversePrincipalScriptName = "GetDataverseSystemUser.ps1"
            $DataversePrincipalEntitySetName = "systemusers"
            $DataversePrincipalPrimaryAttributeName = "systemuserid"
            $DataversePrincipalParams["SearchMode"] = "EntityId"
            break
        }
        "GroupMembersByEntraGroupId" {
            $DataversePrincipalScriptName = "GetDataverseTeam.ps1"
            $DataversePrincipalEntitySetName = "teams"
            $DataversePrincipalPrimaryAttributeName = "teamid"
            $DataversePrincipalParams["SearchMode"] = "EntraObjectId"
            $DataversePrincipalParams["MembershipType"] = "MembersAndGuests"
            break
        }
        "GroupOwnersByEntraGroupId" {
            $DataversePrincipalScriptName = "GetDataverseTeam.ps1"
            $DataversePrincipalEntitySetName = "teams"
            $DataversePrincipalPrimaryAttributeName = "teamid"
            $DataversePrincipalParams["SearchMode"] = "EntraObjectId"
            $DataversePrincipalParams["MembershipType"] = "Owners"
            break
        }
        "ApplicationByClientId" {
            $DataversePrincipalScriptName = "GetDataverseSystemUser.ps1"
            $DataversePrincipalEntitySetName = "systemusers"
            $DataversePrincipalPrimaryAttributeName = "systemuserid"
            $DataversePrincipalParams["SearchMode"] = "ApplicationId"
            break
        }
        "ServicePrincipalByEntraObjectId" {
            $DataversePrincipalScriptName = "GetDataverseSystemUser.ps1"
            $DataversePrincipalEntitySetName = "systemusers"
            $DataversePrincipalPrimaryAttributeName = "systemuserid"
            $DataversePrincipalParams["SearchMode"] = "EntraObjectId"
            break
        }
    }
    [ValidateNotNull()]
    [psobject]$DataversePrincipalRecord = & (
        Join-Path -Resolve (
            Join-Path -Resolve (Join-Path -Resolve $PSScriptRoot "..") "principal"
        ) $DataversePrincipalScriptName
    ) @DataversePrincipalParams
    $DataversePrincipalReference = "${DataversePrincipalEntitySetName}($($DataversePrincipalRecord.$DataversePrincipalPrimaryAttributeName))"

    [ValidateNotNull()][uri]$DataverseInstanceUri = $DataversePrincipalRecord.dataverse_instanceUrl
    [string]$DataverseTokenAudience = $DataverseInstanceUri.GetLeftPart([System.UriPartial]::Authority)
    [uri]$DataverseApiBase = New-Object uri $DataverseInstanceUri, "/api/data/v9.2/`$metadata"

    [hashtable]$ODataHeaders = @{
        "Accept"           = "application/json"
        "OData-Version"    = "4.0"
        "OData-MaxVersion" = "4.01"
        "Prefer"           = "odata.include-annotations=*"
    }

    $DataverseComponentParams = @{
        DataverseUri       = $DataverseInstanceUri
        SolutionUniqueName = $SolutionUniqueName
        SelectExpression   = "msdyn_solutionid,msdyn_componentlogicalname,msdyn_primaryidattribute,msdyn_displayname,msdyn_objectid,msdyn_schemaname,msdyn_name,msdyn_componenttype,msdyn_componenttypename,msdyn_ismanaged,msdyn_iscustomizable,msdyn_hasactivecustomization,msdyn_statusname"
    }
    [ValidateNotNull()]
    [psobject[]]$DataverseComponentSummaries = & (
        Join-Path -Resolve $PSScriptRoot "GetSolutionComponentSummaries.ps1"
    ) @DataverseComponentParams

    $DataverseComponentTypeEntitySetMap = New-Object `
        -TypeName "System.Collections.Generic.Dictionary[string, string]" `
        -ArgumentList ([System.Collections.Generic.IEqualityComparer[string]][System.StringComparer]::OrdinalIgnoreCase)
    [string[]]$DataverseComponentTypes = $DataverseComponentSummaries.msdyn_componentlogicalname
    | Select-Object -Unique
    | Where-Object {
        $DataverseApiUri = New-Object uri $DataverseApiBase, "EntityDefinitions(LogicalName = '${_}')?`$select=MetadataId,LogicalName,OwnershipType,EntitySetName"
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "GET $DataverseApiUri"
        }
        $DataverseApiResponse = Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
            -Method Get -Uri $DataverseApiUri `
            -Headers $ODataHeaders `
            -SessionVariable $PowerPlatformWebSession `
            -Verbose:$false
        $DataverseComponentTypeEntitySetMap[$DataverseApiResponse.LogicalName] = $DataverseApiResponse.EntitySetName
        return $DataverseApiResponse.OwnershipType -notin "None", "OrganizationOwned", "BusinessOwned"
    }

    $DataverseComponentSummaries = $DataverseComponentSummaries
    | Where-Object -Property "msdyn_componentlogicalname" -In $DataverseComponentTypes
}

process {
    foreach ($DataverseComponentSummary in (
            $DataverseComponentSummaries
            | Where-Object -Property "msdyn_componentlogicalname" -In $DataverseComponentTypes
        )) {
        $DataverseComponentTarget = "$($DataverseComponentTypeEntitySetMap[$DataverseComponentSummary.msdyn_componentlogicalname])($($DataverseComponentSummary.msdyn_objectid))"
        $DataverseApiRequestData = @{
            "ownerid@odata.bind" = $DataversePrincipalReference
        }
        $DataverseApiRequestText = ConvertTo-Json -Depth 10 `
            -InputObject $DataverseApiRequestData -Compress
        $DataverseApiUri = New-Object uri $DataverseApiBase, $DataverseComponentTarget
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "PATCH ${DataverseApiUri}"
        }
        Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
            -Method Patch -Uri $DataverseApiUri `
            -Headers $ODataHeaders `
            -ContentType "application/json; charset=utf-8" `
            -Body $DataverseApiRequestText `
            -SessionVariable $PowerPlatformWebSession `
            -Verbose:$false
        | Out-Null
    }
}