// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Snap.Hutao.Service.Abstraction;

internal static class RepositoryCollectionExtension
{
    public static ImmutableArray<TEntity> ImmutableArray<TEntity>(this IRepository<TEntity> repository)
        where TEntity : class
    {
        return repository.Query(query => query.ToImmutableArray());
    }

    public static ImmutableArray<TEntity> ImmutableArray<TEntity>(this IRepository<TEntity> repository, Expression<Func<TEntity, bool>> predicate)
        where TEntity : class
    {
        return repository.ImmutableArray(query => query.Where(predicate));
    }

    public static ImmutableArray<TResult> ImmutableArray<TEntity, TResult>(this IRepository<TEntity> repository, Func<IQueryable<TEntity>, IQueryable<TResult>> query)
        where TEntity : class
    {
        return repository.Query(query1 => query(query1).ToImmutableArray());
    }

    // ObservableCollection<TEntity> is always not readonly.
    public static ObservableCollection<TEntity> ObservableCollection<TEntity>(this IRepository<TEntity> repository)
        where TEntity : class
    {
        return repository.Query(query => query.ToObservableCollection());
    }

    public static ObservableCollection<TEntity> ObservableCollection<TEntity>(this IRepository<TEntity> repository, Expression<Func<TEntity, bool>> predicate)
        where TEntity : class
    {
        return repository.Query(query => query.Where(predicate).ToObservableCollection());
    }
}