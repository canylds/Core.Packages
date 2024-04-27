using Core.Persistence.Repositories;

namespace Core.Security.Entities;

public class UserOperationClaim<TId, TUserId, TOperationClaimId> : Entity<TId>
{
    public TUserId UserId { get; set; }
    public TUserId OperationClaimId { get; set; }

    public UserOperationClaim()
    {
        UserId = default!;
        OperationClaimId = default!;
    }

    public UserOperationClaim(TUserId userId, TUserId operationClaimId)
    {
        UserId = userId;
        OperationClaimId = operationClaimId;
    }

    public UserOperationClaim(TId id, TUserId userId, TUserId operationClaimId) : base(id)
    {
        UserId = userId;
        OperationClaimId = operationClaimId;
    }
}
