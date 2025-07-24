Write-Host "🎮 Starting ECS 3D Visual Demo..." -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "This will open a 3D graphics window with physics simulation" -ForegroundColor Yellow
Write-Host "Controls: SPACE=pause, 1-4=time scale, Mouse=camera, ESC=exit" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press any key to start the demo..." -ForegroundColor Green
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

dotnet run -- visual

Write-Host ""
Write-Host "Demo completed! Press any key to exit..." -ForegroundColor Green
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") 