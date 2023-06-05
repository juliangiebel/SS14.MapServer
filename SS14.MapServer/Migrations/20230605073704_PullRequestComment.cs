using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SS14.MapServer.Migrations
{
    /// <inheritdoc />
    public partial class PullRequestComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PullRequestComment",
                columns: table => new
                {
                    Owner = table.Column<string>(type: "text", nullable: false),
                    Repository = table.Column<string>(type: "text", nullable: false),
                    IssueNumber = table.Column<int>(type: "integer", nullable: false),
                    CommentId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullRequestComment", x => new { x.Owner, x.Repository, x.IssueNumber });
                });

            migrationBuilder.CreateIndex(
                name: "IX_Grid_GridId",
                table: "Grid",
                column: "GridId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PullRequestComment");

            migrationBuilder.DropIndex(
                name: "IX_Grid_GridId",
                table: "Grid");
        }
    }
}
