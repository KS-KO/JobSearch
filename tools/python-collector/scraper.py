import requests
from bs4 import BeautifulSoup
from models import RecommendationRecord
import time
import random

def scrape_saramin(keyword: str, age_group: str) -> list[RecommendationRecord]:
    """
    사람인 검색 결과 페이지를 기초적으로 크롤링합니다.
    (참고: 실제 운영 시에는 robots.txt 준수 및 공식 API 사용을 권장합니다.)
    """
    # 연령대별 매칭 라벨 (DB 저장용) 및 가중치 설명
    age_map = {
        "twenties": "Twenties",
        "thirties": "Thirties",
        "forties": "Forties",
        "fiftiesAndAbove": "FiftiesAndAbove"
    }
    
    encoded_keyword = requests.utils.quote(keyword)
    url = f"https://www.saramin.co.kr/zf_user/search/recruit?searchword={encoded_keyword}"
    
    headers = {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
    }
    
    records = []
    
    try:
        response = requests.get(url, headers=headers, timeout=10)
        response.raise_for_status()
        soup = BeautifulSoup(response.text, "html.parser")
        
        # 사람인 검색 결과 리스트 아이템 선택자
        job_items = soup.select(".item_recruit")
        
        for item in job_items[:10]: # 상위 10개만 수집 (데모 목적)
            try:
                company_name = item.select_one(".corp_name a").text.strip()
                job_title = item.select_one(".job_tit a").text.strip()
                job_href = item.select_one(".job_tit a")["href"]
                job_url = f"https://www.saramin.co.kr{job_href}"
                
                # 부가 정보 추출
                conditions = item.select(".job_condition span")
                region = conditions[0].text.strip() if len(conditions) > 0 else "N/A"
                exp = conditions[1].text.strip() if len(conditions) > 1 else "N/A"
                edu = conditions[2].text.strip() if len(conditions) > 2 else "N/A"
                emp_type = conditions[3].text.strip() if len(conditions) > 3 else "N/A"
                
                sector_tag = item.select_one(".job_sector")
                industry = sector_tag.text.strip().replace("\n", ", ") if sector_tag else "IT/SW"
                
                # 적합도 알고리즘 고도화
                # 1. 기본 점수
                suitability_score = 0.70
                
                # 2. 키워드 매칭 보너스 (제목 또는 가상 업종에서 검색어 발견 시)
                if keyword.lower() in job_title.lower():
                    suitability_score += 0.15
                if keyword.lower() in industry.lower():
                    suitability_score += 0.05
                
                # 3. 연령대별 선호 키워드/조건 가중치 (시뮬레이션 로직)
                # 예: 20대(신입/인턴), 40대 이상(경력/전문성)
                if age_group == "twenties" and ("신입" in exp or "인턴" in emp_type):
                    suitability_score += 0.05
                elif age_group in ["forties", "fiftiesAndAbove"] and ("경력" in exp or "전문" in job_title):
                    suitability_score += 0.05
                
                # 4. 미세 랜덤 변동 (동일 조건 내 순위 다양성 확보)
                suitability_score += (random.random() * 0.04)
                suitability_score = min(0.99, suitability_score)
                
                records.append(RecommendationRecord(
                    companyName=company_name,
                    jobTitle=job_title,
                    jobUrl=job_url,
                    ageGroup=age_map.get(age_group, "Twenties"),
                    platform="Saramin",
                    industry=industry,
                    experienceLevel=exp,
                    employmentType=emp_type,
                    region=region,
                    salaryMillionKrw=random.randint(35, 65),
                    summary=f"{edu} 학력 조건의 {exp} 채용 공고입니다.",
                    suitabilityScore=round(suitability_score, 2)
                ))
            except Exception as e:
                print(f"Error parsing item: {e}")
                continue
                
    except Exception as e:
        print(f"Scraping error: {e}")
        
    return records

def scrape_jobkorea(keyword: str, age_group: str) -> list[RecommendationRecord]:
    """
    잡코리아 전용 검색 API(POST /Search/api/display/v2/jobs)를 사용하여 데이터를 수집합니다.
    CSR(Client Side Rendering) 문제를 해결하고 수집 안정성을 높인 고도화된 방식입니다.
    """
    age_map = {
        "twenties": "Twenties",
        "thirties": "Thirties",
        "forties": "Forties",
        "fiftiesAndAbove": "FiftiesAndAbove"
    }
    
    api_url = "https://www.jobkorea.co.kr/Search/api/display/v2/jobs"
    
    headers = {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Accept-Language": "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7",
        "Content-Type": "application/json",
        "app-version": "1.0.0",
        "referer": f"https://www.jobkorea.co.kr/Search?stext={requests.utils.quote(keyword)}"
    }
    
    # API 요청 바디 구성
    payload = {
        "pageSize": 20,
        "page": 1,
        "sortProperty": "1",
        "sortDirection": "DESC",
        "keyword": keyword,
        "deviceType": "PC"
    }
    
    records = []
    
    try:
        # requests.Session을 사용하여 필요한 경우 쿠키 자동 관리
        session = requests.Session()
        response = session.post(api_url, headers=headers, json=payload, timeout=15)
        response.raise_for_status()
        
        data = response.json()
        job_list = data.get("content", [])
        
        for item in job_list[:10]:
            try:
                company_name = item.get("companyName", "N/A")
                job_title = item.get("title", "N/A")
                job_id = item.get("id", "")
                job_url = f"https://www.jobkorea.co.kr/Recruit/GI_Read/{job_id}" if job_id else "https://www.jobkorea.co.kr"
                
                # 지역 및 조건 정보 추출
                # API 응답에는 areaCodeList, careerLevel 등 가공이 필요한 필드가 포함되어 있을 수 있음
                # 여기서는 UI에 표시하기 좋게 간단히 정규화
                region = "서울/전국" # 기본값
                exp = "경력무관"
                
                if item.get("areaCodeList"):
                    # 실제 구체적 지역명 매핑 대신 데모용으로 간단히 처리
                    region = "수도권" if "B" in str(item.get("areaCodeList")) else "전국"
                
                # 적합도 계산 로직
                suitability_score = 0.72
                if keyword.lower() in job_title.lower():
                    suitability_score += 0.15
                
                # 연령대별 가중치 시뮬레이션
                if age_group == "twenties":
                    suitability_score += (random.random() * 0.05)
                elif age_group in ["forties", "fiftiesAndAbove"]:
                    suitability_score += (random.random() * 0.03)
                
                records.append(RecommendationRecord(
                    companyName=company_name,
                    jobTitle=job_title,
                    jobUrl=job_url,
                    ageGroup=age_map.get(age_group, "Twenties"),
                    platform="JobKorea",
                    industry=item.get("jobClassificationOrIndustry", "IT/서비스").replace(",", "/").strip("/"),
                    experienceLevel=exp,
                    employmentType="정규직",
                    region=region,
                    salaryMillionKrw=random.randint(35, 75),
                    summary=f"잡코리아 API 기반 고도화 추천 공고: {job_title}",
                    suitabilityScore=round(min(0.99, suitability_score), 2)
                ))
            except Exception as e:
                print(f"Error parsing API item: {e}")
                continue
                
    except Exception as e:
        print(f"JobKorea API Scraping error: {e}")
        
    return records
