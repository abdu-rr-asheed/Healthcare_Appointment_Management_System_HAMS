using Microsoft.EntityFrameworkCore;
using HAMS.API.Data;
using HAMS.API.Models.Entities;
using System.Text.Json;

namespace HAMS.API.Extensions
{
    public static class SeedDataExtensions
    {
        public static async Task SeedDataAsync(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<ApplicationDbContext>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            try
            {
                await context.Database.EnsureCreatedAsync();

                if (!context.Departments.Any())
                {
                    logger.LogInformation("Seeding departments...");

                    var departments = new[]
                    {
                        new Department
                        {
                            Id = Guid.NewGuid(),
                            Name = "General Medicine",
                            Description = "General medical consultations and check-ups",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        },
                        new Department
                        {
                            Id = Guid.NewGuid(),
                            Name = "Cardiology",
                            Description = "Heart and cardiovascular system specialists",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        },
                        new Department
                        {
                            Id = Guid.NewGuid(),
                            Name = "Orthopedics",
                            Description = "Musculoskeletal system and orthopedic surgery",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        },
                        new Department
                        {
                            Id = Guid.NewGuid(),
                            Name = "Pediatrics",
                            Description = "Medical care for infants, children, and adolescents",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        },
                        new Department
                        {
                            Id = Guid.NewGuid(),
                            Name = "Dermatology",
                            Description = "Skin, hair, and nail conditions",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        }
                    };

                    await context.Departments.AddRangeAsync(departments);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Departments seeded successfully");
                }

                if (!context.Users.Any(u => u.Role == UserRole.Administrator))
                {
                    logger.LogInformation("Seeding admin user...");

                    var adminUser = new User
                    {
                        Id = Guid.NewGuid(),
                        NhsNumber = "ADMIN001",
                        Email = "admin@hams.example.com",
                        PhoneNumber = "+447000000001",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                        FirstName = "System",
                        LastName = "Administrator",
                        DateOfBirth = DateTime.SpecifyKind(new DateTime(1980, 1, 1), DateTimeKind.Utc),
                        TwoFactorEnabled = false,
                        IsActive = true,
                        Role = UserRole.Administrator,
                        CreatedAt = DateTime.UtcNow,
                        UserName = "admin"
                    };

                    await context.Users.AddAsync(adminUser);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Admin user seeded successfully");
                }

                if (!context.Users.Any(u => u.Role == UserRole.Clinician))
                {
                    logger.LogInformation("Seeding clinicians...");

                    var departments = await context.Departments.Where(d => d.IsActive).ToListAsync();

                    var users = new[]
                    {
                        new User
                        {
                            Id = Guid.NewGuid(),
                            NhsNumber = "DOC001",
                            Email = "dr.smith@hams.example.com",
                            PhoneNumber = "+447000000002",
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Doctor123!"),
                            FirstName = "John",
                            LastName = "Smith",
                            DateOfBirth = DateTime.SpecifyKind(new DateTime(1975, 5, 15), DateTimeKind.Utc),
                            TwoFactorEnabled = false,
                            IsActive = true,
                            Role = UserRole.Clinician,
                            CreatedAt = DateTime.UtcNow,
                            UserName = "dr.smith"
                        },
                        new User
                        {
                            Id = Guid.NewGuid(),
                            NhsNumber = "DOC002",
                            Email = "dr.jones@hams.example.com",
                            PhoneNumber = "+447000000003",
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Doctor123!"),
                            FirstName = "Sarah",
                            LastName = "Jones",
                            DateOfBirth = DateTime.SpecifyKind(new DateTime(1980, 8, 22), DateTimeKind.Utc),
                            TwoFactorEnabled = false,
                            IsActive = true,
                            Role = UserRole.Clinician,
                            CreatedAt = DateTime.UtcNow,
                            UserName = "dr.jones"
                        },
                        new User
                        {
                            Id = Guid.NewGuid(),
                            NhsNumber = "DOC003",
                            Email = "dr.brown@hams.example.com",
                            PhoneNumber = "+447000000004",
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Doctor123!"),
                            FirstName = "Michael",
                            LastName = "Brown",
                            DateOfBirth = DateTime.SpecifyKind(new DateTime(1978, 3, 10), DateTimeKind.Utc),
                            TwoFactorEnabled = false,
                            IsActive = true,
                            Role = UserRole.Clinician,
                            CreatedAt = DateTime.UtcNow,
                            UserName = "dr.brown"
                        }
                    };

                    await context.Users.AddRangeAsync(users);
                    await context.SaveChangesAsync();

                    var clinicians = new List<Clinician>
                    {
                        new Clinician
                        {
                            Id = Guid.NewGuid(),
                            UserId = users[0].Id,
                            ClinicianId = Guid.NewGuid(),
                            DepartmentId = departments[0].Id,
                            Specialty = "General Practitioner",
                            LicenseNumber = "GMC123456",
                            JobTitle = "General Practitioner",
                            GmcNumber = "GMC123456",
                            Qualifications = new List<string> { "MBBS", "MRCP", "MRCGP" },
                            Status = ClinicianStatus.Active,
                            StartDate = DateTime.UtcNow.AddYears(-5),
                            CreatedAt = DateTime.UtcNow
                        },
                        new Clinician
                        {
                            Id = Guid.NewGuid(),
                            UserId = users[1].Id,
                            ClinicianId = Guid.NewGuid(),
                            DepartmentId = departments[1].Id,
                            Specialty = "Cardiologist",
                            LicenseNumber = "GMC234567",
                            JobTitle = "Consultant Cardiologist",
                            GmcNumber = "GMC234567",
                            Qualifications = new List<string> { "MBBS", "MRCP", "PhD Cardiology" },
                            Status = ClinicianStatus.Active,
                            StartDate = DateTime.UtcNow.AddYears(-3),
                            CreatedAt = DateTime.UtcNow
                        },
                        new Clinician
                        {
                            Id = Guid.NewGuid(),
                            UserId = users[2].Id,
                            ClinicianId = Guid.NewGuid(),
                            DepartmentId = departments[2].Id,
                            Specialty = "Orthopedic Surgeon",
                            LicenseNumber = "GMC345678",
                            JobTitle = "Consultant Orthopedic Surgeon",
                            GmcNumber = "GMC345678",
                            Qualifications = new List<string> { "MBBS", "FRCS", "MSc Orthopedics" },
                            Status = ClinicianStatus.Active,
                            StartDate = DateTime.UtcNow.AddYears(-4),
                            CreatedAt = DateTime.UtcNow
                        }
                    };

                    await context.Clinicians.AddRangeAsync(clinicians);
                    await context.SaveChangesAsync();
                    
                    logger.LogInformation("Clinicians seeded successfully");
                }

                if (!context.Users.Any(u => u.Role == UserRole.Patient))
                {
                    logger.LogInformation("Seeding patients...");

                    var patientUsers = new List<User>
                    {
                        new User
                        {
                            Id = Guid.NewGuid(),
                            NhsNumber = "PATIENT001",
                            Email = "jane.doe@example.com",
                            PhoneNumber = "+447000000005",
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Patient123!"),
                            FirstName = "Jane",
                            LastName = "Doe",
                            DateOfBirth = DateTime.SpecifyKind(new DateTime(1985, 3, 15), DateTimeKind.Utc),
                            TwoFactorEnabled = false,
                            IsActive = true,
                            Role = UserRole.Patient,
                            CreatedAt = DateTime.UtcNow,
                            UserName = "jane.doe"
                        },
                        new User
                        {
                            Id = Guid.NewGuid(),
                            NhsNumber = "PATIENT002",
                            Email = "john.smith@example.com",
                            PhoneNumber = "+447000000006",
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Patient456!"),
                            FirstName = "John",
                            LastName = "Smith",
                            DateOfBirth = DateTime.SpecifyKind(new DateTime(1990, 7, 22), DateTimeKind.Utc),
                            TwoFactorEnabled = true,
                            IsActive = true,
                            Role = UserRole.Patient,
                            CreatedAt = DateTime.UtcNow,
                            UserName = "john.smith"
                        },
                        new User
                        {
                            Id = Guid.NewGuid(),
                            NhsNumber = "PATIENT003",
                            Email = "robert.johnson@example.com",
                            PhoneNumber = "+447000000007",
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Patient789!"),
                            FirstName = "Robert",
                            LastName = "Johnson",
                            DateOfBirth = DateTime.SpecifyKind(new DateTime(1978, 11, 30), DateTimeKind.Utc),
                            TwoFactorEnabled = false,
                            IsActive = true,
                            Role = UserRole.Patient,
                            CreatedAt = DateTime.UtcNow,
                            UserName = "robert.johnson"
                        }
                    };

                    var patients = new List<Patient>
                    {
                        new Patient
                        {
                            Id = Guid.NewGuid(),
                            UserId = patientUsers[0].Id,
                            Address = "123 Main Street",
                            City = "London",
                            Postcode = "SW1A 1AA",
                            SmsOptIn = true,
                            EmergencyContactName = "John Doe",
                            EmergencyContactPhone = "+447000000008",
                            CreatedAt = DateTime.UtcNow
                        },
                        new Patient
                        {
                            Id = Guid.NewGuid(),
                            UserId = patientUsers[1].Id,
                            Address = "45 Oak Avenue",
                            City = "Manchester",
                            Postcode = "M1 1AA",
                            SmsOptIn = true,
                            EmergencyContactName = "Jane Smith",
                            EmergencyContactPhone = "+447000000009",
                            CreatedAt = DateTime.UtcNow
                        },
                        new Patient
                        {
                            Id = Guid.NewGuid(),
                            UserId = patientUsers[2].Id,
                            Address = "78 High Street",
                            City = "Birmingham",
                            Postcode = "B1 2AA",
                            SmsOptIn = false,
                            EmergencyContactName = "Robert Johnson",
                            EmergencyContactPhone = "+447000000010",
                            CreatedAt = DateTime.UtcNow
                        }
                    };

                    await context.Users.AddRangeAsync(patientUsers);
                    await context.SaveChangesAsync();
                    await context.Patients.AddRangeAsync(patients);
                    await context.SaveChangesAsync();

                    logger.LogInformation("Patients seeded successfully");
                }

                if (!context.AvailabilitySlots.Any())
                {
                    logger.LogInformation("Seeding availability slots...");

                    var clinicians = await context.Clinicians
                        .Include(c => c.User)
                        .Include(c => c.Department)
                        .ToListAsync();

                    var startDate = DateTime.UtcNow.Date.AddDays(1);
                    var endDate = DateTime.UtcNow.Date.AddDays(14);

                    foreach (var clinician in clinicians)
                    {
                        var currentDate = startDate;

                        while (currentDate <= endDate)
                        {
                            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                            {
                                for (int hour = 9; hour <= 16; hour++)
                                {
                                    var slot = new AvailabilitySlot
                                    {
                                        Id = Guid.NewGuid(),
                                        ClinicianId = clinician.Id,
                                        DepartmentId = clinician.DepartmentId,
                                        StartDateTime = DateTime.SpecifyKind(currentDate.AddHours(hour), DateTimeKind.Utc),
                                        EndDateTime = DateTime.SpecifyKind(currentDate.AddHours(hour + 1), DateTimeKind.Utc),
                                        IsAvailable = true,
                                        IsCancelled = false,
                                        CreatedAt = DateTime.UtcNow
                                    };

                                    await context.AvailabilitySlots.AddAsync(slot);
                                }
                            }

                            currentDate = currentDate.AddDays(1);
                        }
                    }

                    await context.SaveChangesAsync();
                    logger.LogInformation("Availability slots seeded successfully");
                }

                logger.LogInformation("Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database");
                throw;
            }
        }
    }
}