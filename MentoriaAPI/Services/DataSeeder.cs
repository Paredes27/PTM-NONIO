using MentoriaAPI.Data;
using MentoriaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace MentoriaAPI.Services
{
	public class DataSeeder
	{
		private readonly ApplicationDbContext _context;
		private readonly PasswordService _passwordService;

		public DataSeeder(ApplicationDbContext context, PasswordService passwordService)
		{
			_context = context;
			_passwordService = passwordService;
		}

		public async Task SeedAsync()
		{
			if (await _context.Users.AnyAsync())
			{
				await EnsureTutoringSeedAsync();
				if (!await _context.MentorshipMatches.AnyAsync())
				{
					await SeedDemoOperationalDataAsync();
				}
				return;
			}

			var studentRole = new Role { RoleName = "Student" };
			var mentorRole = new Role { RoleName = "Mentor" };
			var tutorRole = new Role { RoleName = "Tutor" };
			var adminRole = new Role { RoleName = "Admin" };

			_context.Roles.AddRange(studentRole, mentorRole, tutorRole, adminRole);

			var admin = new User
			{
				Name = "Administrador PTM",
				Email = "admin@mentoria.local",
				PasswordHash = _passwordService.HashPassword("Admin123!"),
				Bio = "Gestão global da plataforma.",
				AreasOfInterest = "gestão,analytics,coordenação",
				Availability = "segunda-manha,quarta-tarde"
			};

			var student1 = new User
			{
				Name = "João Gonçalves",
				Email = "joao@mentoria.local",
				PasswordHash = _passwordService.HashPassword("Joao123!"),
				Bio = "Aluno de LEI a precisar de apoio em programação e algoritmos.",
				AreasOfInterest = "csharp,bases de dados,algoritmos",
				Availability = "segunda-manha,terca-tarde,quinta-manha",
				StudentProfile = new StudentProfile
				{
					Course = "LEI",
					AcademicYear = "1",
					Goals = "aprender csharp e algoritmos",
					AverageGrade = 12.5m,
					EnrollmentPhase = 1,
					IsEnrolled = true
				}
			};

			var student2 = new User
			{
				Name = "Bruno Silva",
				Email = "bruno@mentoria.local",
				PasswordHash = _passwordService.HashPassword("Aluno123!"),
				Bio = "Interessado em desenvolvimento web.",
				AreasOfInterest = "web,api,sql",
				Availability = "quarta-tarde,sexta-manha",
				StudentProfile = new StudentProfile
				{
					Course = "LEI",
					AcademicYear = "2",
					Goals = "evoluir em api e web",
					AverageGrade = 14.8m,
					EnrollmentPhase = 1,
					IsEnrolled = true
				}
			};

			var student3 = new User
			{
				Name = "Marta Costa",
				Email = "marta@mentoria.local",
				PasswordHash = _passwordService.HashPassword("Aluno123!"),
				Bio = "Aluna de primeiro ano num curso ainda sem mentores disponíveis.",
				AreasOfInterest = "matematica,programacao",
				Availability = "segunda-tarde,quinta-manha",
				StudentProfile = new StudentProfile
				{
					Course = "LCC",
					AcademicYear = "1",
					Goals = "integração na universidade e organização de estudo",
					AverageGrade = 13.1m,
					EnrollmentPhase = 2,
					IsEnrolled = true
				}
			};

			var additionalStudents = new List<User>
			{
				CreateStudent(
					"Inês Almeida",
					"ines@mentoria.local",
					"Aluno123!",
					"LEI",
					1,
					"csharp,algoritmos,api",
					"segunda-manha,quarta-tarde",
					"aprender csharp e melhorar algoritmos",
					12.2m,
					1),
				CreateStudent(
					"Rui Fernandes",
					"rui@mentoria.local",
					"Aluno123!",
					"LEI",
					1,
					"sql,bases de dados,backend",
					"terca-tarde,sexta-manha",
					"melhorar sql e modelação de dados",
					13.7m,
					1),
				CreateStudent(
					"Sofia Ramos",
					"sofia@mentoria.local",
					"Aluno123!",
					"LEI",
					1,
					"csharp,api,backend",
					"segunda-manha,quinta-manha",
					"aprofundar csharp e construção de api",
					11.9m,
					2),
				CreateStudent(
					"Diogo Martins",
					"diogo@mentoria.local",
					"Aluno123!",
					"LEI",
					1,
					"csharp,algoritmos,web",
					"quarta-tarde,quinta-manha",
					"reforçar csharp e programação",
					12.8m,
					2),
				CreateStudent(
					"Carla Pinto",
					"carla@mentoria.local",
					"Aluno123!",
					"LEI",
					1,
					"sql,arquitetura,backend",
					"terca-tarde,sexta-manha",
					"ganhar confiança em sql",
					14.1m,
					3),
				CreateStudent(
					"Nuno Barros",
					"nuno@mentoria.local",
					"Aluno123!",
					"LEI",
					1,
					"design,comunicacao,multimedia",
					"sabado-manha,domingo-tarde",
					"adaptação ao ensino superior",
					15.0m,
					3),
				CreateStudent(
					"Pedro Nunes",
					"pedro@mentoria.local",
					"Aluno123!",
					"LCC",
					1,
					"matematica,programacao,logica",
					"segunda-tarde,quinta-manha",
					"integração e organização do estudo",
					13.4m,
					2),
				CreateStudent(
					"Beatriz Lopes",
					"beatriz@mentoria.local",
					"Aluno123!",
					"LEI",
					1,
					"web,api,frontend",
					"quarta-tarde,sexta-manha",
					"melhorar desenvolvimento web e api",
					12.6m,
					1),
				CreateStudent(
					"Tomás Ribeiro",
					"tomas@mentoria.local",
					"Aluno123!",
					"LEI",
					1,
					"sql,bases de dados,backend",
					"terca-tarde,sexta-manha",
					"reforçar bases de dados e backend",
					11.7m,
					2),
				CreateStudent(
					"Leonor Matos",
					"leonor@mentoria.local",
					"Aluno123!",
					"LCC",
					1,
					"logica,matematica,programacao",
					"segunda-tarde,quinta-manha",
					"melhorar lógica e programação",
					14.3m,
					1),
				CreateStudent(
					"Gabriel Sousa",
					"gabriel@mentoria.local",
					"Aluno123!",
					"LCC",
					1,
					"estatistica,matematica,dados",
					"terca-manha,quinta-manha",
					"aprofundar matemática aplicada e dados",
					13.0m,
					3),
				CreateStudent(
					"Mariana Alves",
					"mariana@mentoria.local",
					"Aluno123!",
					"LEI",
					1,
					"csharp,api,testes",
					"segunda-manha,quinta-manha",
					"aprender testes e construção de api",
					12.9m,
					2),
				CreateStudent(
					"Henrique Faria",
					"henrique@mentoria.local",
					"Aluno123!",
					"LEI",
					1,
					"ux,design,comunicacao",
					"sabado-manha,domingo-tarde",
					"testar cenário com compatibilidade baixa",
					15.5m,
					2)
			};

			var mentor1 = new User
			{
				Name = "Valentim Silva",
				Email = "valentim@mentoria.local",
				PasswordHash = _passwordService.HashPassword("Valentim123!"),
				Bio = "Mentor focado em backend .NET e acompanhamento académico.",
				AreasOfInterest = "csharp,api,sql",
				Availability = "segunda-manha,quarta-tarde,quinta-manha",
				MentorProfile = new MentorProfile
				{
					Course = "LEI",
					Specialization = "csharp",
					AreasOfExpertise = "csharp,api,sql",
					Availability = "segunda-manha,quarta-tarde,quinta-manha",
					MaxStudents = 4,
					YearsOfExperience = 3,
					AcademicYear = 2
				}
			};

			var mentor2 = new User
			{
				Name = "Tiago Reis",
				Email = "tiago@mentoria.local",
				PasswordHash = _passwordService.HashPassword("Mentor123!"),
				Bio = "Mentor de LCC focado em programação, matemática e organização de estudo.",
				AreasOfInterest = "programacao,matematica,logica",
				Availability = "segunda-tarde,quinta-manha",
				MentorProfile = new MentorProfile
				{
					Course = "LCC",
					Specialization = "programacao",
					AreasOfExpertise = "programacao,matematica,logica",
					Availability = "segunda-tarde,quinta-manha",
					MaxStudents = 5,
					YearsOfExperience = 5,
					AcademicYear = 3
				}
			};

			var mentor3 = new User
			{
				Name = "Sara Monteiro",
				Email = "sara@mentoria.local",
				PasswordHash = _passwordService.HashPassword("Mentor123!"),
				Bio = "Mentora LEI focada em web, APIs e frontend.",
				AreasOfInterest = "web,api,frontend",
				Availability = "quarta-tarde,sexta-manha",
				MentorProfile = new MentorProfile
				{
					Course = "LEI",
					Specialization = "web",
					AreasOfExpertise = "web,api,frontend",
					Availability = "quarta-tarde,sexta-manha",
					MaxStudents = 3,
					YearsOfExperience = 2,
					AcademicYear = 2
				}
			};

			var mentor4 = new User
			{
				Name = "Miguel Antunes",
				Email = "miguel.antunes@mentoria.local",
				PasswordHash = _passwordService.HashPassword("Mentor123!"),
				Bio = "Mentor LEI especializado em SQL, backend e arquitetura.",
				AreasOfInterest = "sql,bases de dados,backend",
				Availability = "terca-tarde,sexta-manha",
				MentorProfile = new MentorProfile
				{
					Course = "LEI",
					Specialization = "sql",
					AreasOfExpertise = "sql,bases de dados,backend,arquitetura",
					Availability = "terca-tarde,sexta-manha",
					MaxStudents = 3,
					YearsOfExperience = 4,
					AcademicYear = 3
				}
			};

			var mentor5 = new User
			{
				Name = "Clara Esteves",
				Email = "clara@mentoria.local",
				PasswordHash = _passwordService.HashPassword("Mentor123!"),
				Bio = "Mentora LCC focada em lógica, matemática e dados.",
				AreasOfInterest = "logica,matematica,dados",
				Availability = "segunda-tarde,quinta-manha,terca-manha",
				MentorProfile = new MentorProfile
				{
					Course = "LCC",
					Specialization = "matematica",
					AreasOfExpertise = "logica,matematica,dados,estatistica",
					Availability = "segunda-tarde,quinta-manha,terca-manha",
					MaxStudents = 3,
					YearsOfExperience = 3,
					AcademicYear = 2
				}
			};

			var tutor1 = new User
			{
				Name = "Prof. Ana Martins",
				Email = "ana.martins@mentoria.local",
				PasswordHash = _passwordService.HashPassword("Tutor123!"),
				Bio = "Docente tutora responsável por LEI e LCC.",
				AreasOfInterest = "tutoria,integração,programação",
				Availability = "segunda-manha,quinta-tarde",
				TutorProfile = new TutorProfile
				{
					Courses = "LEI,LCC",
					Department = "Informática",
					MaxMentors = 8
				}
			};

			var tutor2 = new User
			{
				Name = "Prof. Miguel Rocha",
				Email = "miguel.rocha@mentoria.local",
				PasswordHash = _passwordService.HashPassword("Tutor123!"),
				Bio = "Docente tutor de LEI.",
				AreasOfInterest = "acompanhamento,metodologias,web",
				Availability = "terca-manha,sexta-tarde",
				TutorProfile = new TutorProfile
				{
					Courses = "LEI",
					Department = "Informática",
					MaxMentors = 6
				}
			};

			var mentors = new[] { mentor1, mentor2, mentor3, mentor4, mentor5 };
			_context.Users.AddRange(admin, student1, student2, student3, tutor1, tutor2);
			_context.Users.AddRange(mentors);
			_context.Users.AddRange(additionalStudents);
			await _context.SaveChangesAsync();

			_context.UserRoles.AddRange(
				new UserRole { UserId = admin.UserId, RoleId = adminRole.RoleId },
				new UserRole { UserId = student1.UserId, RoleId = studentRole.RoleId },
				new UserRole { UserId = student2.UserId, RoleId = studentRole.RoleId },
				new UserRole { UserId = student3.UserId, RoleId = studentRole.RoleId },
				new UserRole { UserId = tutor1.UserId, RoleId = tutorRole.RoleId },
				new UserRole { UserId = tutor2.UserId, RoleId = tutorRole.RoleId });
			_context.UserRoles.AddRange(mentors.Select(mentor => new UserRole { UserId = mentor.UserId, RoleId = mentorRole.RoleId }));
			_context.UserRoles.AddRange(additionalStudents.Select(student => new UserRole { UserId = student.UserId, RoleId = studentRole.RoleId }));

			await _context.SaveChangesAsync();
			await SeedDemoOperationalDataAsync();
			await SeedTutoringAssignmentsAsync(3);
			await EnsureUnassignedTestStudentsAsync(studentRole);
		}

		private async Task EnsureTutoringSeedAsync()
		{
			var seedChanged = false;
			var tutorRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Tutor");
			if (tutorRole == null)
			{
				tutorRole = new Role { RoleName = "Tutor" };
				_context.Roles.Add(tutorRole);
				await _context.SaveChangesAsync();
			}

			var ana = await _context.Users
				.Include(u => u.TutorProfile)
				.FirstOrDefaultAsync(u => u.Email == "ana.martins@mentoria.local");
			if (ana == null)
			{
				ana = new User
				{
					Name = "Prof. Ana Martins",
					Email = "ana.martins@mentoria.local",
					PasswordHash = _passwordService.HashPassword("Tutor123!"),
					Bio = "Docente tutora responsável por LEI e LCC.",
					AreasOfInterest = "tutoria,integração,programação",
					Availability = "segunda-manha,quinta-tarde",
					TutorProfile = new TutorProfile
					{
						Courses = "LEI,LCC",
						Department = "Informática",
						MaxMentors = 8
					}
				};
				_context.Users.Add(ana);
				await _context.SaveChangesAsync();
			}

			if (!await _context.UserRoles.AnyAsync(ur => ur.UserId == ana.UserId && ur.RoleId == tutorRole.RoleId))
			{
				_context.UserRoles.Add(new UserRole { UserId = ana.UserId, RoleId = tutorRole.RoleId });
			}

			var miguel = await _context.Users
				.Include(u => u.TutorProfile)
				.FirstOrDefaultAsync(u => u.Email == "miguel.rocha@mentoria.local");
			if (miguel == null)
			{
				miguel = new User
				{
					Name = "Prof. Miguel Rocha",
					Email = "miguel.rocha@mentoria.local",
					PasswordHash = _passwordService.HashPassword("Tutor123!"),
					Bio = "Docente tutor de LEI.",
					AreasOfInterest = "acompanhamento,metodologias,web",
					Availability = "terca-manha,sexta-tarde",
					TutorProfile = new TutorProfile
					{
						Courses = "LEI",
						Department = "Informática",
						MaxMentors = 6
					}
				};
				_context.Users.Add(miguel);
				await _context.SaveChangesAsync();
			}

			if (!await _context.UserRoles.AnyAsync(ur => ur.UserId == miguel.UserId && ur.RoleId == tutorRole.RoleId))
			{
				_context.UserRoles.Add(new UserRole { UserId = miguel.UserId, RoleId = tutorRole.RoleId });
			}

			if (!await _context.Users.AnyAsync(u => u.Email == "marta@mentoria.local"))
			{
				var marta = new User
				{
					Name = "Marta Costa",
					Email = "marta@mentoria.local",
					PasswordHash = _passwordService.HashPassword("Aluno123!"),
					Bio = "Aluna de primeiro ano num curso ainda sem mentores disponíveis.",
					AreasOfInterest = "matematica,programacao",
					Availability = "segunda-tarde,quinta-manha",
					StudentProfile = new StudentProfile
					{
						Course = "LCC",
						AcademicYear = "1",
						Goals = "integração na universidade e organização de estudo",
						AverageGrade = 13.1m,
						EnrollmentPhase = 2,
						IsEnrolled = true
					}
				};
				_context.Users.Add(marta);
				await _context.SaveChangesAsync();

				var studentRole = await _context.Roles.FirstAsync(r => r.RoleName == "Student");
				_context.UserRoles.Add(new UserRole { UserId = marta.UserId, RoleId = studentRole.RoleId });
				seedChanged = true;
			}

			var existingStudentRole = await _context.Roles.FirstAsync(r => r.RoleName == "Student");
			var existingMentorRole = await _context.Roles.FirstAsync(r => r.RoleName == "Mentor");
			seedChanged |= await EnsureMentorSeedAsync(existingMentorRole, "Sara Monteiro", "sara@mentoria.local", "LEI", "web,api,frontend", "quarta-tarde,sexta-manha", "web", 3, 2, 2);
			seedChanged |= await EnsureMentorSeedAsync(existingMentorRole, "Miguel Antunes", "miguel.antunes@mentoria.local", "LEI", "sql,bases de dados,backend", "terca-tarde,sexta-manha", "sql", 3, 4, 3);
			seedChanged |= await EnsureMentorSeedAsync(existingMentorRole, "Clara Esteves", "clara@mentoria.local", "LCC", "logica,matematica,dados,estatistica", "segunda-tarde,quinta-manha,terca-manha", "matematica", 3, 3, 2);
			seedChanged |= await EnsureStudentSeedAsync(existingStudentRole, "Inês Almeida", "ines@mentoria.local", "LEI", "csharp,algoritmos,api", "segunda-manha,quarta-tarde", "aprender csharp e melhorar algoritmos", 12.2m, 1);
			seedChanged |= await EnsureStudentSeedAsync(existingStudentRole, "Rui Fernandes", "rui@mentoria.local", "LEI", "sql,bases de dados,backend", "terca-tarde,sexta-manha", "melhorar sql e modelação de dados", 13.7m, 1);
			seedChanged |= await EnsureStudentSeedAsync(existingStudentRole, "Sofia Ramos", "sofia@mentoria.local", "LEI", "csharp,api,backend", "segunda-manha,quinta-manha", "aprofundar csharp e construção de api", 11.9m, 2);
			seedChanged |= await EnsureStudentSeedAsync(existingStudentRole, "Diogo Martins", "diogo@mentoria.local", "LEI", "csharp,algoritmos,web", "quarta-tarde,quinta-manha", "reforçar csharp e programação", 12.8m, 2);
			seedChanged |= await EnsureStudentSeedAsync(existingStudentRole, "Carla Pinto", "carla@mentoria.local", "LEI", "sql,arquitetura,backend", "terca-tarde,sexta-manha", "ganhar confiança em sql", 14.1m, 3);
			seedChanged |= await EnsureStudentSeedAsync(existingStudentRole, "Nuno Barros", "nuno@mentoria.local", "LEI", "design,comunicacao,multimedia", "sabado-manha,domingo-tarde", "adaptação ao ensino superior", 15.0m, 3);
			seedChanged |= await EnsureStudentSeedAsync(existingStudentRole, "Pedro Nunes", "pedro@mentoria.local", "LCC", "matematica,programacao,logica", "segunda-tarde,quinta-manha", "integração e organização do estudo", 13.4m, 2);
			seedChanged |= await EnsureStudentSeedAsync(existingStudentRole, "Beatriz Lopes", "beatriz@mentoria.local", "LEI", "web,api,frontend", "quarta-tarde,sexta-manha", "melhorar desenvolvimento web e api", 12.6m, 1);
			seedChanged |= await EnsureStudentSeedAsync(existingStudentRole, "Tomás Ribeiro", "tomas@mentoria.local", "LEI", "sql,bases de dados,backend", "terca-tarde,sexta-manha", "reforçar bases de dados e backend", 11.7m, 2);
			seedChanged |= await EnsureStudentSeedAsync(existingStudentRole, "Leonor Matos", "leonor@mentoria.local", "LCC", "logica,matematica,programacao", "segunda-tarde,quinta-manha", "melhorar lógica e programação", 14.3m, 1);
			seedChanged |= await EnsureStudentSeedAsync(existingStudentRole, "Gabriel Sousa", "gabriel@mentoria.local", "LCC", "estatistica,matematica,dados", "terca-manha,quinta-manha", "aprofundar matemática aplicada e dados", 13.0m, 3);
			seedChanged |= await EnsureStudentSeedAsync(existingStudentRole, "Mariana Alves", "mariana@mentoria.local", "LEI", "csharp,api,testes", "segunda-manha,quinta-manha", "aprender testes e construção de api", 12.9m, 2);
			seedChanged |= await EnsureStudentSeedAsync(existingStudentRole, "Henrique Faria", "henrique@mentoria.local", "LEI", "ux,design,comunicacao", "sabado-manha,domingo-tarde", "testar cenário com compatibilidade baixa", 15.5m, 2);

			await _context.SaveChangesAsync();

			if (seedChanged ||
				!await _context.TutoringAssignments.AnyAsync() ||
				!await _context.MentorTutorAssignments.AnyAsync() ||
				await HasUnassignedActiveFirstYearStudentsAsync())
			{
				await SeedTutoringAssignmentsAsync(3);
			}

			await EnsureUnassignedTestStudentsAsync(existingStudentRole);
		}

		private async Task EnsureUnassignedTestStudentsAsync(Role studentRole)
		{
			var studentNames = new[]
			{
				"Pedro Folgado",
				"Afonso Teixeira",
				"Catarina Neves",
				"Guilherme Pires",
				"Rita Moreira",
				"Francisco Lima",
				"Matilde Rocha",
				"Vasco Cardoso",
				"Laura Batista",
				"Duarte Correia",
				"Carolina Freitas",
				"Gonçalo Martins",
				"Andreia Castro",
				"Ricardo Esteves",
				"Joana Tavares",
				"Marcelo Costa",
				"Filipa Azevedo",
				"Daniel Barros",
				"Eva Campos",
				"Tiago Almeida",
				"Samuel Oliveira",
				"Anaís Pereira",
				"Rodrigo Figueiredo",
				"Patrícia Nogueira",
				"Luís Machado"
			};
			var testStudents = studentNames
				.Select((name, index) =>
				{
					var number = index + 1;
					return new
					{
						Number = number,
						Name = name,
						Email = $"teste.aluno{number:00}@mentoria.local",
						Course = number % 3 == 0 ? "LCC" : "LEI",
						Interests = number % 3 == 0
							? "matematica,logica,programacao"
							: number % 2 == 0
								? "sql,bases de dados,backend"
								: "csharp,api,web",
						Availability = number % 3 == 0
							? "segunda-tarde,quinta-manha"
							: number % 2 == 0
								? "terca-tarde,sexta-manha"
								: "segunda-manha,quarta-tarde"
					};
				})
				.ToList();

			foreach (var item in testStudents)
			{
				var student = await _context.Users
					.Include(u => u.StudentProfile)
					.FirstOrDefaultAsync(u => u.Email == item.Email);

				if (student == null)
				{
					student = CreateStudent(
						item.Name,
						item.Email,
						"Aluno123!",
						item.Course,
						1,
						item.Interests,
						item.Availability,
						"aluno criado para testes de emparelhamento",
						12 + item.Number % 6,
						1);
					_context.Users.Add(student);
					await _context.SaveChangesAsync();
				}

				if (!await _context.UserRoles.AnyAsync(ur => ur.UserId == student.UserId && ur.RoleId == studentRole.RoleId))
				{
					_context.UserRoles.Add(new UserRole { UserId = student.UserId, RoleId = studentRole.RoleId });
				}

				if (!await _context.TutoringAssignments.AnyAsync(a => a.StudentId == student.UserId && a.Status == "Active"))
				{
					var tutor = await FindTutorForCourseAsync(item.Course);
					if (tutor != null)
					{
						_context.TutoringAssignments.Add(new TutoringAssignment
						{
							StudentId = student.UserId,
							MentorId = null,
							TutorId = tutor.UserId,
							Course = item.Course,
							EnrollmentPhase = 1,
							Status = "Active",
							Summary = "Aluno de teste criado sem mentor para validação do emparelhamento.",
							AllocationWarning = string.Empty,
							MentorEmailSent = false,
							TutorEmailSent = true,
							NonioTutorListUpdated = true,
							AssignedAt = DateTime.UtcNow
						});
					}
				}
			}

			await _context.SaveChangesAsync();
		}

		private async Task<User?> FindTutorForCourseAsync(string course)
		{
			var tutors = await _context.Users
				.Include(u => u.TutorProfile)
				.Include(u => u.TutorAssignments)
				.Where(u => u.IsActive && u.TutorProfile != null)
				.ToListAsync();

			return tutors
				.Where(t => t.TutorProfile!.Courses
					.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
					.Contains(course, StringComparer.OrdinalIgnoreCase))
				.OrderBy(t => t.TutorAssignments.Count(a => a.Status == "Active"))
				.ThenBy(t => t.Name)
				.FirstOrDefault();
		}

		private async Task<bool> HasUnassignedActiveFirstYearStudentsAsync()
		{
			var assignedStudentIds = await _context.TutoringAssignments
				.Where(assignment => assignment.Status == "Active")
				.Select(assignment => assignment.StudentId)
				.ToListAsync();

			return await _context.StudentProfiles.AnyAsync(profile =>
				profile.AcademicYear == "1" &&
				profile.IsEnrolled &&
				!assignedStudentIds.Contains(profile.UserId));
		}

		private User CreateStudent(
			string name,
			string email,
			string password,
			string course,
			int academicYear,
			string interests,
			string availability,
			string goals,
			decimal averageGrade,
			int enrollmentPhase)
		{
			return new User
			{
				Name = name,
				Email = email,
				PasswordHash = _passwordService.HashPassword(password),
				Bio = $"Aluno de {course} no programa PTM.",
				AreasOfInterest = interests,
				Availability = availability,
				StudentProfile = new StudentProfile
				{
					Course = course,
					AcademicYear = academicYear.ToString(),
					Goals = goals,
					AverageGrade = averageGrade,
					EnrollmentPhase = enrollmentPhase,
					IsEnrolled = true
				}
			};
		}

		private async Task<bool> EnsureStudentSeedAsync(
			Role studentRole,
			string name,
			string email,
			string course,
			string interests,
			string availability,
			string goals,
			decimal averageGrade,
			int enrollmentPhase)
		{
			if (await _context.Users.AnyAsync(u => u.Email == email))
			{
				return false;
			}

			var student = CreateStudent(name, email, "Aluno123!", course, 1, interests, availability, goals, averageGrade, enrollmentPhase);
			_context.Users.Add(student);
			await _context.SaveChangesAsync();
			_context.UserRoles.Add(new UserRole { UserId = student.UserId, RoleId = studentRole.RoleId });
			return true;
		}

		private async Task<bool> EnsureMentorSeedAsync(
			Role mentorRole,
			string name,
			string email,
			string course,
			string expertise,
			string availability,
			string specialization,
			int maxStudents,
			int yearsOfExperience,
			int academicYear)
		{
			if (await _context.Users.AnyAsync(u => u.Email == email))
			{
				return false;
			}

			var mentor = new User
			{
				Name = name,
				Email = email,
				PasswordHash = _passwordService.HashPassword("Mentor123!"),
				Bio = $"Mentor de {course} disponível para testes de emparelhamento.",
				AreasOfInterest = expertise,
				Availability = availability,
				MentorProfile = new MentorProfile
				{
					Course = course,
					Specialization = specialization,
					AreasOfExpertise = expertise,
					Availability = availability,
					MaxStudents = maxStudents,
					YearsOfExperience = yearsOfExperience,
					AcademicYear = academicYear
				}
			};

			_context.Users.Add(mentor);
			await _context.SaveChangesAsync();
			_context.UserRoles.Add(new UserRole { UserId = mentor.UserId, RoleId = mentorRole.RoleId });
			return true;
		}

		private async Task SeedTutoringAssignmentsAsync(int phase)
		{
			var distributionService = new TutoringDistributionService(_context);
			await distributionService.DistributeAsync(phase);
		}

		private async Task SeedDemoOperationalDataAsync()
		{
			var joao = await _context.Users.FirstAsync(u => u.Email == "joao@mentoria.local");
			var bruno = await _context.Users.FirstAsync(u => u.Email == "bruno@mentoria.local");
			var valentim = await _context.Users.FirstAsync(u => u.Email == "valentim@mentoria.local");
			var tiago = await _context.Users.FirstAsync(u => u.Email == "tiago@mentoria.local");

			var activeMatch = new MentorshipMatch
			{
				StudentId = joao.UserId,
				MentorId = valentim.UserId,
				CompatibilityScore = 92,
				Status = "Active",
				Summary = "Mesmo curso, interesses alinhados, disponibilidade comum e mentor com capacidade.",
				CreatedAt = DateTime.UtcNow.AddDays(-10)
			};

			var secondaryMatch = new MentorshipMatch
			{
				StudentId = bruno.UserId,
				MentorId = tiago.UserId,
				CompatibilityScore = 81,
				Status = "Active",
				Summary = "Especialização adequada em APIs e bases de dados para evolução do aluno.",
				CreatedAt = DateTime.UtcNow.AddDays(-6)
			};

			_context.MentorshipMatches.AddRange(activeMatch, secondaryMatch);
			await _context.SaveChangesAsync();

			var completedSession = new MentoringSession
			{
				MatchId = activeMatch.MatchId,
				ScheduledAt = DateTime.UtcNow.AddDays(-7),
				DurationMinutes = 60,
				Topic = "Plano de estudo para C# e algoritmos",
				Notes = "Definidos objetivos semanais e revisão de exercícios.",
				Status = "Completed",
				CompletedAt = DateTime.UtcNow.AddDays(-7).AddHours(1)
			};

			var upcomingSession = new MentoringSession
			{
				MatchId = activeMatch.MatchId,
				ScheduledAt = DateTime.UtcNow.AddDays(2),
				DurationMinutes = 45,
				Topic = "Revisão de bases de dados",
				Notes = "Preparar dúvidas sobre SQL e modelação.",
				Status = "Scheduled"
			};

			var mentorSession = new MentoringSession
			{
				MatchId = secondaryMatch.MatchId,
				ScheduledAt = DateTime.UtcNow.AddDays(1),
				DurationMinutes = 50,
				Topic = "Planeamento de API REST",
				Notes = "Definir endpoints e validação de pedidos.",
				Status = "Scheduled"
			};

			_context.MentoringSessions.AddRange(completedSession, upcomingSession, mentorSession);
			await _context.SaveChangesAsync();

			_context.Feedbacks.AddRange(
				new Feedback
				{
					SessionId = completedSession.SessionId,
					AuthorUserId = joao.UserId,
					TargetUserId = valentim.UserId,
					Rating = 5,
					Comment = "Sessão muito útil para organizar estudo e perceber prioridades.",
					CreatedAt = DateTime.UtcNow.AddDays(-6)
				},
				new Feedback
				{
					SessionId = completedSession.SessionId,
					AuthorUserId = valentim.UserId,
					TargetUserId = joao.UserId,
					Rating = 4,
					Comment = "Aluno empenhado, com boa adesão ao plano definido.",
					CreatedAt = DateTime.UtcNow.AddDays(-6)
				});

			await _context.SaveChangesAsync();
		}
	}
}
