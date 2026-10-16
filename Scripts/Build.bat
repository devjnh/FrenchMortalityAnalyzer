@ECHO OFF
REM Builds the Windows (.NET Framework 4.7.2) release and packages it in Release\FrenchMortality.zip
REM Package layout:
REM   FrenchMortality\FrenchMortalityAnalyzer.exe (+ .exe.config, which makes it look for the dll files in Bin)
REM   FrenchMortality\Bin\*.dll, Bin\x86 and Bin\x64 (native SQLite)
REM   FrenchMortality\*.bat (examples, from Scripts\Package)
SETLOCAL
FOR %%I IN ("%~dp0..") DO SET RootFolder=%%~fI
SET BuildFolder=%RootFolder%\FrenchMortalityAnalyzer\bin\Release\net472
SET ReleaseFolder=%RootFolder%\Release
SET PackageFolder=%ReleaseFolder%\FrenchMortality
SET ZipFile=%ReleaseFolder%\FrenchMortality.zip

dotnet build "%RootFolder%\FrenchMortalityAnalyzer\FrenchMortalityAnalyzer.csproj" -c Release -f net472 --no-incremental
IF ERRORLEVEL 1 GOTO Error

IF EXIST "%PackageFolder%" RMDIR /S /Q "%PackageFolder%"
MKDIR "%PackageFolder%\Bin" || GOTO Error
COPY /Y "%BuildFolder%\FrenchMortalityAnalyzer.exe" "%PackageFolder%" > NUL || GOTO Error
COPY /Y "%BuildFolder%\FrenchMortalityAnalyzer.exe.config" "%PackageFolder%" > NUL || GOTO Error
COPY /Y "%BuildFolder%\*.dll" "%PackageFolder%\Bin" > NUL || GOTO Error
XCOPY "%BuildFolder%\x86" "%PackageFolder%\Bin\x86" /I /Y /Q > NUL || GOTO Error
XCOPY "%BuildFolder%\x64" "%PackageFolder%\Bin\x64" /I /Y /Q > NUL || GOTO Error
COPY /Y "%~dp0Package\*.bat" "%PackageFolder%" > NUL || GOTO Error

IF EXIST "%ZipFile%" MOVE /Y "%ZipFile%" "%ReleaseFolder%\FrenchMortality.bak.zip" > NUL
PUSHD "%ReleaseFolder%"
"%SystemRoot%\System32\tar.exe" -a -c -f FrenchMortality.zip FrenchMortality
IF ERRORLEVEL 1 (POPD & GOTO Error)
POPD
ECHO.
ECHO Package created: %ZipFile%
GOTO End

:Error
ECHO.
ECHO Build failed!
EXIT /B 1

:End
PAUSE
