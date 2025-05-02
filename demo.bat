@echo off
REM ===========================================
REM Demo launch script for JoyBridge solution
REM ===========================================

echo [1/7] Starting containers
docker-compose up -d rabbitmq postgres

echo Waiting 10 seconds for containers to initialize...
timeout /t 10 /nobreak >nul

echo [2/7] Restoring and building all projects...
dotnet restore
dotnet build

echo [3/7] Launching Gateway
start "Gateway" cmd /k "dotnet run --project Gateway"

echo [4/7] Launching EventBridge 
start "EventBridge" cmd /k "dotnet run --project EventBridge"

echo [5/7] Launching PollingAgent
start "PollingAgent" cmd /k "dotnet run --project PollingAgent"

echo [6/7] Launching Simulator 
start "Simulator" cmd /k "dotnet run --project Simulator"

echo [7/7] Launching ThinClient
start "ThinClient" cmd /k "dotnet run --project ThinClient"

echo Environment initialized, use the opened windows to observe logs and the UI.

pause