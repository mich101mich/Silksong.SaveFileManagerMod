@REM Move to the directory of the script
cd /d "%~dp0"

dotnet build SaveFileManagerMod.csproj

@REM Make a sound to indicate that the build is complete. Use different sounds for success and failure.
if %errorlevel% neq 0 (
    powershell -c "(New-Object Media.SoundPlayer 'C:\Windows\Media\Windows Critical Stop.wav').PlaySync();"
) else (
    powershell -c "(New-Object Media.SoundPlayer 'C:\Windows\Media\Windows Notify System Generic.wav').PlaySync();"
)
