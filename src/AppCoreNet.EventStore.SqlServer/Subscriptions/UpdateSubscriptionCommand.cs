// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using AppCoreNet.EventStore.Subscriptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer.Subscriptions;

internal sealed class UpdateSubscriptionCommand : SqlTextCommand
{
    private readonly string _schema;

    public required SubscriptionId SubscriptionId { get; init; }

    public required long Position { get; init; }

    public UpdateSubscriptionCommand(DbContext dbContext, string? schema)
        : base(dbContext)
    {
        _schema = SchemaUtils.GetEventStoreSchema(schema);
    }

    protected override string GetCommandText()
    {
        return $"UPDATE [{_schema}].[{nameof(Model.EventSubscription)}] SET"
               + $" [{nameof(Model.EventSubscription.ProcessedAt)}] = SYSUTCDATETIME(),"
               + $" [{nameof(Model.EventSubscription.Position)}] = @{nameof(Position)}"
               + $" WHERE [{nameof(Model.EventSubscription.SubscriptionId)}] = @{nameof(SubscriptionId)}";
    }

    protected override SqlParameter[] GetCommandParameters()
    {
        return
        [
            new SqlParameter($"@{nameof(SubscriptionId)}", SubscriptionId.Value),
            new SqlParameter($"@{nameof(Position)}", Position),
        ];
    }
}