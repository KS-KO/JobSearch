namespace JobSearch.Contracts.Responses;

public sealed record ServiceStatusResponse(string Status, DateTimeOffset CheckedAtUtc);
