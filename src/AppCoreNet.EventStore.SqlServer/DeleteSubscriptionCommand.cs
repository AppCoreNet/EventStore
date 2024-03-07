using System;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer;

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
        if (SubscriptionId == SubscriptionId.All)
            return $"DELETE FROM [{_schema}].{nameof(Model.EventSubscription)}";

        if (SubscriptionId.IsPrefix)
            return $"DELETE FROM [{_schema}].{nameof(Model.EventSubscription)} WHERE SubscriptionId LIKE @SubscriptionId + '%'";

        if (SubscriptionId.IsSuffix)
            return $"DELETE FROM [{_schema}].{nameof(Model.EventSubscription)} WHERE SubscriptionId LIKE '%' + @SubscriptionId";

        return $"DELETE FROM [{_schema}].{nameof(Model.EventSubscription)} WHERE SubscriptionId=@SubscriptionId";
    }

    protected override SqlParameter[] GetCommandParameters()
    {
        if (SubscriptionId == SubscriptionId.All)
            return Array.Empty<SqlParameter>();

        return
        [
            new SqlParameter("@SubscriptionId", SubscriptionId.Value.Trim('*'))
        ];
    }
}