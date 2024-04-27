using Core.Persistence.Repositories;

namespace Core.Security.Entities;

public class RefreshToken<TId, TUserId> : Entity<TId>
{
    public TUserId UserId { get; set; }
    public string Token { get; set; }
    public DateTime Expires { get; set; }
    public string CreatedByIp { get; set; }
    public DateTime? Revoked { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? ReasonRevoked { get; set; }

    public RefreshToken()
    {
        UserId = default!;
        Token = string.Empty;
        CreatedByIp = string.Empty;
    }

    public RefreshToken(TUserId userId, string token, DateTime expires, string createdByIp)
    {
        UserId = userId;
        Token = token;
        Expires = expires;
        CreatedByIp = createdByIp;
    }

    public RefreshToken(TId id, TUserId userId, string token, DateTime expires, string createdByIp) : base(id)
    {
        UserId = userId;
        Token = token;
        Expires = expires;
        CreatedByIp = createdByIp;
    }
}
