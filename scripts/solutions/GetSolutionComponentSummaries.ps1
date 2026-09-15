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
    [Parameter(Mandatory = $false)]
    [AllowNull()][AllowEmptyString()]
    [string]$FilterExpression,
    [Parameter(Mandatory = $false)]
    [AllowNull()][AllowEmptyString()]
    [string]$SelectExpression = "msdyn_total,msdyn_solutionid,msdyn_componentlogicalname,msdyn_componenttypename,msdyn_objectid,msdyn_name,msdyn_schemaname,msdyn_uniquename,msdyn_displayname,msdyn_status,msdyn_statusname,msdyn_ismanaged,msdyn_iscustomizable,msdyn_hasactivecustomization"
)

begin {
    $DataverseSolutionParams = @{
        EnvironmentId = $EnvironmentId
        DataverseUri = $DataverseUri
        SolutionUniqueName = $SolutionUniqueName
    }
    [ValidateNotNull()]
    [psobject[]]$DataverseSolutionRecords = & (
        Join-Path -Resolve $PSScriptRoot "GetSolutionProperties.ps1"
    ) @DataverseSolutionParams

    [hashtable]$ODataHeaders = @{
        "Accept"           = "application/json"
        "OData-Version"    = "4.0"
        "OData-MaxVersion" = "4.01"
        "Prefer"           = "odata.include-annotations=*, return=representation"
    }
}

process {
    foreach ($DataverseSolutionRecord in $DataverseSolutionRecords) {
        [ValidateNotNullOrEmpty()]
        [string]$EnvironmentId = $DataverseSolutionRecord.pwrplatf_environmentName
        [ValidateNotNull()][uri]$DataverseInstanceUri = $DataverseSolutionRecord.dataverse_instanceUrl
        $DataverseVersion = "9.2"
        [string]$DataverseTokenAudience = $DataverseInstanceUri.GetLeftPart([System.UriPartial]::Authority)
        [uri]$DataverseApiBase = New-Object uri $DataverseInstanceUri, "/api/data/v${DataverseVersion}/`$metadata"
        [ValidateNotNull()]
        [string]$DataverseSolutionId = $DataverseSolutionRecord.solutionid
        $DataverseApiFilterExpression = "msdyn_solutionid eq '${DataverseSolutionId}'"
        if ($FilterExpression) {
            $DataverseApiFilterExpression += "and ${FilterExpression}"
        }
        $DataverseApiSelectExpression = $SelectExpression ?? "*"
        $DataverseApiUri = New-Object uri $DataverseApiBase, "msdyn_solutioncomponentsummaries?`$filter=${DataverseApiFilterExpression}&`$select=${DataverseApiSelectExpression}"
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "GET $DataverseApiUri"
        }
        $DataverseApiResponse = Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
            -Method Get -Uri $DataverseApiUri `
            -Headers $ODataHeaders `
            -SessionVariable $PowerPlatformWebSession `
            -Verbose:$false
        $DataverseApiResponse.value
        | Add-Member -PassThru -Force -NotePropertyMembers @{
            pwrplatf_environmentName = $EnvironmentId
            dataverse_instanceUrl    = $DataverseInstanceUri.ToString()
        }
        | Write-Output
    }
}