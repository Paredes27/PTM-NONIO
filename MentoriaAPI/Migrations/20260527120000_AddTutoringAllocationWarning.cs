using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentoriaAPI.Migrations
{
	public partial class AddTutoringAllocationWarning : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<string>(
				name: "AllocationWarning",
				table: "TutoringAssignments",
				type: "TEXT",
				maxLength: 800,
				nullable: false,
				defaultValue: "");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "AllocationWarning",
				table: "TutoringAssignments");
		}
	}
}
