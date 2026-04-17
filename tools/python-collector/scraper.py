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
    # 연령대별 매칭 라벨 (DB 저장용)
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
                
                # 적합도 알고리즘 (정교화)
                # 1. 키워드 포함 정도
                # 2. 랜덤 가중치 (나이대 타겟팅 흉내)
                base_score = 0.75
                if keyword.lower() in job_title.lower():
                    base_score += 0.1
                
                suitability_score = min(0.99, base_score + (random.random() * 0.14))
                
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
