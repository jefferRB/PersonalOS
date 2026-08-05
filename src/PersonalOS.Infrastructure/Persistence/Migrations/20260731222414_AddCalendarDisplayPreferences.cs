using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalOS.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds the day planner's visible-hours window and slot length to the account's preferences.
    /// </summary>
    /// <remarks>
    /// The scaffolded version defaulted every column to its CLR zero: midnight to midnight, with a
    /// slot length of zero minutes. Applied to an existing account that would produce a window with
    /// no hours in it and a grid with no rows, so the defaults below are the ones the domain
    /// actually treats as "never chosen".
    /// </remarks>
    public partial class AddCalendarDisplayPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "CalendarDayStartTime",
                table: "UserPreferences",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(6, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "CalendarDayEndTime",
                table: "UserPreferences",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(22, 0));

            migrationBuilder.AddColumn<int>(
                name: "CalendarSlotMinutes",
                table: "UserPreferences",
                type: "int",
                nullable: false,
                defaultValue: 15);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropColumn(
                name: "CalendarDayEndTime",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "CalendarDayStartTime",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "CalendarSlotMinutes",
                table: "UserPreferences");
        }
    }
}
