using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SDAHymns.Core.Migrations
{
    /// <inheritdoc />
    public partial class VerseOrderIsUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Verses_HymnId_DisplayOrder",
                table: "Verses");

            migrationBuilder.DropIndex(
                name: "IX_Verses_HymnId_VerseNumber",
                table: "Verses");

            migrationBuilder.CreateIndex(
                name: "IX_Verses_HymnId_DisplayOrder",
                table: "Verses",
                columns: new[] { "HymnId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Verses_HymnId_VerseNumber",
                table: "Verses",
                columns: new[] { "HymnId", "VerseNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Verses_HymnId_DisplayOrder",
                table: "Verses");

            migrationBuilder.DropIndex(
                name: "IX_Verses_HymnId_VerseNumber",
                table: "Verses");

            migrationBuilder.CreateIndex(
                name: "IX_Verses_HymnId_DisplayOrder",
                table: "Verses",
                columns: new[] { "HymnId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Verses_HymnId_VerseNumber",
                table: "Verses",
                columns: new[] { "HymnId", "VerseNumber" },
                unique: true);
        }
    }
}
