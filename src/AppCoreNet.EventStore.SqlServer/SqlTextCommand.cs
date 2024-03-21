// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer;

internal abstract class SqlTextCommand : SqlCommand<int>
{
    protected SqlTextCommand(DbContext dbContext)
        : base(dbContext)
    {
    }

    protected abstract string GetCommandText();

    protected abstract SqlParameter[] GetCommandParameters();

    protected override async Task<int> ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        return await DbContext.Database.ExecuteSqlRawAsync(GetCommandText(), GetCommandParameters(), cancellationToken)
                              .ConfigureAwait(false);
    }
}