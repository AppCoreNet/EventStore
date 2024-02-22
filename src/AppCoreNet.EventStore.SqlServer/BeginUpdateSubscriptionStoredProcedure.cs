using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppCoreNet.EventStore.SqlServer;

internal sealed class BeginUpdateSubscriptionStoredProcedure : SqlStoredProcedure<Model.BeginUpdateSubscriptionResult>
{
    private const string ProcedureName = "BeginUpdateSubscription";

    required public string SubscriptionId { get; init; }

    public BeginUpdateSubscriptionStoredProcedure(DbContext dbContext, string? schema)
        : base(dbContext, $"[{SchemaUtils.GetEventStoreSchema(schema)}].{ProcedureName}")
    {
    }

    public static string GetCreateScript(string? schema)
    {
        schema ??= SchemaUtils.GetEventStoreSchema(schema);

        return $"""
                CREATE PROCEDURE [{schema}].{ProcedureName} (
                    @SubscriptionId NVARCHAR({Constants.SubscriptionIdMaxLength})
                    )
                AS
                BEGIN
                    DECLARE @Id AS INT;
                    DECLARE @StreamId AS NVARCHAR({Constants.StreamIdMaxLength});
                    DECLARE @Position AS BIGINT;

                    SELECT TOP 1 @Id = SU.Id, @Position = SU.Position, @StreamId = SU.StreamId
                            FROM [events].EventSubscription AS SU WITH (UPDLOCK, ROWLOCK, READPAST)
                            WHERE SU.SubscriptionId = @SubscriptionId;

                    SELECT @Id AS Id, @SubscriptionId AS SubscriptionId, @Position AS Position, @StreamId AS StreamId;
                END
                """;
    }

    public static string GetDropScript(string? schema)
    {
        return $"DROP PROCEDURE [{schema}].{ProcedureName}";
    }

    protected override SqlParameter[] GetParameters()
    {
        return
        [
            new SqlParameter("@SubscriptionId", SubscriptionId),
        ];
    }
}