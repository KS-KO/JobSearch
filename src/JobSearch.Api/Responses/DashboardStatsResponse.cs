namespace JobSearch.Api.Responses;

public sealed record DashboardStatsResponse(
    int TotalCount,
    int SaraminCount,
    int JobKoreaCount,
    string LastUpdatedTime
);
