using Xunit;

namespace AppCoreNet.EventStore.SqlServer;

[CollectionDefinition(Name, DisableParallelization = true)]
public class SqlServerTestCollection : ICollectionFixture<SqlServerTestFixture>
{
    public const string Name = "SqlServer";
}