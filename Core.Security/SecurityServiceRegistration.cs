using Core.Security.EmailAuthenticator;
using Core.Security.Jwt;
using Core.Security.OtpAuthenticator.OtpNet;
using Core.Security.OtpAuthenticator;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Security;

public static class SecurityServiceRegistration
{
    public static IServiceCollection AddSecurityServices<TUserId, TOperationClaimId, TRefreshTokenId>(this IServiceCollection services)
    {
        services.AddScoped<ITokenHelper<TUserId, TOperationClaimId, TRefreshTokenId>,
            JwtHelper<TUserId, TOperationClaimId, TRefreshTokenId>>();
        services.AddScoped<IEmailAuthenticatorHelper, EmailAuthenticatorHelper>();
        services.AddScoped<IOtpAuthenticatorHelper, OtpNetOtpAuthenticatorHelper>();

        return services;
    }
}
