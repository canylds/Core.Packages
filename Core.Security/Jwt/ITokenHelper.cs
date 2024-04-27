using Core.Security.Entities;

namespace Core.Security.Jwt;

public interface ITokenHelper<TUserId, TOperationClaimId, TRefreshTokenId>
{
    public AccessToken CreateToken(User<TUserId> user, IList<OperationClaim<TOperationClaimId>> operationClaims);
    public RefreshToken<TRefreshTokenId, TUserId> CreateRefreshToken(User<TUserId> user, string ipAddress);
}