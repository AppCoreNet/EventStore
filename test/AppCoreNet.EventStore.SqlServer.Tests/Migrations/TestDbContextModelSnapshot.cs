﻿// <auto-generated />
using System;
using AppCoreNet.EventStore.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace AppCoreNet.EventStore.SqlServer.Migrations
{
    [DbContext(typeof(TestDbContext))]
    partial class TestDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("AppCoreNet:EventStoreSchema", "events")
                .HasAnnotation("ProductVersion", "6.0.26")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("AppCoreNet.EventStore.SqlServer.Entities.Event", b =>
                {
                    b.Property<long>("Sequence")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Sequence"), 0L, 1);

                    b.Property<DateTimeOffset?>("CreatedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Data")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("EventStreamId")
                        .HasColumnType("int");

                    b.Property<string>("EventType")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("Metadata")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("Position")
                        .HasColumnType("bigint");

                    b.HasKey("Sequence");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Sequence"));

                    b.HasIndex("EventStreamId");

                    b.HasIndex("EventStreamId", "Position")
                        .IsUnique();

                    b.ToTable("Event", "events");
                });

            modelBuilder.Entity("AppCoreNet.EventStore.SqlServer.Entities.EventStream", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 0L, 1);

                    b.Property<long>("Position")
                        .HasColumnType("bigint");

                    b.Property<long>("Sequence")
                        .HasColumnType("bigint");

                    b.Property<string>("StreamId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"));

                    b.HasIndex("Sequence");

                    b.HasIndex("StreamId")
                        .IsUnique();

                    b.ToTable("EventStream", "events");
                });

            modelBuilder.Entity("AppCoreNet.EventStore.SqlServer.Entities.WatchResult", b =>
                {
                    b.Property<long?>("Position")
                        .HasColumnType("bigint");

                    b.Property<long?>("Sequence")
                        .HasColumnType("bigint");

                    b.Property<string>("StreamId")
                        .HasColumnType("nvarchar(max)");

                    b.ToView(null);
                });

            modelBuilder.Entity("AppCoreNet.EventStore.SqlServer.Entities.WriteResult", b =>
                {
                    b.Property<long?>("Position")
                        .HasColumnType("bigint");

                    b.Property<long?>("Sequence")
                        .HasColumnType("bigint");

                    b.Property<int>("StatusCode")
                        .HasColumnType("int");

                    b.ToView(null);
                });

            modelBuilder.Entity("AppCoreNet.EventStore.SqlServer.Entities.Event", b =>
                {
                    b.HasOne("AppCoreNet.EventStore.SqlServer.Entities.EventStream", "EventStream")
                        .WithMany()
                        .HasForeignKey("EventStreamId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("EventStream");
                });
#pragma warning restore 612, 618
        }
    }
}
