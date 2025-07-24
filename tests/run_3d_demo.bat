@echo off
echo 🎮 Starting ECS 3D Visual Demo...
echo ================================
echo.
echo This will open a 3D graphics window with physics simulation
echo Controls: SPACE=pause, 1-4=time scale, Mouse=camera, ESC=exit
echo.
echo Press any key to start the demo...
pause >nul

dotnet run -- visual

echo.
echo Demo completed! Press any key to exit...
pause >nul 