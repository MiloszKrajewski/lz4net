function Clean-BinObj([string] $src)
{
	$folders = get-childitem -recurse -force $src -include bin,obj
	$folders | % {
		try {
			remove-item $_.fullname -recurse -force
		}
		catch {
			write-host -fore yellow $_
		}
	}
	$folders = get-childitem -recurse -force $src -include bin,obj
	$files = $folders | % { get-childitem $_.fullname * -recurse -file }
	$files | % { 
		try {
			remove-item $_.fullname 
		}
		catch {
			write-host -fore yellow $_
		}
	}
}

function Create-Folder([string] $path)
{
	if (test-path -path $path) { remove-item -recurse -force $path }
	new-item $path -itemtype directory
}

function Remove-Folder([string] $path)
{
	remove-item -recurse -force $path
}

function Set-VsVars()
{
	$vstools = "."
	if (test-path env:VS90COMNTOOLS) { $vstools = $env:VS90COMNTOOLS } # 2008
	if (test-path env:VS100COMNTOOLS) { $vstools = $env:VS100COMNTOOLS } # 2010
	if (test-path env:VS110COMNTOOLS) { $vstools = $env:VS110COMNTOOLS } # 2012
	if (test-path env:VS120COMNTOOLS) { $vstools = $env:VS120COMNTOOLS } # 2013
	
	$batchFile = join-path $vstools "vsvars32.bat"

	$cmd = "`"$batchFile`" & set"
	cmd /c $cmd | % {
		$p, $v = $_.split('=')
		set-item -path env:$p -value $v
	}
	
	write-host -foreground Yellow "VsVars has been loaded from: '$batchFile'"
}

function Clean-Solution([string] $sln)
{
	write-host -foreground Yellow "Cleaning '$sln'"
	exec { msbuild $sln "/t:Clean" "/v:minimal" }
}

function Build-Solution([string] $sln, [string] $platform = $null)
{
	if ($platform -eq $null) { $platform = "Any CPU" }
	write-host -foreground Yellow "Rebuilding '$sln' for '$platform'"
	exec { msbuild $sln "/t:Build" "/p:Configuration=Release" "/p:Platform=$platform" "/v:minimal" }
}

function Update-AssemblyVersion([string] $folder, [string] $version, [string[]] $excludes = $null) {
	if ($version -notmatch "[0-9]+(\.([0-9]+|\*)){1,3}") {
		Write-Error "Version number incorrect format: $version"
	}
	
	$aiProductVersionRx = '(?<=AssemblyVersion(Attribute)?\(")[0-9]+(\.([0-9]+|\*)){1,3}(?="\))'
	$aiFileVersionRx = '(?<=AssemblyFileVersion(Attribute)?\(")[0-9]+(\.([0-9]+|\*)){1,3}(?="\))'

    $rclProductVersionRx = "(?<=^\s*PRODUCTVERSION\s+)[0-9]+(\,([0-9]+|\*)){1,3}(?=\s*$)"
    $rclFileVersionRx = "(?<=^\s*FILEVERSION\s+)[0-9]+(\,([0-9]+|\*)){1,3}(?=\s*$)"

    $rcvProductVersionRx = '(?<=^\s*VALUE\s+"ProductVersion",\s*")[0-9]+(\.([0-9]+|\*)){1,3}(?="\s*$)'
    $rcvFileVersionRx = '(?<=^\s*VALUE\s+"FileVersion",\s*")[0-9]+(\.([0-9]+|\*)){1,3}(?="\s*$)'
	
	get-childitem -recurse -include "AssemblyInfo.cs","AssemblyInfo.cpp","app.rc" $folder | % {
		$name = resolve-path $_.FullName -relative
        $isrc = $_.Extension -eq ".rc"
		
		$update_assembly_and_file = $true
		
		# set an exclude flag where only AssemblyFileVersion is set
		if ($excludes -ne $null) { $excludes | % { if ($name -match $_) { $update_assembly_and_file = $false } } }
 
		$tmp = ($name + ".tmp")
		if (test-path ($tmp)) { remove-item $tmp }
		
		$content = $original = get-content $name

        if ($isrc) {
            $content = $content | % { $_ -replace $rclFileVersionRx, ($version -replace "\.", ",") }
            $content = $content | % { $_ -replace $rcvFileVersionRx, $version }
        } else {
			$content = $content | % { $_ -replace $aiFileVersionRx, $version }
		}

		if ($update_assembly_and_file) {
            if ($isrc) {
                $content = $content | % { $_ -replace $rclProductVersionRx, ($version -replace "\.", ",") }
                $content = $content | % { $_ -replace $rcvProductVersionRx, $version }
            } else {
				$content = $content | % { $_ -replace $aiProductVersionRx, $version }
			}
		}
		
		if ((compare-object $content $original).length -gt 0) {
			if ($update_assembly_and_file) {
				write-host "Updating AssemblyVersion for '$name' to '$version'"
			} else {
				write-host "Updating FileVersion only for '$name' to '$version'"
			}
			
			$encoding = New-Object System.Text.UTF8Encoding($False)
			[System.IO.File]::WriteAllLines($tmp, $content, $encoding)
		
		    if (test-path ($name)) { remove-item $name }
		    move-item $tmp $name -force	
		}
	}
}