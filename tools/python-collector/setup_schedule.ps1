# JobSearch 자동 수집 스케줄러 등록 스크립트
# 관리자 권한으로 실행하거나, 스케줄러에 등록하여 매일 자동으로 수집할 수 있게 설정합니다.

$TaskName = "JobSearchDataCollector"
$ScriptPath = "c:\Project\JobSearch\tools\python-collector\collect_data.ps1"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host " JobSearch 데이터 자동 수집 스케줄러 등록 시작 " -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

try {
    # 1. 실행할 동작 정의 (PowerShell 스크립트 백그라운드 실행)
    $Action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-ExecutionPolicy Bypass -WindowStyle Hidden -File ""$ScriptPath"""

    # 2. 실행 트리거 정의 (매일 오전 9시)
    $Trigger = New-ScheduledTaskTrigger -Daily -At 9am

    # 3. 추가 설정 (배터리 사용 시에도 실행되도록 허용 등)
    $Settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable

    # 4. 스케줄러 등록 (동일한 이름이 있으면 덮어쓰기)
    Register-ScheduledTask -TaskName $TaskName -Action $Action -Trigger $Trigger -Settings $Settings -Description "매일 오전 9시에 사람인 및 잡코리아 채용 데이터를 자동 수집하여 JobSearch.db에 업데이트합니다." -Force | Out-Null
    
    Write-Host "`n[성공] '$TaskName' 작업이 Windows 스케줄러에 등록되었습니다." -ForegroundColor Green
    Write-Host "매일 오전 9시에 자동으로 백그라운드에서 실행됩니다." -ForegroundColor Yellow
}
catch {
    Write-Host "`n[오류] 스케줄러 등록에 실패했습니다. 관리자 권한으로 터미널을 실행했는지 확인해주세요." -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Gray
}

Write-Host "`nPress any key to exit..."
$Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") | Out-Null
