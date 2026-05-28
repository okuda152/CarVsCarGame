# CarVsCarGame クイックセーブ
# ダブルクリックまたは PowerShell から実行するだけで現在の状態をコミット＆プッシュ

$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm"
$message   = "save: $timestamp"

git -C $PSScriptRoot add Assets/ ProjectSettings/ Packages/
git -C $PSScriptRoot commit -m $message

if ($LASTEXITCODE -eq 0) {
    git -C $PSScriptRoot push origin master
    Write-Host "✅ セーブ完了: $message" -ForegroundColor Green
} else {
    Write-Host "⚠️ 変更なし（セーブ不要）" -ForegroundColor Yellow
}

Read-Host "Enterで閉じる"
