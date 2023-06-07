using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using SS14.MapServer.Models.Types;

#nullable disable

namespace SS14.MapServer.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Image",
                columns: table => new
                {
                    Path = table.Column<string>(type: "text", nullable: false),
                    InternalPath = table.Column<string>(type: "text", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Image", x => x.Path);
                });

            migrationBuilder.CreateTable(
                name: "Map",
                columns: table => new
                {
                    MapGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    GitRef = table.Column<string>(type: "text", nullable: false),
                    MapId = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Attribution = table.Column<string>(type: "text", nullable: true),
                    ParallaxLayers = table.Column<List<ParallaxLayer>>(type: "jsonb", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Map", x => x.MapGuid);
                });

            migrationBuilder.CreateTable(
                name: "Tile",
                columns: table => new
                {
                    MapGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    GridId = table.Column<int>(type: "integer", nullable: false),
                    X = table.Column<int>(type: "integer", nullable: false),
                    Y = table.Column<int>(type: "integer", nullable: false),
                    Size = table.Column<int>(type: "integer", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tile", x => new { x.MapGuid, x.GridId, x.X, x.Y });
                });

            migrationBuilder.CreateTable(
                name: "Grid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GridId = table.Column<int>(type: "integer", nullable: false),
                    Tiled = table.Column<bool>(type: "boolean", nullable: false),
                    TileSize = table.Column<int>(type: "integer", nullable: false),
                    Offset_X = table.Column<float>(type: "real", nullable: false),
                    Offset_Y = table.Column<float>(type: "real", nullable: false),
                    Extent_A_X = table.Column<float>(type: "real", nullable: false),
                    Extent_A_Y = table.Column<float>(type: "real", nullable: false),
                    Extent_B_X = table.Column<float>(type: "real", nullable: false),
                    Extent_B_Y = table.Column<float>(type: "real", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    MapGuid = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grid", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Grid_Map_MapGuid",
                        column: x => x.MapGuid,
                        principalTable: "Map",
                        principalColumn: "MapGuid");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Grid_MapGuid",
                table: "Grid",
                column: "MapGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Map_GitRef_MapId",
                table: "Map",
                columns: new[] { "GitRef", "MapId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tile_MapGuid_GridId",
                table: "Tile",
                columns: new[] { "MapGuid", "GridId" });

            migrationBuilder.Sql(@"
                DROP FUNCTION IF EXISTS update_timestamp;
                CREATE FUNCTION update_timestamp() RETURNS TRIGGER
	                LANGUAGE plpgsql AS $$
	                BEGIN
		                NEW.""LastUpdated"" = CURRENT_TIMESTAMP;
                            RETURN NEW;
                    END;
                    $$;
            ");

            migrationBuilder.Sql(@"
                CREATE TRIGGER ""Map_UPDATE""
                        BEFORE UPDATE ON ""Map""
                        FOR EACH ROW
                        EXECUTE PROCEDURE update_timestamp();
                ");

            migrationBuilder.Sql(@"
                CREATE TRIGGER ""Image_UPDATE""
                        BEFORE UPDATE ON ""Image""
                        FOR EACH ROW
                        EXECUTE PROCEDURE update_timestamp();
                ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Grid");

            migrationBuilder.DropTable(
                name: "Image");

            migrationBuilder.DropTable(
                name: "Tile");

            migrationBuilder.DropTable(
                name: "Map");

            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS update_timestamp;");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS ""Map_Update"";");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS ""Image_Update"";");
        }
    }
}
