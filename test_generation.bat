@echo off
echo Testing config generation...
echo Current directory: %CD%

echo Checking Excel files...
dir Assets\ExcelConfigs\*.xlsx

echo Checking existing JSON files...
dir Assets\GameRes\JsonConfigs\ 2>nul

echo Checking existing CS files...
dir Assets\Scripts\Configs\*.cs

echo Done.
pause
