using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SS14.MapServer.Migrations
{
    /// <inheritdoc />
    public partial class MapGitRefIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Map_GitRef",
                table: "Map",
                column: "GitRef");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Map_GitRef",
                table: "Map");
        }
    }
}
