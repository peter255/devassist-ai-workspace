@echo off
cd /d "%~dp0.."
if not exist "frontend\devassist-ui\node_modules" (
  echo Installing frontend dependencies...
  npm install --prefix frontend\devassist-ui
)
echo Starting DevAssist (API + SPA proxy, Visual Studio style)...
dotnet run --project src\DevAssist.Api --launch-profile http
