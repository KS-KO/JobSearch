namespace JobSearch.Contracts.Responses;

public sealed record RecommendationSearchResponse(
    int TotalCount,
    IReadOnlyList<RecommendationItemResponse> Items);
