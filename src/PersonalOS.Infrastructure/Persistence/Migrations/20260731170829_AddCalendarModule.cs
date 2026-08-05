using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalOS.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Turns the flat planning table into the calendar aggregate: items gain a kind and a
    /// recurrence rule, and the per-day decision moves into its own table.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The scaffolded version of this migration renamed <c>Status</c> into
    /// <c>RecurrenceSelectedWeekdaysMask</c> and dropped <c>CompletedAtUtc</c>, because those are
    /// the cheapest column operations that produce the new shape. That would have reinterpreted
    /// every stored status as a weekday bitmask and thrown away every completion instant, so the
    /// data movement below is written by hand instead.
    /// </para>
    /// <para>
    /// Three things therefore have to happen in order: the new table and columns are created, the
    /// existing statuses and completion instants are copied into the new table, and only then are
    /// the old columns dropped.
    /// </para>
    /// </remarks>
    public partial class AddCalendarModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.RenameColumn(
                name: "ScheduledLocalDate",
                table: "PlanningItems",
                newName: "StartDate");

            migrationBuilder.RenameIndex(
                name: "IX_PlanningItems_UserId_ScheduledLocalDate",
                table: "PlanningItems",
                newName: "IX_PlanningItems_UserId_StartDate");

            // Everything that already exists was a one-off task, so the defaults below are not
            // arbitrary: they are exactly what such a row means under the new model.
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "PlanningItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceFrequency",
                table: "PlanningItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceInterval",
                table: "PlanningItems",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RecurrenceEndDate",
                table: "PlanningItems",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceSelectedWeekdaysMask",
                table: "PlanningItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PlanningItemOccurrenceStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanningItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurrenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningItemOccurrenceStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanningItemOccurrenceStates_PlanningItems_PlanningItemId",
                        column: x => x.PlanningItemId,
                        principalTable: "PlanningItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanningItemOccurrenceStates_PlanningItemId_OccurrenceDate",
                table: "PlanningItemOccurrenceStates",
                columns: new[] { "PlanningItemId", "OccurrenceDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanningItemOccurrenceStates_UserId_OccurrenceDate",
                table: "PlanningItemOccurrenceStates",
                columns: new[] { "UserId", "OccurrenceDate" });

            // Every item that was completed or cancelled becomes one occurrence state on the day it
            // was scheduled for. Items that were merely planned get no row at all: under the new
            // model the absence of a row is what "planned" means, so writing one would be noise.
            // The two enumerations happen to agree on their numeric values (planned 0, completed 1,
            // cancelled 2), which is why the status copies across directly.
            migrationBuilder.Sql(
                """
                INSERT INTO [PlanningItemOccurrenceStates]
                    ([Id], [UserId], [PlanningItemId], [OccurrenceDate], [Status],
                     [CreatedAtUtc], [UpdatedAtUtc], [CompletedAtUtc])
                SELECT
                    NEWID(),
                    [items].[UserId],
                    [items].[Id],
                    [items].[StartDate],
                    [items].[Status],
                    [items].[UpdatedAtUtc],
                    [items].[UpdatedAtUtc],
                    [items].[CompletedAtUtc]
                FROM [PlanningItems] AS [items]
                WHERE [items].[Status] <> 0;
                """);

            // The category list gained Fitness and Nutrition and put the areas of life in a
            // different order, so the stored numbers no longer mean what they used to. CASE reads
            // the original value for every branch, so the remap cannot cascade through itself.
            //   1 Work     -> 2 Work
            //   2 Study    -> 3 Study
            //   3 Training -> 5 Fitness
            //   5 Personal -> 1 Personal
            // General (0) and Health (4) keep their numbers.
            migrationBuilder.Sql(
                """
                UPDATE [PlanningItems]
                SET [Category] = CASE [Category]
                    WHEN 1 THEN 2
                    WHEN 2 THEN 3
                    WHEN 3 THEN 5
                    WHEN 5 THEN 1
                    ELSE [Category]
                END;
                """);

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PlanningItems");

            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "PlanningItems");
        }

        /// <inheritdoc />
        /// <remarks>
        /// The reverse is best effort. A one-off item recovers its status exactly, because its only
        /// occurrence was the day it was scheduled for. A repeating item cannot: the old shape had
        /// one status per row and no way to express a decision about a particular day, so the
        /// decisions about every other day are lost. That is inherent in going back to a model that
        /// cannot represent them, not something this script chooses to discard.
        /// </remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "PlanningItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAtUtc",
                table: "PlanningItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [items]
                SET [items].[Status] = [states].[Status],
                    [items].[CompletedAtUtc] = [states].[CompletedAtUtc]
                FROM [PlanningItems] AS [items]
                INNER JOIN [PlanningItemOccurrenceStates] AS [states]
                    ON [states].[PlanningItemId] = [items].[Id]
                    AND [states].[OccurrenceDate] = [items].[StartDate];
                """);

            // Fitness and Nutrition have no equivalent in the old list, so both fall back to
            // General rather than being mapped onto an area of life the user never chose.
            migrationBuilder.Sql(
                """
                UPDATE [PlanningItems]
                SET [Category] = CASE [Category]
                    WHEN 1 THEN 5
                    WHEN 2 THEN 1
                    WHEN 3 THEN 2
                    WHEN 5 THEN 3
                    WHEN 6 THEN 0
                    ELSE [Category]
                END;
                """);

            migrationBuilder.DropTable(
                name: "PlanningItemOccurrenceStates");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "PlanningItems");

            migrationBuilder.DropColumn(
                name: "RecurrenceFrequency",
                table: "PlanningItems");

            migrationBuilder.DropColumn(
                name: "RecurrenceInterval",
                table: "PlanningItems");

            migrationBuilder.DropColumn(
                name: "RecurrenceEndDate",
                table: "PlanningItems");

            migrationBuilder.DropColumn(
                name: "RecurrenceSelectedWeekdaysMask",
                table: "PlanningItems");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "PlanningItems",
                newName: "ScheduledLocalDate");

            migrationBuilder.RenameIndex(
                name: "IX_PlanningItems_UserId_StartDate",
                table: "PlanningItems",
                newName: "IX_PlanningItems_UserId_ScheduledLocalDate");
        }
    }
}
