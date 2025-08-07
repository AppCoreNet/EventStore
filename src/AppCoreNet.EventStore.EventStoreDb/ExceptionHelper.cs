using System;
using System.Diagnostics;

namespace AppCoreNet.EventStore.EventStoreDb;

internal static class ExceptionHelper
{
    [StackTraceHidden]
    public static Exception Rethrow(Exception error)
    {
        return new EventStoreException($"An error occured accessing the event store: {error.Message}", error);
    }
}