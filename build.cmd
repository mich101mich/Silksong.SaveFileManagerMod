@REM Move to the directory of the script
cd /d "%~dp0"

dotnet build SaveFileManagerMod.csproj

@REM Make a sound to indicate that the build is complete
powershell -c "(New-Object Media.SoundPlayer 'C:\Windows\Media\Windows Notify System Generic.wav').PlaySync();"
