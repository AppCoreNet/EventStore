// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer;

internal abstract class SqlStoredProcedure<T> : SqlCommand<T>
    where T : class
{
    private readonly string _procedureName;

    protected SqlStoredProcedure(DbContext dbContext, string procedureName)
        : base(dbContext)
    {
        _procedureName = procedureName;
    }

    protected abstract SqlParameter[] GetParameters();

    [SuppressMessage(
        "ReSharper",
        "CoVariantArrayConversion",
        Justification = "Parameters are only read.")]
    protected override async Task<T> ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        SqlParameter[] parameters = GetParameters();
        string parametersNames = string.Join(", ", parameters.Select(p => p.ParameterName));
        string sql = $"EXEC {_procedureName} {parametersNames}";

        return await DbContext.Set<T>()
                              .FromSqlRaw(sql, parameters)
                              .AsNoTracking()
                              .AsAsyncEnumerable()
                              .FirstAsync(cancellationToken)
                              .ConfigureAwait(false);
    }
}