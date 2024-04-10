// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AppCoreNet.EventStore.SqlServer;

internal sealed class SqlServerConfigureEventStoreOptions : IPostConfigureOptions<SqlServerEventStoreOptions>
{
    private readonly IHostEnvironment _environment;

    public SqlServerConfigureEventStoreOptions(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public void PostConfigure(string? name, SqlServerEventStoreOptions options)
    {
        if (string.IsNullOrEmpty(options.ApplicationName))
        {
            options.ApplicationName = _environment.ApplicationName;
        }
    }
}