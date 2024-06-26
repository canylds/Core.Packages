﻿using Core.Security.Encryption;
using Core.Security.Entities;
using Core.Security.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Core.Security.Jwt;

public class JwtHelper<TUserId, TOperationClaimId, TRefreshTokenId> : ITokenHelper<TUserId, TOperationClaimId, TRefreshTokenId>
{
    public IConfiguration Configuration { get; }
    private readonly TokenOptions _tokenOptions;
    private DateTime _accessTokenExpiration;

    public JwtHelper(IConfiguration configuration)
    {
        Configuration = configuration;
        const string configurationSection = "TokenOptions";
        _tokenOptions = Configuration.GetSection(configurationSection).Get<TokenOptions>()
            ?? throw new NullReferenceException($"\"{configurationSection}\" section cannot found in configuration.");
    }

    public RefreshToken<TRefreshTokenId, TUserId> CreateRefreshToken(User<TUserId> user, string ipAddress)
    {
        RefreshToken<TRefreshTokenId, TUserId> refreshToken = new()
        {
            UserId = user.Id,
            Token = RandomRefreshToken(),
            Expires = DateTime.UtcNow.AddDays(_tokenOptions.RefreshTokenTTL),
            CreatedByIp = ipAddress
        };

        return refreshToken;
    }

    public AccessToken CreateToken(User<TUserId> user, IList<OperationClaim<TOperationClaimId>> operationClaims)
    {
        _accessTokenExpiration = DateTime.Now.AddMinutes(_tokenOptions.AccessTokenExpiration);

        SecurityKey securityKey = SecurityKeyHelper.CreateSecurityKey(_tokenOptions.SecurityKey);

        SigningCredentials signingCredentials = SigningCredentialsHelper.CreateSigningCredentials(securityKey);

        JwtSecurityToken jwt = CreateJwtSecurityToken(_tokenOptions, user, signingCredentials, operationClaims);

        JwtSecurityTokenHandler jwtSecurityTokenHandler = new();

        string? token = jwtSecurityTokenHandler.WriteToken(jwt);

        return new AccessToken
        {
            Token = token,
            Expiration = _accessTokenExpiration
        };
    }

    public JwtSecurityToken CreateJwtSecurityToken(TokenOptions tokenOptions,
        User<TUserId> user,
        SigningCredentials signingCredentials,
        IList<OperationClaim<TOperationClaimId>> operationClaims)
    {
        JwtSecurityToken jwt = new(tokenOptions.Issuer,
            tokenOptions.Audience,
            expires: _accessTokenExpiration,
            notBefore: DateTime.Now,
            claims: SetClaims(user, operationClaims),
            signingCredentials: signingCredentials);

        return jwt;
    }

    private IEnumerable<Claim> SetClaims(User<TUserId> user, IList<OperationClaim<TOperationClaimId>> operationClaims)
    {
        List<Claim> claims = [];

        claims.AddNameIdentifier(user.Id!.ToString()!);
        claims.AddEmail(user.Email);
        claims.AddName($"{user.FirstName} {user.LastName}");
        claims.AddRoles(operationClaims.Select(c => c.Name).ToArray());

        return claims.ToImmutableList();
    }

    private string RandomRefreshToken()
    {
        byte[] numberByte = new byte[32];

        using var random = RandomNumberGenerator.Create();

        random.GetBytes(numberByte);

        return Convert.ToBase64String(numberByte);
    }
}