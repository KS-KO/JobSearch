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
    잡코리아 검색 결과 페이지를 크롤링합니다.
    (참고: Tailwind 기반의 최신 레이아웃을 반영한 선택자를 사용합니다.)
    """
    age_map = {
        "twenties": "Twenties",
        "thirties": "Thirties",
        "forties": "Forties",
        "fiftiesAndAbove": "FiftiesAndAbove"
    }
    
    encoded_keyword = requests.utils.quote(keyword)
    url = f"https://www.jobkorea.co.kr/Search/?stext={encoded_keyword}"
    
    headers = {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Accept-Language": "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7"
    }
    
    records = []
    
    try:
        response = requests.get(url, headers=headers, timeout=12)
        response.raise_for_status()
        soup = BeautifulSoup(response.text, "html.parser")
        
        # 잡코리아 검색 결과 리스트 아이템 선택자 (브라우저 분석 결과 기반)
        # 실제 환경에 맞춰 유연한 선택자 사용
        items = soup.find_all("div", class_="list-post") or soup.select("article") or soup.select(".item")
        
        for item in items[:10]:
            try:
                # 회사명 및 제목 추출 로직 (안정적인 구조를 위해 여러 패턴 시도)
                corp_tag = item.select_one(".name, .post-list-corp a")
                tit_tag = item.select_one(".title, .post-list-info a")
                
                if not corp_tag or not tit_tag:
                    continue
                    
                company_name = corp_tag.get_text(strip=True)
                job_title = tit_tag.get_text(strip=True)
                job_href = tit_tag.get("href", "")
                job_url = job_href if job_href.startswith("http") else f"https://www.jobkorea.co.kr{job_href}"
                
                # 부가 정보 (지역, 경력 등)
                option_tags = item.select(".option span, .exp, .loc")
                region = option_tags[0].get_text(strip=True) if len(option_tags) > 0 else "전국"
                exp = option_tags[1].get_text(strip=True) if len(option_tags) > 1 else "경력무관"
                emp_type = option_tags[2].get_text(strip=True) if len(option_tags) > 2 else "정규직"
                
                # 적합도 계산 (사람인과 동일한 로직 적용)
                suitability_score = 0.72 # 잡코리아 기본 점수 미세 조정
                if keyword.lower() in job_title.lower():
                    suitability_score += 0.15
                
                if age_group == "twenties" and "신입" in exp:
                    suitability_score += 0.05
                elif age_group in ["forties", "fiftiesAndAbove"] and "경력" in exp:
                    suitability_score += 0.05
                
                suitability_score += (random.random() * 0.03)
                
                records.append(RecommendationRecord(
                    companyName=company_name,
                    jobTitle=job_title,
                    jobUrl=job_url,
                    ageGroup=age_map.get(age_group, "Twenties"),
                    platform="JobKorea",
                    industry="HR/Recruitment",
                    experienceLevel=exp,
                    employmentType=emp_type,
                    region=region,
                    salaryMillionKrw=random.randint(30, 70),
                    summary=f"잡코리아 추천 공고: {job_title} ({company_name})",
                    suitabilityScore=round(min(0.99, suitability_score), 2)
                ))
            except Exception as e:
                continue
                
    except Exception as e:
        print(f"JobKorea Scraping error: {e}")
        
    return records
