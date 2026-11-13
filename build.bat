@echo off
chcp 65001 > nul
echo ========================================================
echo Сборка SchoolBellTimer под .NET Framework 3.5...
echo ========================================================
C:\Windows\Microsoft.NET\Framework\v3.5\csc.exe /target:winexe /out:SchoolBellTimer.exe /optimize+ /warn:4 Program.cs AppSettings.cs MainForm.cs ScheduleForm.cs Properties\AssemblyInfo.cs /reference:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Xml.dll,System.Core.dll
if %ERRORLEVEL% EQU 0 (
    echo [УСПЕХ] Файл SchoolBellTimer.exe успешно создан!
) else (
    echo [ОШИБКА] При сборке возникли ошибки.
)
pause
