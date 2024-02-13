using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppCoreNet.EventStore.SqlServer.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "events");

            migrationBuilder.CreateTable(
                name: "EventStream",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "0, 1"),
                    StreamId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    Position = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventStream", x => x.Id)
                        .Annotation("SqlServer:Clustered", true);
                });

            migrationBuilder.CreateTable(
                name: "Event",
                schema: "events",
                columns: table => new
                {
                    Sequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "0, 1"),
                    EventStreamId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Position = table.Column<long>(type: "bigint", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => x.Sequence)
                        .Annotation("SqlServer:Clustered", true);
                    table.ForeignKey(
                        name: "FK_Event_EventStream_EventStreamId",
                        column: x => x.EventStreamId,
                        principalSchema: "events",
                        principalTable: "EventStream",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Event_EventStreamId",
                schema: "events",
                table: "Event",
                column: "EventStreamId");

            migrationBuilder.CreateIndex(
                name: "IX_Event_EventStreamId_Position",
                schema: "events",
                table: "Event",
                columns: new[] { "EventStreamId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventStream_Sequence",
                schema: "events",
                table: "EventStream",
                column: "Sequence");

            migrationBuilder.CreateIndex(
                name: "IX_EventStream_StreamId",
                schema: "events",
                table: "EventStream",
                column: "StreamId",
                unique: true);

            migrationBuilder.CreateEventStoreProcedures(schema: "events");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropEventStoreProcedures(schema: "events");

            migrationBuilder.DropTable(
                name: "Event",
                schema: "events");

            migrationBuilder.DropTable(
                name: "EventStream",
                schema: "events");
        }
    }
}
