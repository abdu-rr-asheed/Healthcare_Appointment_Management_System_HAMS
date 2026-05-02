using Microsoft.EntityFrameworkCore;
using HAMS.API.Models.Entities;

namespace HAMS.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Clinician> Clinicians { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AvailabilitySlot> AvailabilitySlots { get; set; }
        public DbSet<ClinicalNote> ClinicalNotes { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<RegularSchedule> RegularSchedules { get; set; }
        public DbSet<LeavePeriod> LeavePeriods { get; set; }
        public DbSet<SlotConfiguration> SlotConfigurations { get; set; }
        public DbSet<AppointmentSlot> AppointmentSlots { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.NhsNumber)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Appointment>()
                .HasIndex(a => new { a.SlotId, a.Status })
                .IsUnique()
                .HasFilter("\"Status\" != 2");

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.Timestamp);

            modelBuilder.Entity<AvailabilitySlot>()
                .HasIndex(s => new { s.ClinicianId, s.StartDateTime, s.EndDateTime });

            modelBuilder.Entity<Patient>()
                .Property(p => p.Metadata)
                .HasColumnType("jsonb");

            modelBuilder.Entity<Clinician>()
                .Property(c => c.Qualifications)
                .HasColumnType("jsonb");

            modelBuilder.Entity<AuditLog>()
                .Property(a => a.Details)
                .HasColumnType("jsonb");

            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            modelBuilder.Entity<RegularSchedule>()
                .HasIndex(s => s.ClinicianId);

            modelBuilder.Entity<LeavePeriod>()
                .HasIndex(l => l.ClinicianId);

            modelBuilder.Entity<SlotConfiguration>()
                .HasIndex(sc => sc.ClinicianId);

            modelBuilder.Entity<AppointmentSlot>()
                .HasIndex(aps => new { aps.ClinicianId, aps.StartDateTime, aps.EndDateTime });

            modelBuilder.Entity<Patient>()
                .Property(p => p.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            modelBuilder.Entity<Clinician>()
                .Property(c => c.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            modelBuilder.Entity<Department>()
                .Property(d => d.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            modelBuilder.Entity<Appointment>()
                .Property(a => a.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            modelBuilder.Entity<AvailabilitySlot>()
                .Property(a => a.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            modelBuilder.Entity<ClinicalNote>()
                .Property(c => c.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            modelBuilder.Entity<Notification>()
                .Property(n => n.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            modelBuilder.Entity<User>()
                .HasOne(u => u.Patient)
                .WithOne(p => p.User)
                .HasForeignKey<Patient>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Clinician)
                .WithOne(c => c.User)
                .HasForeignKey<Clinician>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(r => r.Token)
                .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RefreshToken>()
                .Property(r => r.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");
        }
    }
}