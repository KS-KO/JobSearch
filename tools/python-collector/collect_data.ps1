# JobSearch 데이터 수집 자동화 스크립트
# 이 스크립트는 Python 수집기를 실행하여 사람인과 잡코리아의 최신 데이터를 DB에 업데이트합니다.

$CollectorPath = "c:\Project\JobSearch\tools\python-collector\main.py"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  JobSearch 데이터 수집을 시작합니다...  " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

if (Test-Path $CollectorPath) {
    # Python 스크립트 실행
    python $CollectorPath
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n[성공] 데이터 수집 및 DB 업데이트가 완료되었습니다." -ForegroundColor Green
    } else {
        Write-Host "`n[오류] 수집기 실행 중 문제가 발생했습니다." -ForegroundColor Red
    }
} else {
    Write-Host "[오류] 수집기 파일을 찾을 수 없습니다: $CollectorPath" -ForegroundColor Red
}

Write-Host "`nPress any key to exit..."
$Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
