// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InstallScriptBlocks.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    internal static class InstallScriptBlocks
    {
        public static string InstallWeb
        {
            get
            {
                return @"
{
param(
	[string] $RootPath,
	[string] $Domain,
	[string] $CertPath,
	[SecureString] $CertPassword,
	[switch] $EnableSNI,
	[switch] $AddHostHeaders
	)

try
{
	Write-Output ""Beginning Deployment of a Website:""
	Write-Output ""    RootPath: $RootPath""
	Write-Output ""    Domain: $Domain""
	Write-Output ""    CertPath: $CertPath""
	Write-Output ""    CertPassword: $CertPassword""
	Write-Output ''
	
	if (-not (Test-Path $RootPath))
	{
		md $RootPath
	}
	
	# Add IIS and suppporting features
	Add-WindowsFeature -IncludeManagementTools -Name Web-Default-Doc, Web-Dir-Browsing, Web-Http-Errors, Web-Static-Content, Web-Http-Redirect, Web-Http-Logging, Web-Custom-Logging, Web-Log-Libraries, Web-Request-Monitor, Web-Http-Tracing, Web-Basic-Auth, Web-Digest-Auth, Web-Windows-Auth, Web-Net-Ext, Web-Net-Ext45, Web-Asp-Net, Web-Asp-Net45, Web-ISAPI-Ext, Web-ISAPI-Filter, Web-Scripting-Tools, NET-Framework-45-ASPNET
	# Set IIS Service to restart on failure and reboot on 3rd failure
	$services = Get-WMIObject win32_service | Where-Object {$_.name -imatch ""W3SVC"" -and $_.startmode -eq ""Auto""}; foreach ($service in $services){sc.exe failure $service.name reset= 86400 actions= restart/5000/restart/5000/reboot/5000}

	Import-Module WebAdministration
	
	$innerPackageDirForWebPackage = 'packagedWebsite' # this value must match whats in the build script Build.ps1

	$SitePath = Join-Path $RootPath $innerPackageDirForWebPackage
	Write-Output ""Using site path for IIS at $SitePath""

	Write-Output ""Removing default site if present to avoid any potential conflicts""
	if (Test-Path 'IIS:\Sites\Default Web Site'){ Remove-Item 'IIS:\Sites\Default Web Site' -Force -Recurse}

	if (-not (Test-Path $SitePath))
	{
		throw ""Site missing at $SitePath""
	}
	
	if (-not (Test-Path $CertPath))
	{
		throw ""Cert missing at $CertPath""
	}
	
	$certStoreLocation = 'cert:\LocalMachine\My'
	Write-Output ""Installing cert at $certStoreLocation""
	$certResult = Import-PfxCertificate -FilePath $CertPath -Password $CertPassword -CertStoreLocation $certStoreLocation -Exportable
	rm $CertPath -Force
	Write-Output ""Cert installed with Thumbprint: $($certResult.Thumbprint) - Deleted file""

	$appPoolName = ""$($Domain)_AppPool""
	Write-Output ""Creating Application Pool: $appPoolName""
	New-Item ""IIS:\AppPools\$appPoolName"" | Out-Null
	Set-ItemProperty ""IIS:\AppPools\$appPoolName"" managedRuntimeVersion v4.0 | Out-Null

	$sslFlags = 0
	if ($EnableSNI)
	{
		$sslFlags = 1
		Write-Output ""SNI Enabled (can use multiple host names on same machine""
	}
	else
	{
		Write-Output ""SNI is NOT Enabled (can NOT use multiple host names on same machine""
	}
	
	if ($AddHostHeaders)
	{
		Write-Output ""Creating site at $SitePath for domain $Domain WITH hostHeaders""
		New-Item -Path ""IIS:\Sites\$Domain"" -bindings @{protocol=""http"";bindingInformation="":80:$Domain""} -physicalPath $SitePath -applicationPool $appPoolName
		New-WebBinding -name $Domain -Protocol https -HostHeader ""$Domain"" -Port 443 -SslFlags $sslFlags
	}
	else
	{
		Write-Output ""Creating site at $SitePath for domain $Domain WITH OUT hostHeaders""
		New-Item -Path ""IIS:\Sites\$Domain"" -bindings @{protocol=""http"";bindingInformation="":80:""} -physicalPath $SitePath -applicationPool $appPoolName
		New-WebBinding -name $Domain -Protocol https -Port 443 -SslFlags $sslFlags
	}
	
	$cert = Get-Item $(Join-Path $certStoreLocation $certResult.Thumbprint)
	New-Item -Path ""IIS:\SslBindings\!443!$Domain"" -Value $cert -SSLFlags $sslFlags
	
	Write-Output ""Performing IIS RESET to make sure everything is up and running correctly""
	iisreset
	
	$site = Get-Item ""IIS:\sites\$Domain"" -ErrorAction SilentlyContinue
	$newSitePath = $site.physicalPath
	
	if ($newSitePath -ne $SitePath)
	{
		throw ""Failed to correctly deploy site to $SitePath, instead it got configured to $newSitePath""
	}

	$deploymentInfo = join-path $SitePath ""deploymentInfo.xml""
	Write-Output ""Writing deployment info to $deploymentInfo""

	$xmlWriter = New-Object System.XML.XmlTextWriter($deploymentInfo,$Null)
	# choose a pretty formatting:
	$xmlWriter.Formatting = 'Indented'
	$xmlWriter.Indentation = 1
	$xmlWriter.IndentChar = ""	""
	$xmlWriter.WriteStartDocument()
	$xmlWriter.WriteStartElement('Publish')
	$xmlWriter.WriteElementString('SitePath',$SitePath)
	$xmlWriter.WriteElementString('Domain',$Domain)
	$xmlWriter.WriteElementString('AppPool',$appPoolName)
	$xmlWriter.WriteElementString('Username',[Environment]::Username)
	$xmlWriter.WriteElementString('UserDomainName',[Environment]::UserDomainName)
	$xmlWriter.WriteElementString('MachineName',[Environment]::MachineName)
	$xmlWriter.WriteElementString('PublishDate',[System.DateTime]::Now.ToString('yyyyMMdd-HHmm'))
	$xmlWriter.WriteEndElement()
	$xmlWriter.WriteEndDocument()
	$xmlWriter.Flush()
	$xmlWriter.Close()

	Write-Output ""Finished successfully""
}
catch
{
    Write-Error """"
    Write-Error ""ERROR DURING EXECUTION @ $([DateTime]::Now.ToString('yyyyMMdd-HHmm'))""
    Write-Error """"
    Write-Error ""  BEGIN Error Details:""
    Write-Error """"
    Write-Error ""   $_""
    Write-Error ""   IN FILE: $($_.InvocationInfo.ScriptName)""
    Write-Error ""   AT LINE: $($_.InvocationInfo.ScriptLineNumber) OFFSET: $($_.InvocationInfo.OffsetInLine)""
    Write-Error """"
    Write-Error ""  END   Error Details:""
    Write-Error """"
    Write-Error wr""ERROR DURING EXECUTION""
    Write-Error """"
    
    throw
}
}
";
            }
        }

        public static string UnzipFile
        {
            get
            {
                return @"
{
param(
	[string] $FilePath,
	[string] $TargetDirectoryPath
	)

try
{
	$shell_app=new-object -com shell.application
	$zip_file = $shell_app.namespace($FilePath)
	$destination = $shell_app.namespace($TargetDirectoryPath)
	$destination.Copyhere($zip_file.items())
	Write-Output ""Finished successfully""
}
catch
{
    Write-Error """"
    Write-Error ""ERROR DURING EXECUTION @ $([DateTime]::Now.ToString('yyyyMMdd-HHmm'))""
    Write-Error """"
    Write-Error ""  BEGIN Error Details:""
    Write-Error """"
    Write-Error ""   $_""
    Write-Error ""   IN FILE: $($_.InvocationInfo.ScriptName)""
    Write-Error ""   AT LINE: $($_.InvocationInfo.ScriptLineNumber) OFFSET: $($_.InvocationInfo.OffsetInLine)""
    Write-Error """"
    Write-Error ""  END   Error Details:""
    Write-Error """"
    Write-Error wr""ERROR DURING EXECUTION""
    Write-Error """"
    
    throw
}
}
";
            }
        }

        public static string UpdateItsConfigPrecedence
        {
            get
            {
                return @"
{
    param(
	    [string] $FilePath,
	    [string] $Environment
	    )

    try
    {
        [xml] $c = Get-Content $FilePath
        $n = $c.configuration.appSettings.add | ?{$_.key -eq 'Its.Configuration.Settings.Precedence'}
        $n.value = $Environment
        $c.Save($FilePath)
    }
    catch
    {
        Write-Error """"
        Write-Error ""ERROR DURING EXECUTION @ $([DateTime]::Now.ToString('yyyyMMdd-HHmm'))""
        Write-Error """"
        Write-Error ""  BEGIN Error Details:""
        Write-Error """"
        Write-Error ""   $_""
        Write-Error ""   IN FILE: $($_.InvocationInfo.ScriptName)""
        Write-Error ""   AT LINE: $($_.InvocationInfo.ScriptLineNumber) OFFSET: $($_.InvocationInfo.OffsetInLine)""
        Write-Error """"
        Write-Error ""  END   Error Details:""
        Write-Error """"
        Write-Error wr""ERROR DURING EXECUTION""
        Write-Error """"
    
        throw
    }
}
";
            }
        }
    }
}