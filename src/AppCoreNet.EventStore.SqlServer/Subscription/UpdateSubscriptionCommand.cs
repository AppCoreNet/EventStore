// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using AppCoreNet.EventStore.Subscription;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer.Subscription;

internal sealed class UpdateSubscriptionCommand : SqlTextCommand
{
    private readonly string _schema;

    required public SubscriptionId SubscriptionId { get; init; }

    required public long Position { get; init; }

    public UpdateSubscriptionCommand(DbContext dbContext, string? schema)
        : base(dbContext)
    {
        _schema = SchemaUtils.GetEventStoreSchema(schema);
    }

    protected override string GetCommandText()
    {
        return $"UPDATE [{_schema}].{nameof(Model.EventSubscription)}  SET ProcessedAt=SYSUTCDATETIME(), Position=@Position WHERE SubscriptionId=@SubscriptionId";
    }

    protected override SqlParameter[] GetCommandParameters()
    {
        return
        [
            new SqlParameter("@SubscriptionId", SubscriptionId.Value),
            new SqlParameter("@Position", Position),
        ];
    }
}