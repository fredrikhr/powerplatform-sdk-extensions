#Requires -Version 7.0
#Requires -Modules @{ ModuleName = "Az.Accounts"; ModuleVersion = "5.0.0" }
[CmdletBinding()]
param ()

begin {
    [ValidateNotNull()]
    [psobject]$PowerPlatformEnvironment = & (
        Join-Path -Resolve (
            Join-Path -Resolve (Join-Path -Resolve $PSScriptRoot "..") "env"
        ) "GetEnvironmentProperties.ps1"
    ) |
    Select-Object -First 1
    [ValidateNotNull()][psobject]$DataverseMetadataInfo = $PowerPlatformEnvironment.linkedEnvironmentMetadata
    [ValidateNotNull()][uri]$DataverseInstanceUri = $DataverseMetadataInfo.instanceUrl
    [string]$DataverseTokenAudience = $DataverseInstanceUri.GetLeftPart([System.UriPartial]::Authority)
    [ValidateNotNull()][uri]$DataverseApiRootUri = $DataverseMetadataInfo.instanceApiUrl
    [uri]$DataverseApiBase = New-Object uri $DataverseApiRootUri, "/api/data/v$($DataverseMetadataInfo.version)/`$metadata"
    [hashtable]$ODataReadHeaders = @{
        "Accept"           = "application/json"
        "OData-Version"    = "4.0"
        "OData-MaxVersion" = "4.01"
        "Prefer"           = "odata.include-annotations=*, return=representation"
    }
    [hashtable]$ODataWriteHeaders = $ODataReadHeaders + @{
        "Content-Type" = "application/json; charset=utf-8"
    }
    $DataverseApiUri = New-Object uri $DataverseApiBase, "solutions?`$select=solutionid,uniquename,friendlyname,version,ismanaged,isvisible,modifiedon&`$filter=isvisible eq true&`$orderby=ismanaged,uniquename&`$expand=publisherid(`$select=publisherid,uniquename,friendlyname,customizationprefix,customizationoptionvalueprefix,isreadonly)"
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Verbose "GET $DataverseApiUri"
    }
    $DataverseApiResponse = Invoke-RestMethod -Authentication OAuth `
        -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
        -Method Get -Uri $DataverseApiUri `
        -Headers $ODataReadHeaders `
        -WebSession $PowerPlatformWebSession `
        -Verbose:$false
    [PSObject[]]$DataverseSolutionRecords = $DataverseApiResponse.value |
    Out-GridView -PassThru -Title "Select Dataverse Solution"
}

process {
    foreach ($DataverseSolutionRecord in $DataverseSolutionRecords) {
        $DataverseSolutionId = $DataverseSolutionRecord.solutionid
        $DataverseApiUri = New-Object uri $DataverseApiBase, "msdyn_solutioncomponentsummaries?`$filter=msdyn_solutionid eq '${DataverseSolutionId}' and msdyn_componentlogicalname eq 'workflow' and msdyn_workflowcategory eq '5'&`$select=msdyn_objectid,msdyn_workflowcategoryname,msdyn_name,msdyn_statusname"
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "GET $DataverseApiUri"
        }
        $DataverseApiResponse = Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
            -Method Get -Uri $DataverseApiUri `
            -Headers $ODataReadHeaders `
            -WebSession $PowerPlatformWebSession `
            -Verbose:$false
        $DataverseWorkflowSummaryRecords = $DataverseApiResponse.value |
        Out-GridView -PassThru -Title "Select Solution Cloud Flow"
        foreach ($DataverseWorkflowSummaryRecord in $DataverseWorkflowSummaryRecords) {
            $DataverseCloudFlowId = $DataverseWorkflowSummaryRecord.msdyn_objectid
            $DataverseApiUri = New-Object uri $DataverseApiBase, "workflows(${DataverseCloudFlowId})?`$select=workflowid,category,name,description,statecode,statuscode"
            if ($VerbosePreference -ne 'SilentlyContinue') {
                Write-Verbose "PATCH $DataverseApiUri"
            }
            $DataverseApiResponse = Invoke-RestMethod -Authentication OAuth `
                -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
                -Method Patch -Uri $DataverseApiUri `
                -Headers $ODataWriteHeaders `
                -Body (ConvertTo-Json -Depth 10 -InputObject @{
                    statecode  = 1
                    statuscode = 2
                })`
                -WebSession $PowerPlatformWebSession `
                -Verbose:$false
            Write-Output $DataverseApiResponse
        }
    }
}