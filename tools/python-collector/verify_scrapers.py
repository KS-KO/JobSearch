
from scraper import scrape_saramin, scrape_jobkorea
import json

def verify():
    keyword = "Python"
    age_group = "twenties"

    print(f"--- Verifying Saramin with keyword: {keyword} ---")
    saramin_results = scrape_saramin(keyword, age_group)
    print(f"Found {len(saramin_results)} results for Saramin")
    for i, res in enumerate(saramin_results[:3]):
        print(f"Result {i+1}: {res.companyName} - {res.jobTitle} ({res.jobUrl})")

    print(f"\n--- Verifying Jobkorea with keyword: {keyword} ---")
    jobkorea_results = scrape_jobkorea(keyword, age_group)
    print(f"Found {len(jobkorea_results)} results for Jobkorea")
    for i, res in enumerate(jobkorea_results[:3]):
        print(f"Result {i+1}: {res.companyName} - {res.jobTitle} ({res.jobUrl})")

if __name__ == "__main__":
    verify()
