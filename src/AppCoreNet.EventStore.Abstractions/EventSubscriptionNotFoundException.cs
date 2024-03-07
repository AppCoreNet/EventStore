using AppCoreNet.Diagnostics;

namespace AppCoreNet.EventStore;

public class EventSubscriptionNotFoundException : EventStoreException
{
    public string SubscriptionId { get; }

    public EventSubscriptionNotFoundException(string subscriptionId)
        : base($"Event subscription '{subscriptionId}' not found.")
    {
        Ensure.Arg.NotEmpty(subscriptionId);
        SubscriptionId = subscriptionId;
    }
}