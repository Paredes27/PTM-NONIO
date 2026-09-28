using MentoriaAPI.Data;
using MentoriaAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
	});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "Mentoria API",
		Version = "v1",
		Description = "API para gestão automática de tutoria e mentoria."
	});

	options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Name = "Authorization",
		Type = SecuritySchemeType.Http,
		Scheme = "bearer",
		BearerFormat = "JWT",
		In = ParameterLocation.Header,
		Description = "Introduz o token JWT no formato: Bearer {token}"
	});

	options.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "Bearer"
				}
			},
			Array.Empty<string>()
		}
	});
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<MatchingService>();
builder.Services.AddScoped<TutoringDistributionService>();
builder.Services.AddScoped<DataSeeder>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured.");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = builder.Configuration["Jwt:Issuer"],
			ValidAudience = builder.Configuration["Jwt:Audience"],
			IssuerSigningKey = signingKey
		};
	});

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
	Console.WriteLine("Inicializar base de dados...");
	dbContext.Database.EnsureCreated();
	Console.WriteLine("Garantir esquema de tutoria...");
	await EnsureTutoringSchemaAsync(dbContext);
	Console.WriteLine("Criar dados de demonstração...");
	var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
	await seeder.SeedAsync();
	Console.WriteLine("Dados prontos.");
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

static async Task EnsureTutoringSchemaAsync(ApplicationDbContext dbContext)
{
	await dbContext.Database.ExecuteSqlRawAsync("""
		CREATE TABLE IF NOT EXISTS "TutorProfiles" (
			"UserId" INTEGER NOT NULL CONSTRAINT "PK_TutorProfiles" PRIMARY KEY,
			"Courses" TEXT NOT NULL,
			"Department" TEXT NOT NULL,
			"MaxMentors" INTEGER NOT NULL,
			CONSTRAINT "FK_TutorProfiles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("UserId") ON DELETE CASCADE
		);
		""");

	await dbContext.Database.ExecuteSqlRawAsync("""
		CREATE TABLE IF NOT EXISTS "TutoringAssignments" (
			"AssignmentId" INTEGER NOT NULL CONSTRAINT "PK_TutoringAssignments" PRIMARY KEY AUTOINCREMENT,
			"StudentId" INTEGER NOT NULL,
			"MentorId" INTEGER NULL,
			"TutorId" INTEGER NOT NULL,
			"Course" TEXT NOT NULL,
			"EnrollmentPhase" INTEGER NOT NULL,
			"Status" TEXT NOT NULL,
			"Summary" TEXT NOT NULL,
			"AllocationWarning" TEXT NOT NULL DEFAULT '',
			"MentorEmailSent" INTEGER NOT NULL,
			"TutorEmailSent" INTEGER NOT NULL,
			"NonioTutorListUpdated" INTEGER NOT NULL,
			"AssignedAt" TEXT NOT NULL,
			CONSTRAINT "FK_TutoringAssignments_Users_MentorId" FOREIGN KEY ("MentorId") REFERENCES "Users" ("UserId"),
			CONSTRAINT "FK_TutoringAssignments_Users_StudentId" FOREIGN KEY ("StudentId") REFERENCES "Users" ("UserId"),
			CONSTRAINT "FK_TutoringAssignments_Users_TutorId" FOREIGN KEY ("TutorId") REFERENCES "Users" ("UserId")
		);
		""");

	await dbContext.Database.ExecuteSqlRawAsync("""
		CREATE TABLE IF NOT EXISTS "MentorTutorAssignments" (
			"AssignmentId" INTEGER NOT NULL CONSTRAINT "PK_MentorTutorAssignments" PRIMARY KEY AUTOINCREMENT,
			"MentorId" INTEGER NOT NULL,
			"TutorId" INTEGER NOT NULL,
			"Course" TEXT NOT NULL,
			"Status" TEXT NOT NULL,
			"Summary" TEXT NOT NULL,
			"TutorEmailSent" INTEGER NOT NULL,
			"AssignedAt" TEXT NOT NULL,
			CONSTRAINT "FK_MentorTutorAssignments_Users_MentorId" FOREIGN KEY ("MentorId") REFERENCES "Users" ("UserId"),
			CONSTRAINT "FK_MentorTutorAssignments_Users_TutorId" FOREIGN KEY ("TutorId") REFERENCES "Users" ("UserId")
		);
		""");

	await TryAddColumnAsync(dbContext, "StudentProfiles", "EnrollmentPhase", """ALTER TABLE "StudentProfiles" ADD COLUMN "EnrollmentPhase" INTEGER NOT NULL DEFAULT 1;""");
	await TryAddColumnAsync(dbContext, "StudentProfiles", "IsEnrolled", """ALTER TABLE "StudentProfiles" ADD COLUMN "IsEnrolled" INTEGER NOT NULL DEFAULT 1;""");
	await TryAddColumnAsync(dbContext, "MentorProfiles", "AcademicYear", """ALTER TABLE "MentorProfiles" ADD COLUMN "AcademicYear" INTEGER NOT NULL DEFAULT 2;""");
	await TryAddColumnAsync(dbContext, "TutoringAssignments", "AllocationWarning", """ALTER TABLE "TutoringAssignments" ADD COLUMN "AllocationWarning" TEXT NOT NULL DEFAULT '';""");
}

static async Task TryAddColumnAsync(ApplicationDbContext dbContext, string table, string column, string sql)
{
	if (!await ColumnExistsAsync(dbContext, table, column))
	{
		await dbContext.Database.ExecuteSqlRawAsync(sql);
	}
}

static async Task<bool> ColumnExistsAsync(ApplicationDbContext dbContext, string table, string column)
{
	var connection = dbContext.Database.GetDbConnection();
	if (connection.State != System.Data.ConnectionState.Open)
	{
		await connection.OpenAsync();
	}

	await using var command = connection.CreateCommand();
	command.CommandText = $"PRAGMA table_info(\"{table}\");";
	await using var reader = await command.ExecuteReaderAsync();
	while (await reader.ReadAsync())
	{
		if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
	}

	return false;
}
