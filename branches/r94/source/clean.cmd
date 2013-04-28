@echo off

for /r . %%v in (bin; obj) do if exist "%%v" call :RemoveFolder "%%v"
pause
goto :eof

:RemoveFolder
echo removing %1...
rmdir /s /q %1
exit /b
