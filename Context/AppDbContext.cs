using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TutorLinkBe.Models;

namespace TutorLinkBe.Context;

    public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Classroom> Classrooms { get; set; }
        public DbSet<ClassroomStudent> ClassroomStudents { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<LessonView> LessonViews { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<QuizOption> QuizOptions { get; set; }
        public DbSet<QuizSubmission> QuizSubmissions { get; set; }
        public DbSet<QuizAnswer> QuizAnswers { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<TutorRequest> TutorRequests { get; set; }
        public DbSet<RoleHistory> RoleHistories { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Identity setup

            modelBuilder.Entity<LessonView>()
                .HasIndex(lv => new { lv.LessonId, lv.StudentId })
                .IsUnique();

            modelBuilder.Entity<ClassroomStudent>()
                .HasIndex(cs => new { cs.ClassroomId, cs.StudentId })
                .IsUnique();

            modelBuilder.Entity<Classroom>()
                .HasOne(c => c.Tutor)
                .WithMany(u => u.Classrooms)
                .HasForeignKey(c => c.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Course>()
                .HasOne(c => c.Classroom)
                .WithMany(cl => cl.Courses)
                .HasForeignKey(c => c.ClassroomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Course)
                .WithMany(c => c.Lessons)
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LessonView>()
                .HasOne(lv => lv.Lesson)
                .WithMany(l => l.LessonViews)
                .HasForeignKey(lv => lv.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LessonView>()
                .HasOne(lv => lv.Student)
                .WithMany()
                .HasForeignKey(lv => lv.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.Lesson)
                .WithMany(l => l.Quizzes)
                .HasForeignKey(q => q.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizQuestion>()
                .HasOne(qq => qq.Quiz)
                .WithMany(q => q.QuizQuestions)
                .HasForeignKey(qq => qq.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizOption>()
                .HasOne(qo => qo.QuizQuestion)
                .WithMany(qq => qq.QuizOptions)
                .HasForeignKey(qo => qo.QuizQuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizSubmission>()
                .HasOne(qs => qs.Quiz)
                .WithMany(q => q.QuizSubmissions)
                .HasForeignKey(qs => qs.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizSubmission>()
                .HasOne(qs => qs.Student)
                .WithMany(u => u.QuizSubmissions)
                .HasForeignKey(qs => qs.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizAnswer>()
                .HasOne(qa => qa.QuizSubmission)
                .WithMany(qs => qs.QuizAnswers)
                .HasForeignKey(qa => qa.QuizSubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizAnswer>()
                .HasOne(qa => qa.QuizQuestion)
                .WithMany(qq => qq.QuizAnswers)
                .HasForeignKey(qa => qa.QuizQuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<QuizAnswer>()
                .HasOne(qa => qa.SelectedOption)
                .WithMany()
                .HasForeignKey(qa => qa.SelectedOptionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClassroomStudent>()
                .HasOne(cs => cs.Classroom)
                .WithMany(c => c.ClassroomStudents)
                .HasForeignKey(cs => cs.ClassroomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClassroomStudent>()
                .HasOne(cs => cs.Student)
                .WithMany(u => u.ClassroomStudents)
                .HasForeignKey(cs => cs.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure RefreshToken entity explicitly
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(rt => rt.Id);
                entity.Property(rt => rt.Token).IsRequired();
                entity.Property(rt => rt.UserId).IsRequired();
                entity.HasIndex(rt => rt.Token);
            });
            modelBuilder.Entity<TutorRequest>(entity =>
            {
                entity.HasKey(tr => tr.Id);
                entity.Property(tr => tr.UserId).IsRequired();

                entity.HasOne(tr => tr.User)
                    .WithMany(u => u.TutorRequests)
                    .HasForeignKey(tr => tr.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(tr => tr.Status)
                    .HasConversion<string>();
            });
            modelBuilder.Entity<RoleHistory>(entity =>
            {
                entity.HasKey(rh => rh.Id);
                entity.Property(rh => rh.UserId).IsRequired();

                entity.HasOne(rh => rh.User)
                    .WithMany(u => u.RoleHistories)
                    .HasForeignKey(rh => rh.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rh => rh.ChangedByUser)
                    .WithMany()
                    .HasForeignKey(rh => rh.ChangedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
}