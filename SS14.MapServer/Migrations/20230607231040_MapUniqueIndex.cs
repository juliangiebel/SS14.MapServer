using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SS14.MapServer.Migrations
{
    /// <inheritdoc />
    public partial class MapUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Map_GitRef_MapId",
                table: "Map");

            migrationBuilder.CreateIndex(
                name: "IX_Map_GitRef_MapId",
                table: "Map",
                columns: new[] { "GitRef", "MapId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Map_GitRef_MapId",
                table: "Map");

            migrationBuilder.CreateIndex(
                name: "IX_Map_GitRef_MapId",
                table: "Map",
                columns: new[] { "GitRef", "MapId" });
        }
    }
}
