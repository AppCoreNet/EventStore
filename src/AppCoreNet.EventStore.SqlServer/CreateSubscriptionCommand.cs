using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer;

internal sealed class CreateSubscriptionCommand : SqlTextCommand
{
    private readonly string _schema;

    required public string SubscriptionId { get; init; }

    required public string StreamId { get; init; }

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
            new SqlParameter("@SubscriptionId", SubscriptionId),
            new SqlParameter("@StreamId", StreamId),
        ];
    }
}