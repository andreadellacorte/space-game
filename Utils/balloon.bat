@echo off

if "%~1"=="" call :printhelp  & exit /b 1
setlocal

if "%~2"=="" (set Icon=Information) else (set Icon=%2)
powershell -Command "[void] [System.Reflection.Assembly]::LoadWithPartialName('System.Windows.Forms'); $objNotifyIcon=New-Object System.Windows.Forms.NotifyIcon; $objNotifyIcon.BalloonTipText='%~1'; $objNotifyIcon.Icon=[system.drawing.systemicons]::%Icon%; $objNotifyIcon.BalloonTipTitle='%~3'; $objNotifyIcon.BalloonTipIcon='None'; $objNotifyIcon.Visible=$True; $objNotifyIcon.ShowBalloonTip(5000);"

endlocal
goto :eof

:printhelp
echo USAGE: %~n0 Text [Icon [Title]]
echo Icon can be: Application, Asterisk, Error, Exclamation, Hand, Information, Question, Shield, Warning or WinLogo
exit /b
