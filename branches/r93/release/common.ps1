function Clean-BinObj([string] $src)
{
	get-childitem -recurse -force $src -include bin,obj | % { 
		remove-item -recurse $_ 
	}
}

function Create-Folder([string] $path)
{
	if (test-path -path $path) { remove-item -recurse -force $path }
	new-item $path -itemtype directory
}

function Set-VsVars()
{
	$vstools = "."
	if (test-path env:VS90COMNTOOLS) { $vstools = $env:VS90COMNTOOLS } # 2008
	if (test-path env:VS100COMNTOOLS) { $vstools = $env:VS100COMNTOOLS } # 2010
	if (test-path env:VS110COMNTOOLS) { $vstools = $env:VS110COMNTOOLS } # 2012

	$batchFile = join-path $vstools "vsvars32.bat"

	$cmd = "`"$batchFile`" & set"
	cmd /c $cmd | % {
		$p, $v = $_.split('=')
		set-item -path env:$p -value $v
	}
	
	write-host -foreground Yellow "VsVars has been loaded from: '$batchFile'"
}

function Build-Solution([string] $sln, [string] $platform = $null)
{
	if ($platform -eq $null) { $platform = "Any CPU" }
	write-host -foreground Yellow "Rebuilding '$sln' for '$platform'"
	exec { msbuild $sln "/t:Build" "/p:Configuration=Release" "/p:Platform=$platform" }
}

function UpdateVersion-AssemblyInfo([string] $folder, [string] $version, [string[]] $excludes = $null) {
	if ($version -notmatch "[0-9]+(\.([0-9]+|\*)){1,3}") {
		Write-Error "Version number incorrect format: $version"
	}
	
	$versionPattern = 'AssemblyVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)'
	$versionAssembly = 'AssemblyVersion("' + $version + '")';
	$versionFilePattern = 'AssemblyFileVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)'
	$versionAssemblyFile = 'AssemblyFileVersion("' + $version + '")';
 
	get-childitem -recurse -include "AssemblyInfo.cs","AssemblyInfo.cpp" $folder | % {
		$filename = $_.fullname
		write-host "Found: $filename"
		
#		$update_assembly_and_file = $true
		
		# set an exclude flag where only AssemblyFileVersion is set
#		if ($excludes -ne $null) { $excludes | % { if ($filename -match $_) { $update_assembly_and_file = $false } } }
 
#		$tmp = ($file + ".tmp")
#		if (test-path ($tmp)) { remove-item $tmp }
 
#		if ($update_assembly_and_file) {
#			(get-content $filename) | % { $_ -replace $versionFilePattern, $versionAssemblyFile } | % { $_ -replace $versionPattern, $versionAssembly } > $tmp
#			write-host Updating file AssemblyInfo and AssemblyFileInfo: $filename --> $versionAssembly / $versionAssemblyFile
#		} else {
#			(get-content $filename) | % { $_ -replace $versionFilePattern, $versionAssemblyFile } > $tmp
#			write-host Updating file AssemblyInfo only: $filename --> $versionAssemblyFile
#		}
 
#		if (test-path ($filename)) { remove-item $filename }
#		move-item $tmp $filename -force	
	}
}