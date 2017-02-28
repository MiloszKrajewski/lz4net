rem %1 is $(SolutionDir)
rem %2 is $(ConfigurationName)
rem %3 is $(TargetDir)
rem %4 is mixed mode assembly name

del %3\%4.dll
call :copy %1\bin\win32\%2\%4.dll %3\%4.x86.dll
call :copy %1\bin\x64\%2\%4.dll %3\%4.x64.dll
exit /b 0

:copy
echo %1 -- %2
echo F | xcopy /y /d /f %1 %2
exit /b
