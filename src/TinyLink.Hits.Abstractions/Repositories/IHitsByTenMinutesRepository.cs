using TinyLink.Hits.Abstractions.DataTransferObjects;

namespace TinyLink.Hits.Abstractions.Repositories;

public interface IHitsByTenMinutesRepository
{
    Task<List<CumulatedHitDto>> Get(Guid id, string ownerId, DateTimeOffset start, CancellationToken cancellationToken);
}