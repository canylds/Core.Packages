﻿using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;

namespace Core.Application.Pipelines.Caching;

public class CacheRemovingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICacheRemoverRequest
{
    private readonly IDistributedCache _cache;

    public CacheRemovingBehavior(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request.BypassCache)
            return await next();

        TResponse response = await next();

        if (request.CacheGroupKeys != null)
            for (int i = 0; i < request.CacheGroupKeys.Length; i++)
            {
                byte[]? cachedGroup = await _cache.GetAsync(request.CacheGroupKeys[i], cancellationToken);
                if (cachedGroup != null)
                {
                    HashSet<string> keysInGroup = JsonSerializer.Deserialize<HashSet<string>>(Encoding.Default.GetString(cachedGroup))!;

                    foreach (string key in keysInGroup)
                        await _cache.RemoveAsync(key, cancellationToken);

                    await _cache.RemoveAsync(request.CacheGroupKeys[i], cancellationToken);
                    await _cache.RemoveAsync(key: $"{request.CacheGroupKeys}SlidingExpiration", cancellationToken);
                }
            }


        if (request.CacheKey != null)
            await _cache.RemoveAsync(request.CacheKey, cancellationToken);

        return response;
    }
}