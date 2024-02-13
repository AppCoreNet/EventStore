using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppCoreNet.EventStore.SqlServer.Model;

public static class ModelBuilderExtensions
{
    public static ModelBuilder ApplyEventStoreConfiguration(this ModelBuilder builder, string? schema = null)
    {
        schema ??= builder.Model.GetDefaultSchema();

        if (schema != null)
        {
            builder.HasAnnotation(Constants.EventStoreSchemaAnnotation, schema);
        }

        EntityTypeBuilder<EventStream> eventStreamEntity = builder.Entity<EventStream>();

        eventStreamEntity.ToTable(nameof(EventStream), schema);

        eventStreamEntity.HasKey(e => e.Id)
                         .IsClustered();

        eventStreamEntity.Property(e => e.Id)
                         .UseIdentityColumn(seed: 0);

        eventStreamEntity.HasIndex(e => e.StreamId)
                         .IsUnique();

        eventStreamEntity.Property(e => e.StreamId)
                         .HasMaxLength(Constants.StreamIdMaxLength)
                         .IsRequired();

        eventStreamEntity.HasIndex(e => e.Sequence);

        EntityTypeBuilder<Event> eventEntity = builder.Entity<Event>();

        eventEntity.ToTable(nameof(Event), schema);

        eventEntity.HasKey(e => e.Sequence)
                   .IsClustered();

        eventEntity.Property(e => e.Sequence)
                   .UseIdentityColumn(seed: 0);

        eventEntity.HasIndex(e => e.EventStreamId);

        eventEntity.HasIndex(e => new { e.EventStreamId, e.Position })
                   .IsUnique();

        eventEntity.HasOne(e => e.EventStream)
                   .WithMany()
                   .OnDelete(DeleteBehavior.Cascade);

        eventEntity.Property(e => e.EventType)
                   .IsRequired()
                   .HasMaxLength(Constants.EventTypeMaxLength);

        eventEntity.Property(e => e.Data)
                   .IsRequired();

        builder.Entity<WatchResult>()
               .HasNoKey()
               .ToView(null);

        builder.Entity<WriteResult>()
               .HasNoKey()
               .ToView(null);

        return builder;
    }
}