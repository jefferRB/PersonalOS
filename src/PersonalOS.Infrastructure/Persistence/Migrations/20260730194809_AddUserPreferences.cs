using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Accounts created before Milestone 2 receive the neutral UTC default so that every
            // existing user has a valid preferences record immediately after the upgrade.
            // A regional default is deliberately not hard-coded here.
            migrationBuilder.Sql(
                """
                INSERT INTO [UserPreferences] ([UserId], [TimeZoneId], [CreatedAtUtc], [UpdatedAtUtc])
                SELECT
                    [users].[Id],
                    N'UTC',
                    CAST(SYSUTCDATETIME() AS datetimeoffset),
                    CAST(SYSUTCDATETIME() AS datetimeoffset)
                FROM [AspNetUsers] AS [users]
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [UserPreferences] AS [preferences]
                    WHERE [preferences].[UserId] = [users].[Id]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPreferences");
        }
    }
}
