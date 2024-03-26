// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using AppCoreNet.EventStore.Subscriptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer.Subscriptions;

internal sealed class CreateSubscriptionCommand : SqlTextCommand
{
    private readonly string _schema;

    required public SubscriptionId SubscriptionId { get; init; }

    required public StreamId StreamId { get; init; }

    public bool FailIfExists { get; init; }

    public CreateSubscriptionCommand(DbContext dbContext, string? schema)
        : base(dbContext)
    {
        _schema = SchemaUtils.GetEventStoreSchema(schema);
    }

    protected override string GetCommandText()
    {
        string tableName = $"[{_schema}].[{nameof(Model.EventSubscription)}]";

        if (FailIfExists)
        {
            return $"""
                    INSERT INTO {tableName}
                        ([{nameof(Model.EventSubscription.SubscriptionId)}],
                         [{nameof(Model.EventSubscription.StreamId)}],
                         [{nameof(Model.EventSubscription.CreatedAt)}],
                         [{nameof(Model.EventSubscription.Position)}])
                    VALUES
                        (@{nameof(SubscriptionId)},
                         @{nameof(StreamId)},
                         SYSUTCDATETIME(),
                         {StreamPosition.Start.Value})
                    """;
        }

        return $"""
                INSERT INTO {tableName}
                    ([{nameof(Model.EventSubscription.SubscriptionId)}],
                     [{nameof(Model.EventSubscription.StreamId)}],
                     [{nameof(Model.EventSubscription.CreatedAt)}],
                     [{nameof(Model.EventSubscription.Position)}])
                SELECT
                    @{nameof(SubscriptionId)},
                    @{nameof(StreamId)},
                    SYSUTCDATETIME(),
                    {StreamPosition.Start.Value}
                WHERE NOT EXISTS
                    (SELECT 1 FROM {tableName} WHERE [{nameof(Model.EventSubscription.SubscriptionId)}] = @{nameof(SubscriptionId)})
                """;
    }

    protected override SqlParameter[] GetCommandParameters()
    {
        return
        [
            new SqlParameter($"@{nameof(SubscriptionId)}", SubscriptionId.Value),
            new SqlParameter($"@{nameof(StreamId)}", StreamId.Value),
        ];
    }
}