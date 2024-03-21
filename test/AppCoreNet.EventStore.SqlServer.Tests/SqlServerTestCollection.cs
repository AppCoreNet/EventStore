// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using Xunit;

namespace AppCoreNet.EventStore.SqlServer;

[CollectionDefinition(Name, DisableParallelization = true)]
public class SqlServerTestCollection : ICollectionFixture<SqlServerTestFixture>
{
    public const string Name = "SqlServer";
}