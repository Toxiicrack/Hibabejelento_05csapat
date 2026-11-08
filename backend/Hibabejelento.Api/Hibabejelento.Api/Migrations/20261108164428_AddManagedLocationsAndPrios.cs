using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hibabejelento.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddManagedLocationsAndPrios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(
                        type: "TEXT",
                        maxLength: 300,
                        nullable: false),
                    Description = table.Column<string>(
                        type: "TEXT",
                        maxLength: 500,
                        nullable: true),
                    IsActive = table.Column<bool>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: true),
                    CreatedAt = table.Column<DateTime>(
                        type: "TEXT",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Priorities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(
                        type: "TEXT",
                        maxLength: 20,
                        nullable: false),
                    DefaultDueDays = table.Column<int>(
                        type: "INTEGER",
                        nullable: false),
                    IsDefault = table.Column<bool>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: false),
                    IsActive = table.Column<bool>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: true),
                    CreatedAt = table.Column<DateTime>(
                        type: "TEXT",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Priorities", x => x.Id);
                });

            // A régi ticketek helyszíneiből létrehozza az új helyszínrekordokat.
            migrationBuilder.Sql(
                """
        INSERT INTO Locations (Name, Description, IsActive, CreatedAt)
        SELECT DISTINCT TRIM(Location), NULL, 1, CURRENT_TIMESTAMP
        FROM Tickets
        WHERE TRIM(Location) <> '';
        """);

            // Üres vagy hiányzó régi helyszín esetére biztosít egy használható rekordot.
            migrationBuilder.Sql(
                """
        INSERT INTO Locations (Name, Description, IsActive, CreatedAt)
        SELECT 'Egyéb', NULL, 1, CURRENT_TIMESTAMP
        WHERE NOT EXISTS (SELECT 1 FROM Locations);
        """);

            migrationBuilder.Sql(
                """
        INSERT INTO Locations (Name, Description, IsActive, CreatedAt)
        SELECT 'Egyéb', NULL, 1, CURRENT_TIMESTAMP
        WHERE EXISTS (
            SELECT 1 FROM Tickets WHERE TRIM(Location) = ''
        )
        AND NOT EXISTS (
            SELECT 1 FROM Locations WHERE Name = 'Egyéb'
        );
        """);

            // A kezdeti prioritások megegyeznek a régi enum értékeivel.
            // A Normál lesz az új ticketek alapértelmezett prioritása.
            migrationBuilder.Sql(
                """
        INSERT INTO Priorities
            (Id, Name, DefaultDueDays, IsDefault, IsActive, CreatedAt)
        VALUES
            (1, 'Alacsony', 10, 0, 1, CURRENT_TIMESTAMP),
            (2, 'Normál',    5, 1, 1, CURRENT_TIMESTAMP),
            (3, 'Magas',     3, 0, 1, CURRENT_TIMESTAMP),
            (4, 'Sürgős',    1, 0, 1, CURRENT_TIMESTAMP);
        """);

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "Tickets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriorityId",
                table: "Tickets",
                type: "INTEGER",
                nullable: true);

            // A meglévő helyszínneveket összekapcsolja az új Locations rekordokkal.
            migrationBuilder.Sql(
                """
        UPDATE Tickets
        SET LocationId = (
            SELECT Id
            FROM Locations
            WHERE Name = TRIM(Tickets.Location)
            LIMIT 1
        )
        WHERE TRIM(Location) <> '';
        """);

            migrationBuilder.Sql(
                """
        UPDATE Tickets
        SET LocationId = (
            SELECT Id FROM Locations WHERE Name = 'Egyéb' LIMIT 1
        )
        WHERE TRIM(Location) = '';
        """);

            // A régi enum-szövegeket az új Priorities azonosítóira képezi le.
            migrationBuilder.Sql(
                """
        UPDATE Tickets
        SET PriorityId = CASE TRIM(Priority)
            WHEN 'Alacsony' THEN 1
            WHEN 'Normál'   THEN 2
            WHEN 'Normal'   THEN 2
            WHEN 'Magas'    THEN 3
            WHEN 'Sürgős'   THEN 4
            WHEN 'Surgos'   THEN 4
            ELSE 2
        END;
        """);

            migrationBuilder.AlterColumn<int>(
                name: "LocationId",
                table: "Tickets",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PriorityId",
                table: "Tickets",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Name",
                table: "Locations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Priorities_Name",
                table: "Priorities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_LocationId",
                table: "Tickets",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_PriorityId",
                table: "Tickets",
                column: "PriorityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Locations_LocationId",
                table: "Tickets",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Priorities_PriorityId",
                table: "Tickets",
                column: "PriorityId",
                principalTable: "Priorities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Locations_LocationId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Priorities_PriorityId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_LocationId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_PriorityId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "PriorityId",
                table: "Tickets");

            migrationBuilder.DropTable(name: "Locations");
            migrationBuilder.DropTable(name: "Priorities");
        }
    }
}
