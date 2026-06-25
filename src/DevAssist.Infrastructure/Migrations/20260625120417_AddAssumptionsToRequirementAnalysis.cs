using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevAssist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssumptionsToRequirementAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssumptionsJson",
                table: "RequirementAnalyses",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssumptionsJson",
                table: "RequirementAnalyses");
        }
    }
}
