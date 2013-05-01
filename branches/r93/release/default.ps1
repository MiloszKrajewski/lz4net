Properties {
	$src = "..\source"
	$sln = "$src\LZ4.sln"
	$snk = "$src\LZ4.snk"
	$libz = "$src\packages\LibZ.Bootstrap.1.0.3.3\tools\libz.exe"
}

Include ".\common.ps1"

Task default -depends LibZ

Task LibZ {
	Create-Folder libz
	
	copy-item any\*.dll libz\
	
	exec { cmd /c $libz inject-dll -a libz\LZ4.dll -i libz\*.dll -e LZ4.dll "--move" -k $snk }
	
	UpdateVersion-AssemblyInfo $src 1.2.3.4
}

Task Release -depends Rebuild {
	Create-Folder x86
	Create-Folder x64
	Create-Folder any

	copy-item "$src\LZ4\bin\Release\LZ4.dll" any\
	copy-item "$src\LZ4n\bin\Release\LZ4n.dll" any\
	copy-item "$src\LZ4s\bin\Release\LZ4s.dll" any\

	copy-item "$src\bin\Win32\Release\*.dll" x86\
	copy-item "$src\bin\x64\Release\*.dll" x64\

	copy-item x86\LZ4mm.dll any\LZ4mm.x86.dll
	copy-item x86\LZ4cc.dll any\LZ4cc.x86.dll

	copy-item x64\LZ4mm.dll any\LZ4mm.x64.dll
	copy-item x64\LZ4cc.dll any\LZ4cc.x64.dll
}

Task Rebuild -depends VsVars,Clean,KeyGen {
	Build-Solution $sln "Any CPU"
	Build-Solution $sln x86
	Build-Solution $sln x64
}

Task KeyGen -depends VsVars {
	if (!(test-path $snk)) { exec { cmd /c sn -k $snk } }
}

Task Clean {
	Clean-BinObj $src
	remove-item -recurse -force x86
	remove-item -recurse -force x64
	remove-item -recurse -force any
	remove-item -recurse -force libz
}

Task VsVars {
	Set-VsVars
}
