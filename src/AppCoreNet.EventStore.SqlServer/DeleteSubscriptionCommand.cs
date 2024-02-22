using System;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer;

internal sealed class DeleteSubscriptionCommand : SqlTextCommand
{
    private readonly string _schema;

    required public string SubscriptionId { get; init; }

    public DeleteSubscriptionCommand(DbContext dbContext, string? schema)
        : base(dbContext)
    {
        _schema = SchemaUtils.GetEventStoreSchema(schema);
    }

    protected override string GetCommandText()
    {
        if (SubscriptionId != "$all")
        {
            return $"DELETE FROM [{_schema}].{nameof(Model.EventSubscription)} WHERE SubscriptionId=@SubscriptionId";
        }

        return $"DELETE FROM [{_schema}].{nameof(Model.EventSubscription)}";
    }

    protected override SqlParameter[] GetCommandParameters()
    {
        if (SubscriptionId != "$all")
        {
            return
            [
                new SqlParameter("@SubscriptionId", SubscriptionId)
            ];
        }

        return Array.Empty<SqlParameter>();
    }
}