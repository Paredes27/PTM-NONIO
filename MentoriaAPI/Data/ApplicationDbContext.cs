using MentoriaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace MentoriaAPI.Data
{
	public class ApplicationDbContext : DbContext
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options)
		{
		}

		public DbSet<User> Users { get; set; }
		public DbSet<Role> Roles { get; set; }
		public DbSet<UserRole> UserRoles { get; set; }
		public DbSet<StudentProfile> StudentProfiles { get; set; }
		public DbSet<MentorProfile> MentorProfiles { get; set; }
		public DbSet<TutorProfile> TutorProfiles { get; set; }
		public DbSet<MentorshipMatch> MentorshipMatches { get; set; }
		public DbSet<MentorTutorAssignment> MentorTutorAssignments { get; set; }
		public DbSet<TutoringAssignment> TutoringAssignments { get; set; }
		public DbSet<MentoringSession> MentoringSessions { get; set; }
		public DbSet<Feedback> Feedbacks { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<User>()
				.HasIndex(u => u.Email)
				.IsUnique();

			modelBuilder.Entity<Role>()
				.HasIndex(r => r.RoleName)
				.IsUnique();

			modelBuilder.Entity<UserRole>()
				.HasKey(ur => new { ur.UserId, ur.RoleId });

			modelBuilder.Entity<UserRole>()
				.HasOne(ur => ur.User)
				.WithMany(u => u.UserRoles)
				.HasForeignKey(ur => ur.UserId);

			modelBuilder.Entity<UserRole>()
				.HasOne(ur => ur.Role)
				.WithMany(r => r.UserRoles)
				.HasForeignKey(ur => ur.RoleId);

			modelBuilder.Entity<StudentProfile>()
				.HasOne(sp => sp.User)
				.WithOne(u => u.StudentProfile)
				.HasForeignKey<StudentProfile>(sp => sp.UserId);

			modelBuilder.Entity<MentorProfile>()
				.HasOne(mp => mp.User)
				.WithOne(u => u.MentorProfile)
				.HasForeignKey<MentorProfile>(mp => mp.UserId);

			modelBuilder.Entity<TutorProfile>()
				.HasOne(tp => tp.User)
				.WithOne(u => u.TutorProfile)
				.HasForeignKey<TutorProfile>(tp => tp.UserId);

			modelBuilder.Entity<MentorTutorAssignment>()
				.HasOne(a => a.Mentor)
				.WithMany(u => u.MentorTutorAssignments)
				.HasForeignKey(a => a.MentorId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<MentorTutorAssignment>()
				.HasOne(a => a.Tutor)
				.WithMany(u => u.TutorMentorAssignments)
				.HasForeignKey(a => a.TutorId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<TutoringAssignment>()
				.HasOne(a => a.Student)
				.WithMany(u => u.StudentTutoringAssignments)
				.HasForeignKey(a => a.StudentId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<TutoringAssignment>()
				.HasOne(a => a.Mentor)
				.WithMany(u => u.MentorTutoringAssignments)
				.HasForeignKey(a => a.MentorId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<TutoringAssignment>()
				.HasOne(a => a.Tutor)
				.WithMany(u => u.TutorAssignments)
				.HasForeignKey(a => a.TutorId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<MentorshipMatch>()
				.HasOne(m => m.Student)
				.WithMany(u => u.StudentMatches)
				.HasForeignKey(m => m.StudentId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<MentorshipMatch>()
				.HasOne(m => m.Mentor)
				.WithMany(u => u.MentorMatches)
				.HasForeignKey(m => m.MentorId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<MentoringSession>()
				.HasOne(s => s.Match)
				.WithMany(m => m.Sessions)
				.HasForeignKey(s => s.MatchId);

			modelBuilder.Entity<Feedback>()
				.HasOne(f => f.Session)
				.WithMany(s => s.Feedbacks)
				.HasForeignKey(f => f.SessionId);

			modelBuilder.Entity<Feedback>()
				.HasOne(f => f.AuthorUser)
				.WithMany(u => u.AuthoredFeedbacks)
				.HasForeignKey(f => f.AuthorUserId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Feedback>()
				.HasOne(f => f.TargetUser)
				.WithMany(u => u.ReceivedFeedbacks)
				.HasForeignKey(f => f.TargetUserId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}
