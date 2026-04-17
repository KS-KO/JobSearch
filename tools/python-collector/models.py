from dataclasses import asdict, dataclass


@dataclass(slots=True)
class RecommendationRecord:
    companyName: str
    jobTitle: str
    jobUrl: str
    ageGroup: str
    platform: str
    industry: str
    experienceLevel: str
    employmentType: str
    region: str
    salaryMillionKrw: int
    summary: str
    suitabilityScore: float

    def to_dict(self) -> dict[str, object]:
        return asdict(self)
