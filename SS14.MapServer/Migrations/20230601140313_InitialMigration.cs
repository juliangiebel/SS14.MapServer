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
                name: "Images",
                columns: table => new
                {
                    Path = table.Column<string>(type: "text", nullable: false),
                    InternalPath = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Path);
                });

            migrationBuilder.CreateTable(
                name: "Maps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MapId = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Attribution = table.Column<string>(type: "text", nullable: true),
                    ParallaxLayers = table.Column<List<ParallaxLayer>>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tiles",
                columns: table => new
                {
                    MapId = table.Column<string>(type: "text", nullable: false),
                    GridId = table.Column<int>(type: "integer", nullable: false),
                    X = table.Column<int>(type: "integer", nullable: false),
                    Y = table.Column<int>(type: "integer", nullable: false),
                    Size = table.Column<int>(type: "integer", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tiles", x => new { x.MapId, x.GridId, x.X, x.Y });
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
                    MapId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grid", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Grid_Maps_MapId",
                        column: x => x.MapId,
                        principalTable: "Maps",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Grid_MapId",
                table: "Grid",
                column: "MapId");

            migrationBuilder.CreateIndex(
                name: "IX_Tiles_MapId_GridId",
                table: "Tiles",
                columns: new[] { "MapId", "GridId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Grid");

            migrationBuilder.DropTable(
                name: "Images");

            migrationBuilder.DropTable(
                name: "Tiles");

            migrationBuilder.DropTable(
                name: "Maps");
        }
    }
}
