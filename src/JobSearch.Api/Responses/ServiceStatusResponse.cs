namespace JobSearch.Api.Responses;

public sealed record ServiceStatusResponse(string Status, DateTimeOffset CheckedAtUtc);
