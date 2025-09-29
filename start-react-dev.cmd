@echo off
REM Simple script: start React dev server

cd /d "%~dp0"

pushd "react-client"

if not exist node_modules (
  echo Installing npm dependencies...
  call npm install
  if errorlevel 1 (
    echo npm install failed.
    pause
    popd
    exit /b 1
  )
)

echo Starting dev server...
call npm run dev

popd
echo Dev server exited.
pause