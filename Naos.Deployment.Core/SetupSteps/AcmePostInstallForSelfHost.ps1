<#
.SYNOPSIS
Post-install script to bind new certificate to self-hosted app.
.DESCRIPTION
Note that this script is intended to be run via the install script plugin from win-acme via the batch script wrapper.
As such, we use positional parameters to avoid issues with using a dash in the cmd line.

.PARAMETER CertThumbprint
The thumbprint of the new certificate.

.PARAMETER CertCommonName
The common name of the certificate.

.PARAMETER ProcessNameToKill

.EXAMPLE
AcmePostInstallForSelfHost.ps1 e438d45af2dd55a01c84ff5ce7058edbfe2fcfaf beta.web.api.production-1.cometrics.com CoMetrics.Web.Api

.NOTES
#>
param(
    [string]$CertThumbprint,
    [string]$CertCommonName,
    [string]$ProcessNameToKill
)

############################
# Entry Point
############################

try {
    Write-Host "CertThumbprint: $($CertThumbprint)"
    Write-Host "CertCommonName:  $($CertCommonName)"

    netsh http delete sslcert hostnameport=${CertCommonName}:443
    $CertStoreLocation = "LocalMachine"
    $CertStoreName = "My"
    $ApplicationId = [System.Guid]::NewGuid()
    $HttpsDnsEntries = ,$CertCommonName
    Write-Output " CertStoreLocation: $CertStoreLocation"
    Write-Output "     CertStoreName: $CertStoreName"
    Write-Output "    CertThumbprint: $CertThumbprint"
    Write-Output "     ApplicationId: $ApplicationId"
    Write-Output "   HttpsDnsEntries: $([System.String]::Join(',', $HttpsDnsEntries))"
    if ($CertStoreLocation -ne 'LocalMachine') {
    throw $('Can not configure certificates outside of LocalMachine for Self Hosting; specified: ' + $CertStoreLocation)
    }
    $certFullPath = Join-Path (Join-Path (Join-Path 'cert:' $CertStoreLocation) $CertStoreName) $CertThumbprint
    $cert = Get-Item $certFullPath
    if ($cert -eq $null)
    {
    throw "Cert missing at $certFullPath"
    }
    $HttpsDnsEntries | %{
        $hostNamePort = "$($_):443"
        Write-Output "HostNamePort: $hostNamePort"
        netsh http add sslcert hostnameport=$hostNamePort appid="{$($ApplicationId)}" certhash=$CertThumbprint certstorename=$CertStoreName
    }

    # Don't kill process near the time when the keep-alive task is running.
    $seconds = (Get-Date).Second
    while ($seconds -lt 10 -or $seconds -gt 40) {
        Start-Sleep -Milliseconds 500
        $seconds = (Get-Date).Second
    }

    Get-Process -Name $ProcessNameToKill -ErrorAction SilentlyContinue | Stop-Process -Force
}
catch {
    Write-Error "Script failed: $_"
    exit 1
}

exit 0


