@echo off
REM 神の降る街 - 編集チーム起動スクリプト
REM
REM 使用前に ANTHROPIC_API_KEY を設定してください:
REM   set ANTHROPIC_API_KEY=sk-ant-あなたのキー
REM
REM 使用方法:
REM   run_editorial_team.bat <ファイルパス>
REM   run_editorial_team.bat --text "原稿テキスト"

cd /d "%~dp0"

if "%ANTHROPIC_API_KEY%"=="" (
    echo [エラー] ANTHROPIC_API_KEY が設定されていません。
    echo.
    echo 以下のコマンドでAPIキーを設定してください:
    echo   set ANTHROPIC_API_KEY=sk-ant-あなたのキー
    echo.
    echo APIキーは https://console.anthropic.com/ から取得できます。
    pause
    exit /b 1
)

echo 編集チームを起動します...
node novel_editorial_team.js %*
