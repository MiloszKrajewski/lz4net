@echo off

setlocal ENABLEDELAYEDEXPANSION
set FILES=D:\Archive\Corpus
rem set FILES=T:\Temp\Corpus
set PROJECT=LZ4.Tests.Continuous

:cycle

call :run MixedMode64HC
call :run MixedMode32HC
call :run CppCLI64HC
call :run CppCLI32HC
call :run Unsafe64HC
call :run Unsafe32HC
call :run Safe64HC
call :run Safe32HC

goto :cycle

:run
call :echo %1 @ 64-bit
..\source\%PROJECT%\bin\x64\Release\%PROJECT%.exe compare_lz4hc_implementations.xml %FILES% 1024 %1
call :echo %1 @ 32-bit
..\source\%PROJECT%\bin\x86\Release\%PROJECT%.exe compare_lz4hc_implementations.xml %FILES% 1024 %1
exit /b

:echo
echo.------------------------------------------------------------------------------
echo.%*
echo.------------------------------------------------------------------------------
exit /b
