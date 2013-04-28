@echo off
set VS=UNKNOWN
if not "%VS80COMNTOOLS%"=="" set VS=%VS80COMNTOOLS%
if not "%VS90COMNTOOLS%"=="" set VS=%VS90COMNTOOLS%
if not "%VS100COMNTOOLS%"=="" set VS=%VS100COMNTOOLS%
call "%VS%\vsvars32.bat"

sn -k LZ4.snk