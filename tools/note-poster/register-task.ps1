# register-task.ps1
# Windows Task Scheduler に週次自動投稿タスクを登録する
# 管理者権限で実行: PowerShell を「管理者として実行」してから
#   cd f:\Claude\note-poster; .\register-task.ps1

$TaskName = "IndieBlog-NotePost"
$ScriptDir = "f:\Claude\note-poster"
$NodePath = (Get-Command node -ErrorAction Stop).Source

# 既存タスクを削除（再登録時）
if (Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue) {
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
    Write-Host "既存タスクを削除しました"
}

# アクション: node post-to-note.js を実行
$Action = New-ScheduledTaskAction `
    -Execute $NodePath `
    -Argument "post-to-note.js" `
    -WorkingDirectory $ScriptDir

# トリガー: 毎週日曜 22:00
$Trigger = New-ScheduledTaskTrigger `
    -Weekly `
    -DaysOfWeek Sunday `
    -At "22:00"

# 設定: ネットワーク接続時のみ実行、失敗時は1時間後に再試行
$Settings = New-ScheduledTaskSettingsSet `
    -RunOnlyIfNetworkAvailable `
    -RestartCount 2 `
    -RestartInterval (New-TimeSpan -Hours 1) `
    -ExecutionTimeLimit (New-TimeSpan -Hours 1)

# タスク登録（現在のユーザーで実行）
Register-ScheduledTask `
    -TaskName $TaskName `
    -Action $Action `
    -Trigger $Trigger `
    -Settings $Settings `
    -RunLevel Highest `
    -Description "週刊インディーゲームブログを note.com に自動投稿（毎週日曜 22:00）"

Write-Host ""
Write-Host "✅ タスク登録完了: $TaskName"
Write-Host "   スケジュール: 毎週日曜 22:00"
Write-Host "   スクリプト: $ScriptDir\post-to-note.js"
Write-Host ""
Write-Host "確認コマンド: Get-ScheduledTask -TaskName '$TaskName'"
Write-Host "手動実行:     Start-ScheduledTask -TaskName '$TaskName'"
Write-Host "削除:         Unregister-ScheduledTask -TaskName '$TaskName' -Confirm:`$false"
