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
    [Parameter()][switch]$Disable,
    [Parameter()][switch]$Enable
)

begin {
    $DataverseComponentParams = @{
        EnvironmentId      = $EnvironmentId
        DataverseUri       = $DataverseUri
        SolutionUniqueName = $SolutionUniqueName
        FilterExpression   = "msdyn_componentlogicalname eq 'workflow' and msdyn_workflowcategory eq '5'"
        SelectExpression   = "msdyn_objectid,msdyn_workflowcategoryname,msdyn_name,msdyn_statusname,msdyn_solutionid"
    }
    [PSObject[]]$DataverseComponentSummaries = & (
        Join-Path -Resolve (
            Join-Path -Resolve (Join-Path -Resolve $PSScriptRoot "..") "solutions"
        ) "GetSolutionComponentSummaries.ps1"
    ) @DataverseComponentParams
    | Out-GridView -PassThru -Title "Select Solution Cloud Flow"

    $DataverseVersion = "9.2"
    [hashtable]$ODataHeaders = @{
        "Accept"           = "application/json"
        "OData-Version"    = "4.0"
        "OData-MaxVersion" = "4.01"
        "Prefer"           = "odata.include-annotations=*, return=representation"
    }

    [string]$EnvironmentId = $DataverseComponentSummaries.pwrplatf_environmentName | Select-Object -First 1
    [uri]$DataverseInstanceUri = $DataverseComponentSummaries.dataverse_instanceUrl | Select-Object -First 1
    [string]$DataverseTokenAudience = if ($DataverseInstanceUri) { $DataverseInstanceUri.GetLeftPart([System.UriPartial]::Authority) }
    [uri]$DataverseApiBase = if ($DataverseInstanceUri) { New-Object uri $DataverseInstanceUri, "/api/data/v${DataverseVersion}/`$metadata" }
}

process {
    foreach ($DataverseWorkflowSummaryRecord in $DataverseComponentSummaries) {
        [ValidateNotNull()]
        [string]$DataverseSolutionId = $DataverseWorkflowSummaryRecord.msdyn_solutionid
        [ValidateNotNull()]
        [string]$DataverseCloudFlowId = $DataverseWorkflowSummaryRecord.msdyn_objectid

        $DataverseApiUri = New-Object uri $DataverseApiBase, "workflows(${DataverseCloudFlowId})?`$select=workflowid,category,name,description,statecode,statuscode"
        $DataverseApiResponse = $null
        if ($Disable) {
            $DataverseApiRequestBody = ConvertTo-Json -Depth 10 -Compress -InputObject @{
                statecode  = 0
                statuscode = 1
            }
            if ($VerbosePreference -ne 'SilentlyContinue') {
                Write-Verbose "PATCH $DataverseApiUri"
                Write-Verbose $DataverseApiRequestBody
            }
            $DataverseApiResponse = Invoke-RestMethod -Authentication OAuth `
                -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
                -Method Patch -Uri $DataverseApiUri `
                -Headers $ODataHeaders `
                -ContentType "application/json; charset=utf-8" `
                -Body $DataverseApiRequestBody `
                -WebSession $PowerPlatformWebSession `
                -Verbose:$false
        }
        if ($Enable) {
            $DataverseApiRequestBody = ConvertTo-Json -Depth 10 -Compress -InputObject @{
                statecode  = 1
                statuscode = 2
            }
            if ($VerbosePreference -ne 'SilentlyContinue') {
                Write-Verbose "PATCH $DataverseApiUri"
                Write-Verbose $DataverseApiRequestBody
            }
            $DataverseApiResponse = Invoke-RestMethod -Authentication OAuth `
                -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
                -Method Patch -Uri $DataverseApiUri `
                -Headers $ODataHeaders `
                -ContentType "application/json; charset=utf-8" `
                -Body $DataverseApiRequestBody `
                -WebSession $PowerPlatformWebSession `
                -Verbose:$false
        }
        if ($DataverseApiResponse) {
            Write-Output $DataverseApiResponse
        }
    }
}