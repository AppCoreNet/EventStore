namespace AppCoreNet.EventStore
{
    /// <summary>
    /// An event which is emitted before the transaction processing the event queue is committed.
    /// This allows listeners to ensure any buffered work is completed before the transaction is committed.
    /// </summary>
    [EventType(nameof(EventTransactionCommittingEvent))]
    public class EventTransactionCommittingEvent
    {
    }
}
