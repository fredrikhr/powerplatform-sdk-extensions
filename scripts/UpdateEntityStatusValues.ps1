#Requires -Version 7.0
#Requires -Modules @{ ModuleName = "Az.Accounts"; ModuleVersion = "5.0.0" }
[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string]$FilePath,
    [Parameter(Mandatory = $false)]
    [string]$SolutionName
)

begin {
    [ValidateNotNull()]
    [System.IO.FileInfo]$FileInfo = Get-Item -LiteralPath $FilePath
    [ValidateNotNull()]
    [PSObject[]]$StatusValuesDefinitionData = ConvertFrom-Json -Depth 10 -InputObject (
        Get-Content -Raw -LiteralPath $FilePath
    )
    [ValidateNotNullOrEmpty()]
    [string]$EntityLogicalName = $FileInfo.Directory.Name
    [hashtable]$ODataHeaders = @{
        "Accept"           = "application/json"
        "OData-Version"    = "4.0"
        "OData-MaxVersion" = "4.01"
        "If-Match"         = "*"
        "Content-Type"     = "application/json; charset=utf-8"
        "Prefer"           = "odata.include-annotations=*, return=representation"
    }
    if ($SolutionName) {
        $ODataHeaders["MSCRM.SolutionUniqueName"] = $SolutionName
    }
    $FlowApiUrl = "https://api.flow.microsoft.com/providers/Microsoft.Flow/environments?api-version=2026-01-01"
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Verbose "GET $FlowApiUrl"
    }
    $PowerPlatformEnvironmentsResponse = Invoke-RestMethod -Authentication OAuth `
        -Token ((Get-AzAccessToken -ResourceUrl "https://service.flow.microsoft.com/" -AsSecureString).Token) `
        -Method Get `
        -Uri $FlowApiUrl `
        -SessionVariable "PowerPlatformWebSession" `
        -Verbose:$false
    [ValidateNotNull()]
    [psobject]$PowerPlatformEnvironment = $PowerPlatformEnvironmentsResponse.value |
    ForEach-Object {
        Add-Member -PassThru -InputObject $_.properties `
            -NotePropertyName "environmentName" `
            -NotePropertyValue $_.name
    } |
    Out-GridView -PassThru -Title "Select Power Platform Environment" |
    Select-Object -First 1
    [ValidateNotNull()][psobject]$DataverseMetadataInfo = $PowerPlatformEnvironment.linkedEnvironmentMetadata
    [ValidateNotNull()][uri]$DataverseInstanceUri = $DataverseMetadataInfo.instanceUrl
    [string]$DataverseTokenAudience = $DataverseInstanceUri.GetLeftPart([System.UriPartial]::Authority)
    [ValidateNotNull()][uri]$DataverseApiRootUri = $DataverseMetadataInfo.instanceApiUrl
    [uri]$DataverseApiBase = New-Object uri $DataverseApiRootUri, "/api/data/v$($DataverseMetadataInfo.version)/`$metadata"
}

process {
    $DataverseApiUri = New-Object uri $DataverseApiBase, "EntityDefinitions(LogicalName='${EntityLogicalName}')/Attributes(LogicalName='statuscode')/Microsoft.Dynamics.CRM.StatusAttributeMetadata/OptionSet?`$select=Options"
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Verbose "GET $DataverseApiUri"
    }
    $StatusOptionSet = Invoke-RestMethod -Authentication OAuth `
        -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
        -Method Get -Uri $DataverseApiUri `
        -Headers $ODataHeaders `
        -WebSession $PowerPlatformWebSession `
        -Verbose:$false
    [PSObject[]]$StatusOptions = $StatusOptionSet.Options
    [int[]]$ExistingStatusValues = $StatusOptions.Value
    $StatusValuesDefinitionData |
    Add-Member -PassThru -Force `
        -NotePropertyMembers @{
        SolutionUniqueName   = $SolutionName
        EntityLogicalName    = $EntityLogicalName
        AttributeLogicalName = "statuscode"
    } |
    ForEach-Object {
        [PSObject]$InsertStatusValueObject = $_
        if ($InsertStatusValueObject.Value -notin $ExistingStatusValues) {
            $DataverseApiUri = New-Object uri $DataverseApiBase, "InsertStatusValue"
            if ($VerbosePreference -ne 'SilentlyContinue') {
                Write-Verbose "POST $DataverseApiUri"
                Write-Verbose "Value=$($InsertStatusValueObject.Value), StateCode=$($InsertStatusValueObject.StateCode)"
            }
        }
        else {
            $DataverseApiUri = New-Object uri $DataverseApiBase, "UpdateOptionValue"
            $InsertStatusValueObject.PSObject.Properties.Remove("StateCode")
            [void](
                Add-Member -InputObject $InsertStatusValueObject -Force `
                    -NotePropertyName "MergeLabels" -NotePropertyValue $false
            )
            if ($VerbosePreference -ne 'SilentlyContinue') {
                Write-Verbose "POST $DataverseApiUri"
                Write-Verbose "Value=$($InsertStatusValueObject.Value)"
            }
        }
        Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
            -Method Post -Uri $DataverseApiUri `
            -Headers $ODataHeaders `
            -Body (ConvertTo-Json -Depth 20 -InputObject $InsertStatusValueObject) `
            -WebSession $PowerPlatformWebSession `
            -Verbose:$false |
        Format-List "*"
    }
}