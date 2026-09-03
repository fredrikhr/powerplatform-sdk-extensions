#Requires -Version 7.0
#Requires -Modules @{ ModuleName = "Az.Accounts"; ModuleVersion = "5.0.0" }
[CmdletBinding()]
param ()

begin {
    $PpApiVersion = "2024-10-01"
    $PpApiAudience = "https://api.powerplatform.com"
    [ValidateNotNull()]
    [psobject[]]$PpAppPackages = & (
        Join-Path -Resolve $PSScriptRoot "GetEnvironmentApplicationPackages.ps1"
    )
}

process {
    foreach ($PpAppPackage in $PpAppPackages) {
        [ValidateNotNullOrEmpty()]
        [string]$PpEnvironmentId = $PpAppPackage.environmentName
        [ValidateNotNullOrEmpty()]
        [string]$PpAppPackageUniqueName = $PpAppPackage.uniqueName
        $PpApiUrl = "https://api.powerplatform.com/appmanagement/environments/${PpEnvironmentId}/applicationPackages/${PpAppPackageUniqueName}/install?api-version=${PpApiVersion}"
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "POST $PpApiUrl"
        }
        $PpApiInstallResponse = Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $PpApiAudience -AsSecureString -ErrorAction 'Stop').Token) `
            -Method Post -Uri $PpApiUrl `
            -ContentType "application/json; charset=utf-8" `
            -Body:$null `
            -SessionVariable "PowerPlatformWebSession" -Verbose:$false
        [ValidateNotNull()]
        [psobject]$PpApiOperationProperties = $PpApiInstallResponse.lastOperation
        [ValidateNotNull()]
        [string]$PpApiOperationId = $PpApiOperationProperties.operationId
        $PpApiOperationStatusPolling = $true
        while ($PpApiOperationStatusPolling) {
            [void](Start-Sleep -Seconds 5)
            $PpApiUrl = "https://api.powerplatform.com/appmanagement/environments/${PpEnvironmentId}/operations/${PpApiOperationId}?api-version=${PpApiVersion}"
            if ($VerbosePreference -ne 'SilentlyContinue') {
                Write-Verbose "GET $PpApiUrl"
            }
            [ValidateNotNull()]
            [psobject]$PpApiOperationProperties = Invoke-RestMethod -Authentication OAuth `
                -Token ((Get-AzAccessToken -ResourceUrl $PpApiAudience -AsSecureString -ErrorAction 'Stop').Token) `
                -Method Get -Uri $PpApiUrl `
                -WebSession $PowerPlatformWebSession -Verbose:$false
            if ($VerbosePreference -ne 'SilentlyContinue') {
                Format-List -InputObject $PpApiOperationProperties `
                    -Property @{ N = "packageUniqueName"; Expression = { $PpAppPackageUniqueName } }, "*", @{ N = "timeElapsed"; E = { if ($_.createdDateTime) { ([datetime]::UtcNow - $_.createdDateTime) } else { $null } } }
            }
            [ValidateNotNull()]
            [string]$PpApiOperationId = $PpApiOperationProperties.operationId
            $PpApiOperationStatusPolling = switch ($PpApiOperationProperties.status) {
                "Succeeded" { $false; break; }
                "Failed" { $false; break; }
                "Canceled" { $false; break; }
                default { $true; break; }
            }
        }
        $PpApiUrl = "https://api.powerplatform.com/appmanagement/environments/${PpEnvironmentId}/applicationPackages?api-version=${PpApiVersion}"
        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose "GET $PpApiUrl"
        }
        [ValidateNotNull()]
        $PpAppPackagesResponse = Invoke-RestMethod -Authentication OAuth `
            -Token ((Get-AzAccessToken -ResourceUrl $PpApiAudience -AsSecureString -ErrorAction 'Stop').Token) `
            -Method Get -Uri $PpApiUrl `
            -WebSession $PowerPlatformWebSession -Verbose:$false
        $PpAppPackagesResponse.value |
        Where-Object -Property "uniqueName" -EQ $PpAppPackageUniqueName |
        Write-Output
    }
}