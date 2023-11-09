namespace TinyLink.Hits.Abstractions.DataTransferObjects;

public record CumulatedHitDto(string dateTimeKey, DateTimeOffset dateTime, int totalHits);