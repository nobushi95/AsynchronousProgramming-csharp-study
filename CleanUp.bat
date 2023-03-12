@echo off
echo delete obj
for /R %%d in (obj) do ( if exist "%%d" (rmdir /S /Q "%%d" ) )

echo delete bin
for /R %%d in (bin) do ( if exist "%%d" (rmdir /S /Q "%%d" ) )

@REM echo delete .vs
@REM for /R %%d in (.vs) do ( if exist "%%d" ( rmdir /S /Q "%%d" ) )

pause
