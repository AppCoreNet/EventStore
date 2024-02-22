using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer;

internal sealed class UpdateSubscriptionCommand : SqlTextCommand
{
    private readonly string _schema;

    required public int Id { get; init; }

    required public long Position { get; init; }

    public UpdateSubscriptionCommand(DbContext dbContext, string? schema)
        : base(dbContext)
    {
        _schema = SchemaUtils.GetEventStoreSchema(schema);
    }

    protected override string GetCommandText()
    {
        return $"UPDATE [{_schema}].{nameof(Model.EventSubscription)}  SET ProcessedAt=SYSUTCDATETIME(), Position=@Position WHERE Id=@Id";
    }

    protected override SqlParameter[] GetCommandParameters()
    {
        return
        [
            new SqlParameter("@Id", Id),
            new SqlParameter("@Position", Position),
        ];
    }
}