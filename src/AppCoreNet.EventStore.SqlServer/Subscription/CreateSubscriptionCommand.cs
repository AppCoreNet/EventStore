﻿// Licensed under the MIT license.
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

    public bool FailIfExists { get; init; }

    public CreateSubscriptionCommand(DbContext dbContext, string? schema)
        : base(dbContext)
    {
        _schema = SchemaUtils.GetEventStoreSchema(schema);
    }

    protected override string GetCommandText()
    {
        string tableName = $"[{_schema}].{nameof(Model.EventSubscription)}";

        return FailIfExists
            ? $"INSERT INTO {tableName} (SubscriptionId, StreamId, CreatedAt, Position) VALUES (@SubscriptionId, @StreamId, SYSUTCDATETIME(), {StreamPosition.Start.Value})"
            : $"INSERT INTO {tableName} (SubscriptionId, StreamId, CreatedAt, Position) SELECT @SubscriptionId, @StreamId, SYSUTCDATETIME(), {StreamPosition.Start.Value} WHERE NOT EXISTS (SELECT 1 FROM {tableName} WHERE SubscriptionId=@SubscriptionId)";
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