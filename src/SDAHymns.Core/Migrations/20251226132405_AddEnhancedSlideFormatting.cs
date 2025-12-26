using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SDAHymns.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddEnhancedSlideFormatting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChorusBackgroundColor",
                table: "DisplayProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ChorusIndentAmount",
                table: "DisplayProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ChorusItalic",
                table: "DisplayProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ChorusStyle",
                table: "DisplayProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ChorusTextColor",
                table: "DisplayProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "EnableBlackEndingSlide",
                table: "DisplayProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EndingSlideAutoCloseDuration",
                table: "DisplayProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MetadataColor",
                table: "DisplayProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MetadataFontSize",
                table: "DisplayProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "MetadataOpacity",
                table: "DisplayProfiles",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "MetadataPosition",
                table: "DisplayProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ParagraphSpacing",
                table: "DisplayProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ShowCategory",
                table: "DisplayProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowChorusLabel",
                table: "DisplayProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowHymnNumber",
                table: "DisplayProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowTitleOnFirstVerseOnly",
                table: "DisplayProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowVerseIndicator",
                table: "DisplayProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TransitionDuration",
                table: "DisplayProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VerseNumberColor",
                table: "DisplayProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "VerseNumberSeparateLine",
                table: "DisplayProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "VerseNumberSize",
                table: "DisplayProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VerseNumberStyle",
                table: "DisplayProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "VerseSpacing",
                table: "DisplayProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VerseTransition",
                table: "DisplayProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "DisplayProfiles",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ChorusBackgroundColor", "ChorusIndentAmount", "ChorusItalic", "ChorusStyle", "ChorusTextColor", "EnableBlackEndingSlide", "EndingSlideAutoCloseDuration", "MetadataColor", "MetadataFontSize", "MetadataOpacity", "MetadataPosition", "ParagraphSpacing", "ShowCategory", "ShowChorusLabel", "ShowHymnNumber", "ShowTitleOnFirstVerseOnly", "ShowVerseIndicator", "TransitionDuration", "VerseNumberColor", "VerseNumberSeparateLine", "VerseNumberSize", "VerseNumberStyle", "VerseSpacing", "VerseTransition" },
                values: new object[] { "#1A1A1A", 80, false, "SameAsVerse", "#E0E0E0", true, 10, "#888888", 20, 0.69999999999999996, "None", 40, false, true, false, false, false, 300, "#CCCCCC", false, 32, "InlinePlain", 60, "None" });

            migrationBuilder.UpdateData(
                table: "DisplayProfiles",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ChorusBackgroundColor", "ChorusIndentAmount", "ChorusItalic", "ChorusStyle", "ChorusTextColor", "EnableBlackEndingSlide", "EndingSlideAutoCloseDuration", "MetadataColor", "MetadataFontSize", "MetadataOpacity", "MetadataPosition", "ParagraphSpacing", "ShowCategory", "ShowChorusLabel", "ShowHymnNumber", "ShowTitleOnFirstVerseOnly", "ShowVerseIndicator", "TransitionDuration", "VerseNumberColor", "VerseNumberSeparateLine", "VerseNumberSize", "VerseNumberStyle", "VerseSpacing", "VerseTransition" },
                values: new object[] { "#1A1A1A", 80, false, "SameAsVerse", "#E0E0E0", true, 10, "#888888", 20, 0.69999999999999996, "None", 40, false, true, false, false, false, 300, "#CCCCCC", false, 32, "InlinePlain", 60, "None" });

            migrationBuilder.UpdateData(
                table: "DisplayProfiles",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ChorusBackgroundColor", "ChorusIndentAmount", "ChorusItalic", "ChorusStyle", "ChorusTextColor", "EnableBlackEndingSlide", "EndingSlideAutoCloseDuration", "MetadataColor", "MetadataFontSize", "MetadataOpacity", "MetadataPosition", "ParagraphSpacing", "ShowCategory", "ShowChorusLabel", "ShowHymnNumber", "ShowTitleOnFirstVerseOnly", "ShowVerseIndicator", "TransitionDuration", "VerseNumberColor", "VerseNumberSeparateLine", "VerseNumberSize", "VerseNumberStyle", "VerseSpacing", "VerseTransition" },
                values: new object[] { "#1A1A1A", 80, false, "SameAsVerse", "#E0E0E0", true, 10, "#888888", 20, 0.69999999999999996, "None", 40, false, true, false, false, false, 300, "#CCCCCC", false, 32, "InlinePlain", 60, "None" });

            migrationBuilder.UpdateData(
                table: "DisplayProfiles",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ChorusBackgroundColor", "ChorusIndentAmount", "ChorusItalic", "ChorusStyle", "ChorusTextColor", "EnableBlackEndingSlide", "EndingSlideAutoCloseDuration", "MetadataColor", "MetadataFontSize", "MetadataOpacity", "MetadataPosition", "ParagraphSpacing", "ShowCategory", "ShowChorusLabel", "ShowHymnNumber", "ShowTitleOnFirstVerseOnly", "ShowVerseIndicator", "TransitionDuration", "VerseNumberColor", "VerseNumberSeparateLine", "VerseNumberSize", "VerseNumberStyle", "VerseSpacing", "VerseTransition" },
                values: new object[] { "#1A1A1A", 80, false, "SameAsVerse", "#E0E0E0", true, 10, "#888888", 20, 0.69999999999999996, "None", 40, false, true, false, false, false, 300, "#CCCCCC", false, 32, "InlinePlain", 60, "None" });

            migrationBuilder.UpdateData(
                table: "DisplayProfiles",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ChorusBackgroundColor", "ChorusIndentAmount", "ChorusItalic", "ChorusStyle", "ChorusTextColor", "EnableBlackEndingSlide", "EndingSlideAutoCloseDuration", "MetadataColor", "MetadataFontSize", "MetadataOpacity", "MetadataPosition", "ParagraphSpacing", "ShowCategory", "ShowChorusLabel", "ShowHymnNumber", "ShowTitleOnFirstVerseOnly", "ShowVerseIndicator", "TransitionDuration", "VerseNumberColor", "VerseNumberSeparateLine", "VerseNumberSize", "VerseNumberStyle", "VerseSpacing", "VerseTransition" },
                values: new object[] { "#1A1A1A", 80, false, "SameAsVerse", "#E0E0E0", true, 10, "#888888", 20, 0.69999999999999996, "None", 40, false, true, false, false, false, 300, "#CCCCCC", false, 32, "InlinePlain", 60, "None" });

            migrationBuilder.UpdateData(
                table: "DisplayProfiles",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "ChorusBackgroundColor", "ChorusIndentAmount", "ChorusItalic", "ChorusStyle", "ChorusTextColor", "EnableBlackEndingSlide", "EndingSlideAutoCloseDuration", "MetadataColor", "MetadataFontSize", "MetadataOpacity", "MetadataPosition", "ParagraphSpacing", "ShowCategory", "ShowChorusLabel", "ShowHymnNumber", "ShowTitleOnFirstVerseOnly", "ShowVerseIndicator", "TransitionDuration", "VerseNumberColor", "VerseNumberSeparateLine", "VerseNumberSize", "VerseNumberStyle", "VerseSpacing", "VerseTransition" },
                values: new object[] { "#1A1A1A", 80, false, "SameAsVerse", "#E0E0E0", true, 10, "#888888", 20, 0.69999999999999996, "None", 40, false, true, false, false, false, 300, "#CCCCCC", false, 32, "InlinePlain", 60, "None" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChorusBackgroundColor",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "ChorusIndentAmount",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "ChorusItalic",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "ChorusStyle",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "ChorusTextColor",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "EnableBlackEndingSlide",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "EndingSlideAutoCloseDuration",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "MetadataColor",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "MetadataFontSize",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "MetadataOpacity",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "MetadataPosition",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "ParagraphSpacing",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "ShowCategory",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "ShowChorusLabel",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "ShowHymnNumber",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "ShowTitleOnFirstVerseOnly",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "ShowVerseIndicator",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "TransitionDuration",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "VerseNumberColor",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "VerseNumberSeparateLine",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "VerseNumberSize",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "VerseNumberStyle",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "VerseSpacing",
                table: "DisplayProfiles");

            migrationBuilder.DropColumn(
                name: "VerseTransition",
                table: "DisplayProfiles");
        }
    }
}
