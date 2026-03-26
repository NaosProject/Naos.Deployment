<#
.SYNOPSIS
Add or remove a DNS TXT record to AWS Route 53
.DESCRIPTION
Note that this script is intended to be run via the install script plugin from win-acme via the batch script wrapper.
As such, we use positional parameters to avoid issues with using a dash in the cmd line.

Adapted from the original EasyDNS plugin by RMBolger (Posh-ACME).
Please reference their license terms for use/modification: https://github.com/rmbolger/Posh-ACME/blob/main/LICENSE

.PARAMETER Task
'create' to add the TXT record, 'delete' to remove it.

.PARAMETER IdentifierName
The domain that's being validated (e.g. sub.example.com).

.PARAMETER RecordName
The fully qualified name of the TXT record (e.g. _acme-challenge.example.com).

.PARAMETER TxtValue
The value of the TXT record.

.PARAMETER ExtraParams
This parameter can be ignored and is only used to prevent errors when splatting with more parameters than this function supports.

.EXAMPLE
Route53.ps1 create {Identifier} {RecordName} {Token}
Route53.ps1 delete {Identifier} {RecordName} {Token}

.NOTES
Requires the AWS.Tools.Route53 PowerShell module.
The script will attempt to install it automatically if it is not already present.
#>
param(
    [string]$Task,
    [string]$Identifier,
    [string]$RecordName,
    [string]$TxtValue
)

############################
# Helper Functions
############################
function Invoke-EnsureRequiredModules {
    <#
    .SYNOPSIS
        Ensures the AWS.Tools.Route53 module is available, installing it if necessary.
    #>
    if (-not (Get-Module -ListAvailable -Name 'AWS.Tools.Route53')) {
        Write-Verbose "AWS.Tools.Route53 not found. Attempting to install..."
        Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force
        Install-Module AWS.Tools.Route53 -Scope AllUsers -Force -AllowClobber -Confirm:$false
    }
    Import-Module 'AWS.Tools.Route53' -ErrorAction Stop
}

function Get-R53HostedZoneId {
    <#
    .SYNOPSIS
        Walks up the DNS hierarchy to find the Route 53 Hosted Zone ID for a given record name.
    #>
    param(
        [string]$RecordName,
        [string]$AccessKey,
        [string]$SecretKey
    )

    $commonParams = @{
        AccessKey = $AccessKey
        SecretKey = $SecretKey
    }

    $allZones = Get-R53HostedZonesByName -DNSName $zoneTest @commonParams -ErrorAction Stop
    Write-Verbose ($allZones | ConvertTo-Json -Depth 5)

    $pieces = $RecordName.Split('.')
    for ($i = 0; $i -lt ($pieces.Count - 1); $i++) {
        $zoneTest = ($pieces[$i..($pieces.Count - 1)] -join '.') + '.'  # Route 53 names are dot-terminated
        try {
            Write-Host "Testing '$($zoneTest)'"

            $zone = $allZones | Where-Object { $_.Name -eq $zoneTest } | Select-Object -First 1

            if ($zone) {
                Write-Host "Found hosted zone '$($zone.Name)' (ID: $($zone.Id))"
                return $zone.Id  # e.g. /hostedzone/Z1234567890ABC
            }
        } catch {
            Write-Host "Zone lookup failed for '$zoneTest': $_"
        }
    }

    throw "Unable to find a Route 53 hosted zone for record '$RecordName'."
}

