using Core.Persistence.Dynamic;
using Core.Persistence.Paging;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;

namespace Core.Persistence.Repositories;

public class EfRepositoryBase<TEntity, TEntityId, TContext> : IRepository<TEntity, TEntityId>, IAsyncRepository<TEntity, TEntityId>
    where TEntity : Entity<TEntityId>
    where TContext : DbContext
{
    protected readonly TContext _context;

    public EfRepositoryBase(TContext context)
    {
        _context = context;
    }

    public IQueryable<TEntity> Query() => _context.Set<TEntity>();

    public bool Any(Expression<Func<TEntity, bool>>? predicate = null,
        bool withDeleted = false,
        bool enableTracking = true)
    {
        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);
        return queryable.Any();
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);
        return await queryable.AnyAsync(cancellationToken);
    }

    public TEntity Add(TEntity entity)
    {
        entity.CreatedDate = DateTime.UtcNow;

        _context.Add(entity);
        _context.SaveChanges();

        return entity;
    }

    public async Task<TEntity> AddAsync(TEntity entity)
    {
        entity.CreatedDate = DateTime.UtcNow;

        await _context.AddAsync(entity);
        await _context.SaveChangesAsync();

        return entity;
    }

    public ICollection<TEntity> AddRange(ICollection<TEntity> entities)
    {
        foreach (TEntity entity in entities)
            entity.CreatedDate = DateTime.UtcNow;

        _context.AddRange(entities);
        _context.SaveChanges();

        return entities;
    }

    public async Task<ICollection<TEntity>> AddRangeAsync(ICollection<TEntity> entities)
    {
        foreach (TEntity entity in entities)
            entity.CreatedDate = DateTime.UtcNow;

        await _context.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        return entities;
    }

    public TEntity Update(TEntity entity)
    {
        entity.UpdatedDate = DateTime.UtcNow;

        _context.Update(entity);
        _context.SaveChanges();

        return entity;
    }

    public async Task<TEntity> UpdateAsync(TEntity entity)
    {
        entity.UpdatedDate = DateTime.UtcNow;

        _context.Update(entity);
        await _context.SaveChangesAsync();

        return entity;
    }

    public ICollection<TEntity> UpdateRange(ICollection<TEntity> entities)
    {
        foreach (TEntity entity in entities)
            entity.UpdatedDate = DateTime.UtcNow;

        _context.UpdateRange(entities);
        _context.SaveChanges();

        return entities;
    }

    public async Task<ICollection<TEntity>> UpdateRangeAsync(ICollection<TEntity> entities)
    {
        foreach (TEntity entity in entities)
            entity.UpdatedDate = DateTime.UtcNow;

        _context.UpdateRange(entities);
        await _context.SaveChangesAsync();

        return entities;
    }

    public TEntity Delete(TEntity entity, bool permanent = false)
    {
        SetEntityAsDeleted(entity, permanent);
        _context.SaveChanges();

        return entity;
    }

    public async Task<TEntity> DeleteAsync(TEntity entity, bool permanent = false)
    {
        await SetEntityAsDeletedAsync(entity, permanent);
        await _context.SaveChangesAsync();

        return entity;
    }

    public ICollection<TEntity> DeleteRange(ICollection<TEntity> entities, bool permanent = false)
    {
        SetEntityAsDeleted(entities, permanent);
        _context.SaveChanges();

        return entities;
    }

    public async Task<ICollection<TEntity>> DeleteRangeAsync(ICollection<TEntity> entities, bool permanent = false)
    {
        await SetEntityAsDeletedAsync(entities, permanent);
        await _context.SaveChangesAsync();

        return entities;
    }

    protected void SetEntityAsDeleted(TEntity entity, bool permanent)
    {
        if (!permanent)
        {
            CheckHasEntityHaveOneToOneRelation(entity);
            SetEntityAsSoftDeleted(entity);
        }
        else
            _context.Remove(entity);
    }

    protected async Task SetEntityAsDeletedAsync(TEntity entity, bool permanent)
    {
        if (!permanent)
        {
            CheckHasEntityHaveOneToOneRelation(entity);
            await SetEntityAsSoftDeletedAsync(entity);
        }
        else
            _context.Remove(entity);
    }

    protected void SetEntityAsDeleted(IEnumerable<TEntity> entities, bool permanent)
    {
        foreach (TEntity entity in entities)
            SetEntityAsDeleted(entity, permanent);
    }

    protected async Task SetEntityAsDeletedAsync(IEnumerable<TEntity> entities, bool permanent)
    {
        foreach (TEntity entity in entities)
            await SetEntityAsDeletedAsync(entity, permanent);
    }

    private void SetEntityAsSoftDeleted(IEntityTimestamps entity)
    {
        if (entity.DeletedDate.HasValue)
            return;

        entity.DeletedDate = DateTime.UtcNow;

        var navigations = _context
            .Entry(entity)
            .Metadata
            .GetNavigations()
            .Where(x => x is { IsOnDependent: false, ForeignKey.DeleteBehavior: DeleteBehavior.ClientCascade or DeleteBehavior.Cascade })
            .ToList();

        foreach (INavigation? navigation in navigations)
        {
            if (navigation.TargetEntityType.IsOwned())
                continue;
            if (navigation.PropertyInfo == null)
                continue;

            object? navValue = navigation.PropertyInfo.GetValue(entity);
            if (navigation.IsCollection)
            {
                if (navValue == null || !((IEnumerable<object>)navValue).Any())
                {
                    IQueryable query = _context.Entry(entity).Collection(navigation.PropertyInfo.Name).Query();
                    navValue = GetRelationLoaderQuery(query, navigationPropertyType: navigation.PropertyInfo.GetType())
                        .ToList();

                    if (navValue == null || !((IEnumerable<object>)navValue).Any())
                        continue;
                }

                foreach (IEntityTimestamps navValueItem in (IEnumerable)navValue)
                    SetEntityAsSoftDeleted(navValueItem);
            }
            else
            {
                if (navValue == null || !((IEnumerable<object>)navValue).Any())
                {
                    IQueryable query = _context.Entry(entity).Reference(navigation.PropertyInfo.Name).Query();
                    navValue = GetRelationLoaderQuery(query, navigationPropertyType: navigation.PropertyInfo.GetType())
                        .FirstOrDefault();

                    if (navValue == null || !((IEnumerable<object>)navValue).Any())
                        continue;
                }

                SetEntityAsSoftDeleted((IEntityTimestamps)navValue);
            }
        }

        _context.Update(entity);
    }

    private async Task SetEntityAsSoftDeletedAsync(IEntityTimestamps entity)
    {
        if (entity.DeletedDate.HasValue)
            return;

        entity.DeletedDate = DateTime.UtcNow;

        var navigations = _context
            .Entry(entity)
            .Metadata.GetNavigations()
            .Where(x => x is { IsOnDependent: false, ForeignKey.DeleteBehavior: DeleteBehavior.ClientCascade or DeleteBehavior.Cascade })
            .ToList();

        foreach (INavigation? navigation in navigations)
        {
            if (navigation.TargetEntityType.IsOwned())
                continue;
            if (navigation.PropertyInfo == null)
                continue;

            object? navValue = navigation.PropertyInfo.GetValue(entity);
            if (navigation.IsCollection)
            {
                if (navValue == null || !((IEnumerable<object>)navValue).Any())
                {
                    IQueryable query = _context.Entry(entity).Collection(navigation.PropertyInfo.Name).Query();
                    navValue = await GetRelationLoaderQuery(query, navigationPropertyType: navigation.PropertyInfo.GetType())
                        .ToListAsync();
                    if (navValue == null || !((IEnumerable<object>)navValue).Any())
                        continue;
                }

                foreach (IEntityTimestamps navValueItem in (IEnumerable)navValue)
                    await SetEntityAsSoftDeletedAsync(navValueItem);
            }
            else
            {
                if (navValue == null || !((IEnumerable<object>)navValue).Any())
                {
                    IQueryable query = _context.Entry(entity).Reference(navigation.PropertyInfo.Name).Query();
                    navValue = await GetRelationLoaderQuery(query, navigationPropertyType: navigation.PropertyInfo.GetType())
                        .FirstOrDefaultAsync();
                    if (navValue == null || !((IEnumerable<object>)navValue).Any())
                        continue;
                }

                await SetEntityAsSoftDeletedAsync((IEntityTimestamps)navValue);
            }
        }

        _context.Update(entity);
    }

    protected void CheckHasEntityHaveOneToOneRelation(TEntity entity)
    {
        bool hasEntityHaveOneToOneRelation =
            _context.Entry(entity).Metadata.GetForeignKeys().All(x =>
            x.DependentToPrincipal?.IsCollection == true ||
            x.PrincipalToDependent?.IsCollection == true ||
            x.DependentToPrincipal?.ForeignKey.DeclaringEntityType.ClrType == entity.GetType()) == false;

        if (hasEntityHaveOneToOneRelation)
            throw new InvalidOperationException("Entity has one-to-one relationship. Soft Delete causes problems if you try to create entry again by same foreign key.");
    }

    protected IQueryable<object> GetRelationLoaderQuery(IQueryable query, Type navigationPropertyType)
    {
        Type queryProviderType = query.Provider.GetType();

        MethodInfo createQueryMethod = queryProviderType.GetMethods().First(m =>
        m is { Name: nameof(query.Provider.CreateQuery), IsGenericMethod: true })?.MakeGenericMethod(navigationPropertyType) ??

        throw new InvalidOperationException("CreateQuery<TElement> method is not found in IQueryProvider.");

        var queryProviderQuery = (IQueryable<object>)createQueryMethod.Invoke(query.Provider, parameters: new object[]
        {
            query.Expression
        })!;

        return queryProviderQuery.Where(x => !((IEntityTimestamps)x).DeletedDate.HasValue);
    }

    public TEntity? Get(Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true)
    {
        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)
            queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        return queryable.FirstOrDefault(predicate);
    }

    public async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        return await queryable.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public Paginate<TEntity> GetList(Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true)
    {
        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);
        if (orderBy != null)
            return orderBy(queryable).ToPaginate(index, size);
        return queryable.ToPaginate(index, size);
    }

    public async Task<Paginate<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);
        if (orderBy != null)
            return await orderBy(queryable).ToPaginateAsync(index, size, cancellationToken);
        return await queryable.ToPaginateAsync(index, size, cancellationToken);
    }

    public Paginate<TEntity> GetListByDynamic(DynamicQuery dynamic,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true)
    {
        IQueryable<TEntity> queryable = Query().ToDynamic(dynamic);
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);
        return queryable.ToPaginate(index, size);
    }

    public async Task<Paginate<TEntity>> GetListByDynamicAsync(DynamicQuery dynamic,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> queryable = Query().ToDynamic(dynamic);
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);
        return await queryable.ToPaginateAsync(index, size, cancellationToken);
    }
}