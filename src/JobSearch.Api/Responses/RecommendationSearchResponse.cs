namespace JobSearch.Api.Responses;

public sealed record RecommendationSearchResponse(
    int TotalCount,
    IReadOnlyList<RecommendationItemResponse> Items);
