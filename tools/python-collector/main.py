import sqlite3
from pathlib import Path
from models import RecommendationRecord
from scraper import scrape_saramin

def save_to_sqlite(records: list[RecommendationRecord], db_path: Path):
    """
    수집된 데이터를 SQLite DB에 저장합니다.
    Insert or Replace 방식을 사용하여 중복 데이터는 업데이트합니다.
    """
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    # 테이블이 없으면 생성 (C# 쪽에서 CreateDeepCreated가 수행되지만 보조적으로 확인)
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS Recommendations (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CompanyName TEXT NOT NULL,
            JobTitle TEXT NOT NULL,
            JobUrl TEXT NOT NULL,
            AgeGroup TEXT NOT NULL,
            Platform TEXT,
            Industry TEXT,
            ExperienceLevel TEXT,
            EmploymentType TEXT,
            Region TEXT,
            SalaryMillionKrw INTEGER,
            Summary TEXT,
            SuitabilityScore REAL
        )
    """)
    
    for r in records:
        cursor.execute("""
            INSERT INTO Recommendations (
                CompanyName, JobTitle, JobUrl, AgeGroup, Platform, 
                Industry, ExperienceLevel, EmploymentType, Region, 
                SalaryMillionKrw, Summary, SuitabilityScore
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, (
            r.companyName, r.jobTitle, r.jobUrl, r.ageGroup, r.platform,
            r.industry, r.experienceLevel, r.employmentType, r.region,
            r.salaryMillionKrw, r.summary, r.suitabilityScore
        ))
    
    conn.commit()
    conn.close()

def main():
    root = Path(__file__).resolve().parent.parent.parent
    db_path = root / "JobSearch.db"
    
    print("Starting data collection for multiple age groups...")
    
    keywords = ["개발자", "경영지원", "영업"]
    age_groups = ["twenties", "thirties", "forties", "fiftiesAndAbove"]
    
    total_records = []
    
    for age in age_groups:
        for kw in keywords:
            print(f"Scraping Saramin for [{kw}] targeted at [{age}]...")
            records = scrape_saramin(kw, age)
            total_records.extend(records)
            # 서버 부하 방지를 위한 딜레이
            import time
            time.sleep(1)
    
    if total_records:
        save_to_sqlite(total_records, db_path)
        print(f"Successfully saved {len(total_records)} records to {db_path}")
    else:
        print("No records collected.")

if __name__ == "__main__":
    main()
