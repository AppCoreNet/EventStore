// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using Xunit;

namespace AppCoreNet.EventStore.EventStoreDb;

[CollectionDefinition(Name, DisableParallelization = true)]
public class EventStoreDbTestCollection : ICollectionFixture<EventStoreDbTestFixture>
{
    public const string Name = "EventStoreDb";
}