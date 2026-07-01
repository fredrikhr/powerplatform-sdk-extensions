#Requires -Version 7.0
#Requires -Modules @{ ModuleName = "Az.Accounts"; ModuleVersion = "5.0.0" }
[CmdletBinding()]
param ()

begin {
    $FlowApiVersion = "2026-01-01"
    $FlowApiAudience = "https://service.flow.microsoft.com/"
    $FlowApiUrl = "https://api.flow.microsoft.com/providers/Microsoft.Flow/environments?api-version=${FlowApiVersion}"
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Verbose "GET $FlowApiUrl"
    }
    [ValidateNotNull()]
    $PowerPlatformEnvironmentsResponse = Invoke-RestMethod -Authentication OAuth `
        -Token ((Get-AzAccessToken -ResourceUrl $FlowApiAudience -AsSecureString -ErrorAction 'Stop').Token) `
        -Method Get `
        -Uri $FlowApiUrl `
        -SessionVariable "PowerPlatformWebSession" `
        -Verbose:$false
}

process {
    $PowerPlatformEnvironmentsResponse.value |
    ForEach-Object {
        Add-Member -PassThru -InputObject $_.properties `
            -NotePropertyName "environmentName" `
            -NotePropertyValue $_.name
    } |
    Out-GridView -PassThru -Title "Select Power Platform Environment" |
    Write-Output
}