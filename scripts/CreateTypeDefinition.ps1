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
    [ValidateNotNullOrEmpty()]
    [string]$FileContent = Get-Content -Raw -LiteralPath $FilePath
    [ValidateNotNull()]
    $TypeDefinitionData = ConvertFrom-Json -Depth 10 -InputObject $FileContent
    [ValidateNotNullOrEmpty()]
    [string]$FolderName = $FileInfo.Directory.Parent.Name
    [ValidateSet(
        "EntityDefinitions",
        "GlobalOptionSetDefinitions",
        "RelationshipDefinitions"
        )]
    [ValidateNotNullOrEmpty()]
    [string]$TypeSetName = switch ($FolderName) {
        "Entities" { "EntityDefinitions"; break }
        "OptionSets" { "GlobalOptionSetDefinitions"; break }
        "Relationships" { "RelationshipDefinitions"; break }
    }
    [ValidateNotNullOrEmpty()]
    [string]$TypeLookup = switch ($TypeSetName) {
        "EntityDefinitions" { "LogicalName='$($TypeDefinitionData.LogicalName)'"; break }
        "GlobalOptionSetDefinitions" { "Name='$($TypeDefinitionData.Name)'"; break }
        "RelationshipDefinitions" { "SchemaName='$($TypeDefinitionData.SchemaName)'"; break }
    }
    [hashtable]$ODataHeaders = @{
        "Accept"           = "application/json"
        "OData-Version"    = "4.0"
        "OData-MaxVersion" = "4.01"
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
    $DataverseApiUri = New-Object uri $DataverseApiBase, "${TypeSetName}(${TypeLookup})"
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Verbose "GET $DataverseApiUri"
    }
    $TypeDefinitionResourceResponse = Invoke-RestMethod -Authentication OAuth `
        -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
        -Method Get -Uri $DataverseApiUri `
        -Headers $ODataHeaders `
        -WebSession $PowerPlatformWebSession `
        -Verbose:$false -SkipHttpErrorCheck
    if ($TypeDefinitionResourceResponse.error) {
        $DataverseApiUri = New-Object uri $DataverseApiBase, $TypeSetName
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "POST $DataverseApiUri"
        }
        Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
            -Method Post -Uri $DataverseApiUri `
            -Headers $ODataHeaders `
            -Body $FileContent `
            -WebSession $PowerPlatformWebSession `
            -Verbose:$false |
        Format-List "*"
    }
    else {
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "PUT $DataverseApiUri"
        }
        Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $DataverseTokenAudience -AsSecureString).Token) `
            -Method Put -Uri $DataverseApiUri `
            -Headers $ODataHeaders `
            -Body $FileContent `
            -WebSession $PowerPlatformWebSession `
            -Verbose:$false |
        Format-List "*"
    }

}