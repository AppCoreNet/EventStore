// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using AppCoreNet.EventStore.Subscription;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer.Subscription;

internal sealed class CreateSubscriptionCommand : SqlTextCommand
{
    private readonly string _schema;

    required public SubscriptionId SubscriptionId { get; init; }

    required public StreamId StreamId { get; init; }

    public CreateSubscriptionCommand(DbContext dbContext, string? schema)
        : base(dbContext)
    {
        _schema = SchemaUtils.GetEventStoreSchema(schema);
    }

    protected override string GetCommandText()
    {
        return $"INSERT INTO [{_schema}].{nameof(Model.EventSubscription)} (SubscriptionId, StreamId, CreatedAt, Position) VALUES (@SubscriptionId, @StreamId, SYSUTCDATETIME(), 0)";
    }

    protected override SqlParameter[] GetCommandParameters()
    {
        return
        [
            new SqlParameter("@SubscriptionId", SubscriptionId.Value),
            new SqlParameter("@StreamId", StreamId.Value),
        ];
    }
}