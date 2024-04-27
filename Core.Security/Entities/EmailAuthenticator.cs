using Core.Persistence.Repositories;

namespace Core.Security.Entities;

public class EmailAuthenticator<TId, TUserId> : Entity<TId>
{
    public TUserId UserId { get; set; }
    public string? ActivationKey { get; set; }
    public bool IsVerified { get; set; }

    public EmailAuthenticator()
    {
        UserId = default!;
    }

    public EmailAuthenticator(TUserId userId, bool isVerified)
    {
        UserId = userId;
        IsVerified = isVerified;
    }

    public EmailAuthenticator(TId id, TUserId userId, bool isVerified) : base(id)
    {
        UserId = userId;
        IsVerified = isVerified;
    }
}
