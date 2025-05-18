@echo off
title Jerry's Face Tracking Patcher
color 0f

set "FBX_TO_PATCH=RINDO.fbx"
set "DIFF_FILE=%FBX_TO_PATCH:~0,-4%_diff.hdiff"


:start
echo "     ______               _______             _    _               _____      _       _                   "
echo "    |  ____|             |__   __|           | |  (_)             |  __ \    | |     | |                  "
echo "    | |__ __ _  ___ ___     | |_ __ __ _  ___| | ___ _ __   __ _  | |__) |_ _| |_ ___| |__   ___ _ __     "
echo "    |  __/ _` |/ __/ _ \    | | '__/ _` |/ __| |/ / | '_ \ / _` | |  ___/ _` | __/ __| '_ \ / _ \ '__|    "
echo "    | | | (_| | (_|  __/    | | | | (_| | (__|   <| | | | | (_| | | |  | (_| | || (__| | | |  __/ |       "
echo "    |_|  \__,_|\___\___|    |_|_|  \__,_|\___|_|\_\_|_| |_|\__, | |_|   \__,_|\__\___|_| |_|\___|_|       " 
echo "                                                            __/ |                                         "
echo "                                                           |___/                                          "
echo                                   Jerry's Face Tracking Patcher v0.9
echo.
echo.
if not exist "%FBX_TO_PATCH%" echo Couldn't find FBX file to patch! Please make sure it's in the same directory as '%FBX_TO_PATCH:~0,-4%_FaceTrackingPatcher.bat' && echo FBX Name: %FBX_TO_PATCH% && echo. && pause && exit

copy "%FBX_TO_PATCH%" "%FBX_TO_PATCH%.bak"
FaceTrackingPatcher\hpatchz.exe -f ".\%FBX_TO_PATCH%" "FaceTrackingPatcher\%DIFF_FILE%" ".\%FBX_TO_PATCH%"

echo.
echo.

if %errorlevel% neq 0 (
  echo Error occurred during patching process. Double check the ReadMe.txt on avatar version and report issues to Jerry's Face Tracking Discord.
  echo.
  pause
  exit
)

echo Patch complete! Please read the documentation and instructions.

pause
exit
