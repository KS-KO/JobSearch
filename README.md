# JobSearch

PRD 기반으로 구성한 `C# 메인 서비스 + Python 수집기` 프로젝트입니다.

## Structure

- `src/JobSearch.Api`: ASP.NET Core Web API 진입점
- `src/JobSearch.Desktop`: WPF + MVVM 메인 프로그램
- `src/JobSearch.Application`: 추천 유스케이스와 서비스 계층
- `src/JobSearch.Domain`: 핵심 엔티티와 도메인 규칙
- `src/JobSearch.Infrastructure`: JSON 기반 데이터 적재와 인프라 구현
- `src/JobSearch.Contracts`: API 요청/응답 DTO
- `tests/JobSearch.UnitTests`: 외부 테스트 프레임워크 연결 전 기본 검증 프로젝트
- `tools/python-collector`: 샘플 추천 데이터 수집기

## Run

```powershell
.\build.ps1
dotnet run --project .\src\JobSearch.Api\JobSearch.Api.csproj
dotnet run --project .\src\JobSearch.Desktop\JobSearch.Desktop.csproj
python .\tools\python-collector\main.py
```