function Get-R53CurrentTxtValues {
    <#
    .SYNOPSIS
        Returns the current list of TXT string values for the given record, or an empty array if it does not exist.
    #>
    param(
        [string]$ZoneId,
        [string]$RecordName,
        [string]$AccessKey,
        [string]$SecretKey
    )

    $commonParams = @{
        AccessKey = $AccessKey
        SecretKey = $SecretKey
    }

    $fqdn = if ($RecordName.EndsWith('.')) { $RecordName } else { "$RecordName." }

    try {
        $response = Get-R53ResourceRecordSet -HostedZoneId $ZoneId `
            -StartRecordName $fqdn -StartRecordType TXT -MaxItem 1 @commonParams -ErrorAction Stop

        Write-Verbose ($response | ConvertTo-Json -Depth 5)

        $rrs = $response.ResourceRecordSets | Where-Object { $_.Name -eq $fqdn -and $_.Type -eq 'TXT' } | Select-Object -First 1
        if ($rrs) {
            # Route 53 wraps each TXT value in double-quotes; strip them for comparison
            return @($rrs.ResourceRecords | ForEach-Object { $_.Value.Trim('"') })
        }
    } catch {
        Write-Host "Could not retrieve existing TXT records for '$RecordName': $_"
    }

    return @()
}

function Submit-R53Change {
    <#
    .SYNOPSIS
        Submits a single ChangeResourceRecordSet request to Route 53.
    #>
    param(
        [string]$ZoneId,
        [string]$Action,        # UPSERT | DELETE
        [string]$RecordName,
        [string[]]$TxtValues,   # All values the record should contain after the change
        [string]$AccessKey,
        [string]$SecretKey,
        [int]$Ttl = 300
    )

    $commonParams = @{
        AccessKey = $AccessKey
        SecretKey = $SecretKey
    }

    $fqdn = if ($RecordName.EndsWith('.')) { $RecordName } else { "$RecordName." }

    # Build the ResourceRecord list; Route 53 requires each value to be double-quoted
    $resourceRecords = $TxtValues | ForEach-Object {
        $rr = New-Object Amazon.Route53.Model.ResourceRecord
        $rr.Value = "`"$_`""
        $rr
    }

    $rrSet = New-Object Amazon.Route53.Model.ResourceRecordSet
    $rrSet.Name            = $fqdn
    $rrSet.Type            = 'TXT'
    $rrSet.TTL             = $Ttl
    $rrSet.ResourceRecords = $resourceRecords

    $change = New-Object Amazon.Route53.Model.Change
    $change.Action            = $Action
    $change.ResourceRecordSet = $rrSet

    Edit-R53ResourceRecordSet -HostedZoneId $ZoneId `
        -ChangeBatch_Change $change `
        -ChangeBatch_Comment "ACME DNS-01 challenge managed by WACS" `
        @commonParams -ErrorAction Stop | Out-Null

    Write-Host "Route 53 change submitted: $Action '$fqdn'"
}

############################
# Main Functions
############################

function Add-DnsTxt {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position=0)]
        [string]$RecordName,
        [Parameter(Mandatory, Position=1)]
        [string]$TxtValue,
        [Parameter(Mandatory, Position=2)]
        [string]$AccessKey,
        [Parameter(Mandatory, Position=3)]
        [string]$SecretKey,
        [Parameter(ValueFromRemainingArguments)]
        $ExtraParams
    )

    $zoneId = Get-R53HostedZoneId -RecordName $RecordName -AccessKey $AccessKey -SecretKey $SecretKey

    $existing = Get-R53CurrentTxtValues -ZoneId $zoneId -RecordName $RecordName -AccessKey $AccessKey -SecretKey $SecretKey

    if ($existing -contains $TxtValue) {
        Write-Host "Record '$RecordName' already contains value '$TxtValue'. Nothing to do."
        return
    }

    Write-Host "Adding TXT record '$RecordName' with value '$TxtValue'"
    $newValues = @($existing) + @($TxtValue)
    Submit-R53Change -ZoneId $zoneId -Action 'UPSERT' -RecordName $RecordName -TxtValues $newValues -AccessKey $AccessKey -SecretKey $SecretKey

    <#
    .SYNOPSIS
        Add a DNS TXT record to AWS Route 53.
    .DESCRIPTION
        Locates the hosted zone that owns RecordName, then UPSERTs a TXT record with the supplied value.
        If the record already exists with other values, those are preserved.
    #>
}

function Remove-DnsTxt {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position=0)]
        [string]$RecordName,
        [Parameter(Mandatory, Position=1)]
        [string]$TxtValue,
        [Parameter(Mandatory, Position=2)]
        [string]$AccessKey,
        [Parameter(Mandatory, Position=3)]
        [string]$SecretKey,
        [Parameter(ValueFromRemainingArguments)]
        $ExtraParams
    )

    $zoneId = Get-R53HostedZoneId -RecordName $RecordName -AccessKey $AccessKey -SecretKey $SecretKey

    $existing = Get-R53CurrentTxtValues -ZoneId $zoneId -RecordName $RecordName -AccessKey $AccessKey -SecretKey $SecretKey

    if ($existing -notcontains $TxtValue) {
        Write-Host "Record '$RecordName' with value '$TxtValue' does not exist. Nothing to do."
        return
    }

    $remaining = @($existing | Where-Object { $_ -ne $TxtValue })

    if ($remaining.Count -gt 0) {
        # Other values still exist — update the record set without this value
        Write-Host "Removing value '$TxtValue' from TXT record '$RecordName' (preserving $($remaining.Count) other value(s))"
        Submit-R53Change -ZoneId $zoneId -Action 'UPSERT' -RecordName $RecordName -TxtValues $remaining -AccessKey $AccessKey -SecretKey $SecretKey
    } else {
        # No values left — delete the record set entirely
        Write-Host "Removing TXT record '$RecordName' entirely (last value)"
        Submit-R53Change -ZoneId $zoneId -Action 'DELETE' -RecordName $RecordName -TxtValues @($TxtValue) -AccessKey $AccessKey -SecretKey $SecretKey
    }

    <#
    .SYNOPSIS
        Remove a DNS TXT record from AWS Route 53.
    .DESCRIPTION
        Removes the specified TXT value.
        If the record set contains other values, those are preserved via UPSERT.
        If this was the last value, the record set is deleted entirely.
    #>
}

############################
# Entry Point
############################

Write-Host "Task: $($Task)"
Write-Host "Identifier:  $($Identifier)"
Write-Host "RecordName: $($RecordName)"
Write-Host "TxtValue: $($TxtValue)"

try {
    Invoke-EnsureRequiredModules

    $accessKey = $env:WACS_ROUTE53_ACCESS_KEY
    $secretKey  = $env:WACS_ROUTE53_SECRET_KEY

    if ([string]::IsNullOrWhiteSpace($accessKey)) {
        throw "WACS_ROUTE53_ACCESS_KEY machine environment variable is not set."
    }
    if ([string]::IsNullOrWhiteSpace($secretKey)) {
        throw "WACS_ROUTE53_SECRET_KEY machine environment variable is not set."
    }

    if ($Task -eq 'create') {
        Add-DnsTxt $RecordName $TxtValue $accessKey $secretKey
    }

    if ($Task -eq 'delete') {
        Remove-DnsTxt $RecordName $TxtValue $accessKey $secretKey
    }
}
catch {
    Write-Error "Script failed: $_"
    exit 1
}

exit 0
