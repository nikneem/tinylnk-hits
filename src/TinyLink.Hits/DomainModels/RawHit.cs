using HexMaster.DomainDrivenDesign;
using HexMaster.DomainDrivenDesign.ChangeTracking;
using TinyLink.Hits.Abstractions.DomainModels;

namespace TinyLink.Hits.DomainModels;

public class RawHit : DomainModel<Guid>, IRawHit
{

    public Guid ShortCodeId { get; }
    public string OwnerId { get; }
    public string ShortCode { get; }
    public DateTimeOffset CreatedOn { get; }

    public RawHit(Guid id, string ownerId, Guid shortCodeId, string shortCode, DateTimeOffset createdOn) : base(id)
    {
        OwnerId = ownerId;
        ShortCodeId = shortCodeId;
        ShortCode = shortCode;
        CreatedOn = createdOn;
    }

    private RawHit(string ownerId, Guid shortCodeId, string shortCode) : base(Guid.NewGuid(), TrackingState.New)
    {
        OwnerId = ownerId;
        ShortCodeId = shortCodeId;
        ShortCode = shortCode;
        CreatedOn = DateTimeOffset.UtcNow;
    }

    public static IRawHit Create(string ownerId, Guid shortCodeId, string shortCode)
    {
        return new RawHit(ownerId, shortCodeId, shortCode);
    }

}