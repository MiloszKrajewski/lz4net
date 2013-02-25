rem %1 is $(SolutionDir)
rem %2 is $(ConfigurationName)
rem %3 is $(TargetDir)
rem %4 is mixed mode assembly name

del %3\%4.dll
echo F | xcopy /y /d /f %1\bin\win32\%2\%4.dll %3\%4.x86.dll
echo F | xcopy /y /d /f %1\bin\x64\%2\%4.dll %3\%4.x64.dll
