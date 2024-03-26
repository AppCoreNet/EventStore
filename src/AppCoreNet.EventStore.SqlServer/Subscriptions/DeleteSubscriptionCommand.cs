// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using System;
using AppCoreNet.EventStore.Subscriptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer.Subscriptions;

internal sealed class DeleteSubscriptionCommand : SqlTextCommand
{
    private readonly string _schema;

    required public SubscriptionId SubscriptionId { get; init; }

    public DeleteSubscriptionCommand(DbContext dbContext, string? schema)
        : base(dbContext)
    {
        _schema = SchemaUtils.GetEventStoreSchema(schema);
    }

    protected override string GetCommandText()
    {
        string tableName = $"[{_schema}].[{nameof(Model.EventSubscription)}]";

        if (SubscriptionId == SubscriptionId.All)
        {
            return $"DELETE FROM {tableName}";
        }

        if (SubscriptionId.IsPrefix)
        {
            return $"DELETE FROM {tableName}"
                   + $" WHERE [{nameof(Model.EventSubscription.SubscriptionId)}] LIKE @{nameof(SubscriptionId)}";
        }

        return $"DELETE FROM {tableName}"
               + $" WHERE  [{nameof(Model.EventSubscription.SubscriptionId)}] = @{nameof(SubscriptionId)}";
    }

    protected override SqlParameter[] GetCommandParameters()
    {
        if (SubscriptionId == SubscriptionId.All)
            return Array.Empty<SqlParameter>();

        return
        [
            new SqlParameter($"@{nameof(SubscriptionId)}", SubscriptionId.Value.Replace('*', '%'))
        ];
    }
}