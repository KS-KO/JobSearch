import sqlite3
from pathlib import Path
from models import RecommendationRecord
from scraper import scrape_saramin, scrape_jobkorea

def save_to_sqlite(records: list[RecommendationRecord], db_path: Path):
    """
    수집된 데이터를 SQLite DB에 저장합니다.
    (회사명, 공고제목)이 동일한 경우 중복 저장을 방지합니다.
    """
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    # 테이블 및 유니크 인덱스 생성 (중복 방지용)
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
    
    # 성능 및 정확성을 위해 중복 체크 인덱스 추가 (존재하지 않을 때만 생성)
    cursor.execute("CREATE UNIQUE INDEX IF NOT EXISTS idx_comp_title ON Recommendations(CompanyName, JobTitle)")
    
    count = 0
    for r in records:
        try:
            # INSERT OR IGNORE를 사용하여 동일한 (CompanyName, JobTitle)은 건너뜀
            cursor.execute("""
                INSERT OR IGNORE INTO Recommendations (
                    CompanyName, JobTitle, JobUrl, AgeGroup, Platform, 
                    Industry, ExperienceLevel, EmploymentType, Region, 
                    SalaryMillionKrw, Summary, SuitabilityScore
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                r.companyName, r.jobTitle, r.jobUrl, r.ageGroup, r.platform,
                r.industry, r.experienceLevel, r.employmentType, r.region,
                r.salaryMillionKrw, r.summary, r.suitabilityScore
            ))
            if cursor.rowcount > 0:
                count += 1
        except sqlite3.Error as e:
            print(f"DB Insert error: {e}")
    
    conn.commit()
    conn.close()
    return count

def main():
    root = Path(__file__).resolve().parent.parent.parent
    db_path = root / "JobSearch.db"
    
    print("=== JobSearch Data Collector (Phase 2) ===")
    
    keywords = ["개발자", "데이터 엔지니어", "영업"]
    age_groups = ["twenties", "thirties", "forties", "fiftiesAndAbove"]
    
    total_records = []
    
    for age in age_groups:
        for kw in keywords:
            # 1. 사람인 수집
            print(f"Scraping Saramin: [{kw}] for {age}...")
            saramin_records = scrape_saramin(kw, age)
            total_records.extend(saramin_records)
            
            # 2. 잡코리아 수집
            print(f"Scraping JobKorea: [{kw}] for {age}...")
            jobkorea_records = scrape_jobkorea(kw, age)
            total_records.extend(jobkorea_records)
            
            # 서버 부하 방지 및 차단 회피를 위한 딜레이
            import time
            time.sleep(2)
    
    if total_records:
        new_count = save_to_sqlite(total_records, db_path)
        print(f"Successfully processed {len(total_records)} records.")
        print(f"Newly added/updated: {new_count} records to {db_path}")
    else:
        print("No records collected.")

if __name__ == "__main__":
    main()
