using Microsoft.AspNetCore.Builder;

namespace Core.CrossCuttingConcerns.Exception.Extensions;

public static class ExceptionMiddlewareExtensions
{
    public static void ConfigureCustomExceptionMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionMiddleware>();
}
