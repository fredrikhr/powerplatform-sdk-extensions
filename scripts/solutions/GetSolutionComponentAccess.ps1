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
    [string]$SolutionUniqueName
)

begin {
    $DataverseComponentParams = @{
        EnvironmentId      = $EnvironmentId
        DataverseUri       = $DataverseUri
        SolutionUniqueName = $SolutionUniqueName
        SelectExpression   = "msdyn_solutionid,msdyn_componentlogicalname,msdyn_primaryidattribute,msdyn_displayname,msdyn_objectid,msdyn_schemaname,msdyn_name,msdyn_componenttype,msdyn_componenttypename,msdyn_ismanaged,msdyn_iscustomizable,msdyn_hasactivecustomization,msdyn_statusname"
    }
    [ValidateNotNull()]
    [psobject[]]$DataverseComponentSummaries = & (
        Join-Path -Resolve $PSScriptRoot "GetSolutionComponentSummaries.ps1"
    ) @DataverseComponentParams

    $DataverseVersion = "9.2"
    [hashtable]$ODataHeaders = @{
        "Accept"           = "application/json"
        "OData-Version"    = "4.0"
        "OData-MaxVersion" = "4.01"
        "Prefer"           = "odata.include-annotations=*"
    }

    [string]$EnvironmentId = $DataverseComponentSummaries.pwrplatf_environmentName | Select-Object -First 1
    [uri]$DataverseInstanceUri = $DataverseComponentSummaries.dataverse_instanceUrl | Select-Object -First 1
    [string]$DataverseTokenAudience = if ($DataverseInstanceUri) { $DataverseInstanceUri.GetLeftPart([System.UriPartial]::Authority) }
    [uri]$DataverseApiBase = if ($DataverseInstanceUri) { New-Object uri $DataverseInstanceUri, "/api/data/v${DataverseVersion}/`$metadata" }

    [string[]]$DataverseComponentTypes = $DataverseComponentSummaries.msdyn_componentlogicalname
    | Select-Object -Unique
    | Where-Object {
        $DataverseApiUri = New-Object uri $DataverseApiBase, "EntityDefinitions(LogicalName = '${_}')?`$select=MetadataId,LogicalName,OwnershipType"
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "GET $DataverseApiUri"
        }
        $DataverseApiResponse = Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
            -Method Get -Uri $DataverseApiUri `
            -Headers $ODataHeaders `
            -SessionVariable $PowerPlatformWebSession `
            -Verbose:$false
        return $DataverseApiResponse.OwnershipType -notin "None", "OrganizationOwned"
    }
}

process {
    $DataverseTeamCache = New-Object "System.Collections.Generic.Dictionary[string, PSObject]" `
        -ArgumentList ([System.Collections.Generic.IEqualityComparer[string]][System.StringComparer]::OrdinalIgnoreCase)
    $DataverseUserCache = New-Object "System.Collections.Generic.Dictionary[string, PSObject]" `
        -ArgumentList ([System.Collections.Generic.IEqualityComparer[string]][System.StringComparer]::OrdinalIgnoreCase)

    foreach ($DataverseComponentSummary in $DataverseComponentSummaries) {
        if ($DataverseComponentSummary.msdyn_componentlogicalname -notin $DataverseComponentTypes) {
            continue
        }

        $DataverseComponentTarget = @{
            "@odata.type"                                            = "Microsoft.Dynamics.CRM.$($DataverseComponentSummary.msdyn_componentlogicalname)"
            "$($DataverseComponentSummary.msdyn_primaryidattribute)" = $DataverseComponentSummary.msdyn_objectid
        }

        $DataverseApiUri = New-Object uri $DataverseApiBase, "RetrieveSharedPrincipalsAndAccess(Target=@Target)?@Target=$([uri]::EscapeDataString((ConvertTo-Json -Compress -InputObject $DataverseComponentTarget -Depth 3)))"
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "GET $DataverseApiUri"
        }
        $DataverseApiResponse = Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
            -Method Get -Uri $DataverseApiUri `
            -Headers $ODataHeaders `
            -SessionVariable $PowerPlatformWebSession `
            -Verbose:$false

        Add-Member -InputObject $DataverseComponentSummary -Force `
            -NotePropertyName "PrincipalAccesses" `
            -NotePropertyValue $DataverseApiResponse.PrincipalAccesses

        foreach ($DataverseComponentPrincipalAccess in $DataverseApiResponse.PrincipalAccesses) {
            $DataversePrincipalReference = $DataverseComponentPrincipalAccess.Principal
            [ValidateNotNull()]
            [string]$DataversePrincipalType = $DataversePrincipalReference."@type"
            [ValidateNotNull()]
            [string]$DataversePrincipalOwnerId = $DataversePrincipalReference.ownerid
            switch ($DataversePrincipalType) {
                "#Microsoft.Dynamics.CRM.systemuser" {
                    $DataverseUserCache[$DataversePrincipalOwnerId] = $null
                    break
                }
                "#Microsoft.Dynamics.CRM.team" {
                    $DataverseTeamCache[$DataversePrincipalOwnerId] = $null
                    break
                }
            }
        }
    }

    foreach ($DataversePrincipalOwnerId in $DataverseUserCache.Keys) {
        $DataverseApiUri = New-Object uri $DataverseApiBase, "systemusers(${DataversePrincipalOwnerId})?`$select=systemuserid,fullname,domainname,internalemailaddress,azureactivedirectoryobjectid,applicationid"
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "GET $DataverseApiUri"
        }
        $DataverseApiResponse = Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
            -Method Get -Uri $DataverseApiUri `
            -Headers $ODataHeaders `
            -SessionVariable $PowerPlatformWebSession `
            -Verbose:$false
        $DataverseUserCache[$DataversePrincipalOwnerId] = $DataverseApiResponse
    }
    foreach ($DataversePrincipalOwnerId in $DataverseTeamCache.Keys) {
        $DataverseApiUri = New-Object uri $DataverseApiBase, "teams(${DataversePrincipalOwnerId})?`$select=teamid,name,membershiptype,teamtype,azureactivedirectoryobjectid"
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "GET $DataverseApiUri"
        }
        $DataverseApiResponse = Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
            -Method Get -Uri $DataverseApiUri `
            -Headers $ODataHeaders `
            -SessionVariable $PowerPlatformWebSession `
            -Verbose:$false
        $DataverseTeamCache[$DataversePrincipalOwnerId] = $DataverseApiResponse
    }

    foreach ($DataverseComponentSummary in $DataverseComponentSummaries) {
        foreach ($DataverseComponentAccessData in $DataverseComponentSummary.PrincipalAccesses) {
            $DataverseComponentAccessRecord = New-Object psobject
            foreach ($DataverseComponentSummaryProp in (
                    $DataverseComponentSummary.PSObject.Properties
                    | Where-Object -Property Name -Like "msdyn_*"
                    | Where-Object -Property Name -NotLike "*@OData.Community.Display.V1.FormattedValue"
                )) {
                Add-Member -Force -InputObject $DataverseComponentAccessRecord `
                    -NotePropertyName $DataverseComponentSummaryProp.Name `
                    -NotePropertyValue $DataverseComponentSummaryProp.Value
            }

            $DataverseComponentAccessRights = $DataverseComponentAccessData.AccessMask
            $DataversePrincipalReference = $DataverseComponentAccessData.Principal
            [ValidateNotNull()]
            [string]$DataversePrincipalType = $DataversePrincipalReference."@type"
            [ValidateNotNull()]
            [string]$DataversePrincipalOwnerId = $DataversePrincipalReference.ownerid
            switch ($DataversePrincipalType) {
                "#Microsoft.Dynamics.CRM.systemuser" {
                    $DataverseUserRecord = $DataverseUserCache[$DataversePrincipalOwnerId]
                    foreach ($DataverseUserProp in (
                            $DataverseUserRecord.PSObject.Properties
                            | Where-Object -Property Name -NE "@etag"
                            | Where-Object -Property Name -NE "@context"
                        )) {
                        Add-Member -Force -InputObject $DataverseComponentAccessRecord `
                            -NotePropertyName "systemuser_$($DataverseUserProp.Name)" `
                            -NotePropertyValue $DataverseUserProp.Value
                    }
                    break
                }
                "#Microsoft.Dynamics.CRM.team" {
                    $DataverseTeamRecord = $DataverseTeamCache[$DataversePrincipalOwnerId]
                    foreach ($DataverseTeamProp in (
                            $DataverseTeamRecord.PSObject.Properties
                            | Where-Object -Property Name -NE "@etag"
                            | Where-Object -Property Name -NE "@context"
                        )) {
                        Add-Member -Force -InputObject $DataverseComponentAccessRecord `
                            -NotePropertyName "team_$($DataverseTeamProp.Name)" `
                            -NotePropertyValue $DataverseTeamProp.Value
                    }
                    break
                }
            }
            Add-Member -Force -InputObject $DataverseComponentAccessRecord `
                -NotePropertyName "access_granted" `
                -NotePropertyValue $DataverseComponentAccessRights

            Write-Output $DataverseComponentAccessRecord
        }
    }
}