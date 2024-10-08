﻿// Licensed under the MIT license.
// Copyright (c) The AppCore .NET project.

using AppCoreNet.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppCoreNet.EventStore.SqlServer.Model;

/// <summary>
/// Provides event store related extensions for <see cref="ModelBuilder"/>.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Applies the event store model.
    /// </summary>
    /// <param name="builder">The <see cref="ModelBuilder"/>.</param>
    /// <param name="schema">The database schema name.</param>
    /// <returns>The <see cref="ModelBuilder"/> to allow method chaining.</returns>
    public static ModelBuilder ApplyEventStoreConfiguration(this ModelBuilder builder, string? schema = null)
    {
        Ensure.Arg.NotNull(builder);

        schema ??= builder.Model.GetDefaultSchema();

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

        eventEntity.HasIndex(e => new { e.EventStreamId, e.Index })
                   .IsUnique();

        eventEntity.HasOne(e => e.EventStream)
                   .WithMany()
                   .OnDelete(DeleteBehavior.Cascade);

        eventEntity.Property(e => e.EventType)
                   .IsRequired()
                   .HasMaxLength(Constants.EventTypeMaxLength);

        eventEntity.Property(e => e.Data)
                   .IsRequired();

        EntityTypeBuilder<EventSubscription> subscriptionEntity = builder.Entity<EventSubscription>();

        subscriptionEntity.ToTable(nameof(EventSubscription), schema);

        subscriptionEntity.HasKey(e => e.Id)
                          .IsClustered();

        subscriptionEntity.Property(e => e.Id)
                          .UseIdentityColumn(seed: 0);

        subscriptionEntity.Property(e => e.SubscriptionId)
                          .HasMaxLength(Constants.SubscriptionIdMaxLength)
                          .IsRequired();

        subscriptionEntity.Property(e => e.StreamId)
                          .IsRequired()
                          .HasMaxLength(Constants.StreamIdMaxLength);

        subscriptionEntity.HasIndex(e => e.SubscriptionId)
                          .IsUnique();

        subscriptionEntity.HasIndex(e => e.Position);

        subscriptionEntity.HasIndex(e => e.ProcessedAt);

        builder.Entity<WatchEventsResult>()
               .HasNoKey()
               .ToView(null);

        builder.Entity<WriteEventsResult>()
               .HasNoKey()
               .ToView(null);

        builder.Entity<WatchSubscriptionsResult>()
               .HasNoKey()
               .ToView(null);

        return builder;
    }
}