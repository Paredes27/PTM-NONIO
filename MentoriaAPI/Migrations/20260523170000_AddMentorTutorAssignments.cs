using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentoriaAPI.Migrations
{
	public partial class AddMentorTutorAssignments : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "MentorTutorAssignments",
				columns: table => new
				{
					AssignmentId = table.Column<int>(type: "INTEGER", nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					MentorId = table.Column<int>(type: "INTEGER", nullable: false),
					TutorId = table.Column<int>(type: "INTEGER", nullable: false),
					Course = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
					Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
					Summary = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
					TutorEmailSent = table.Column<bool>(type: "INTEGER", nullable: false),
					AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_MentorTutorAssignments", x => x.AssignmentId);
					table.ForeignKey(
						name: "FK_MentorTutorAssignments_Users_MentorId",
						column: x => x.MentorId,
						principalTable: "Users",
						principalColumn: "UserId",
						onDelete: ReferentialAction.Restrict);
					table.ForeignKey(
						name: "FK_MentorTutorAssignments_Users_TutorId",
						column: x => x.TutorId,
						principalTable: "Users",
						principalColumn: "UserId",
						onDelete: ReferentialAction.Restrict);
				});

			migrationBuilder.CreateIndex(
				name: "IX_MentorTutorAssignments_MentorId",
				table: "MentorTutorAssignments",
				column: "MentorId");

			migrationBuilder.CreateIndex(
				name: "IX_MentorTutorAssignments_TutorId",
				table: "MentorTutorAssignments",
				column: "TutorId");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "MentorTutorAssignments");
		}
	}
}
