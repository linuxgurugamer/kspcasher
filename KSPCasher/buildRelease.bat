
rem @echo off
cd ..
copy /Y "KSPCasher\bin\Release\KSPCasher.dll" "GameData\KSPCasher\Plugins"
copy /Y KSPCasher.version GameData\KSPCasher
copy /Y License.txt "GameData\KSPCasher"
copy /Y ..\MiniAVC.dll GameData\KSPCasher

set RELEASEDIR=d:\Users\jbb\release
set ZIP="c:\Program Files\7-zip\7z.exe"


set VERSIONFILE=KSPCasher.version
rem The following requires the JQ program, available here: https://stedolan.github.io/jq/download/
c:\local\jq-win64  ".VERSION.MAJOR" %VERSIONFILE% >tmpfile
set /P major=<tmpfile

c:\local\jq-win64  ".VERSION.MINOR"  %VERSIONFILE% >tmpfile
set /P minor=<tmpfile

c:\local\jq-win64  ".VERSION.PATCH"  %VERSIONFILE% >tmpfile
set /P patch=<tmpfile

c:\local\jq-win64  ".VERSION.BUILD"  %VERSIONFILE% >tmpfile
set /P build=<tmpfile
del tmpfile
set VERSION=%major%.%minor%.%patch%
if "%build%" NEQ "0"  set VERSION=%VERSION%.%build%

echo Version:  %VERSION%
rem copy /Y README.md GameData\KSPCasher
 

set FILE="%RELEASEDIR%\KSPCasherContinued-%VERSION%.zip"
IF EXIST %FILE% del /F %FILE%
%ZIP% a -tzip %FILE% GameData

pause
