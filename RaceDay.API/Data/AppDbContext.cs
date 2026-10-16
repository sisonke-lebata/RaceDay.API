using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> o) : base(o) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<EventCategory> EventCategories => Set<EventCategory>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<WeatherLog> WeatherLogs => Set<WeatherLog>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<User>().HasIndex(u => u.Email).IsUnique();
        mb.Entity<UserProfile>().HasOne(p => p.User).WithOne(u => u.Profile)
            .HasForeignKey<UserProfile>(p => p.UserID).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<UserSession>().HasIndex(s => s.Token).IsUnique();
        mb.Entity<Event>().HasOne(e => e.Organiser).WithMany(u => u.OrganisedEvents)
            .HasForeignKey(e => e.OrganiserID).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<EventCategory>().HasIndex(ec => new { ec.EventID, ec.CategoryID }).IsUnique();
        mb.Entity<Enrolment>().HasIndex(en => new { en.UserID, en.EventCategoryID }).IsUnique();
        mb.Entity<Result>().HasOne(r => r.Enrolment).WithOne(e => e.Result)
            .HasForeignKey<Result>(r => r.EnrolmentID).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<Category>().Property(c => c.Distance).HasPrecision(5, 2);
        mb.Entity<EventCategory>().Property(ec => ec.Price).HasPrecision(10, 2);
    }

    public static void Seed(AppDbContext db)
    {
        if (db.Users.Any()) return;

        db.Users.AddRange(
            new User
            {
                Email = "thabo@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123!"),
                FirstName = "Thabo",
                LastName = "Mokoena",
                Role = "Participant"
            },
            new User
            {
                Email = "sarah@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123!"),
                FirstName = "Sarah",
                LastName = "Van Der Merwe",
                Role = "Participant"
            },
            new User
            {
                Email = "organiser@raceday.co.za",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123!"),
                FirstName = "RaceDay",
                LastName = "Organiser",
                Role = "Organiser"
            });
        db.SaveChanges();

        db.Categories.AddRange(
            new Category { CategoryName = "5km Run", Distance = 5, AgeRestriction = "Open" },
            new Category { CategoryName = "10km Run", Distance = 10, AgeRestriction = "Open" },
            new Category { CategoryName = "21km Half", Distance = 21.1m, AgeRestriction = "Senior" },
            new Category { CategoryName = "42km Full", Distance = 42.2m, AgeRestriction = "Senior" },
            new Category { CategoryName = "56km Ultra", Distance = 56, AgeRestriction = "Senior" },
            new Category { CategoryName = "Cycle 100km", Distance = 100, AgeRestriction = "Open" });
        db.SaveChanges();

        db.Events.AddRange(
            new Event
            {
                OrganiserID = 3,
                EventName = "Comrades 2026",
                EventDate = new DateTime(2026, 6, 14),
                RegistrationDeadline = new DateTime(2026, 5, 30),
                Location = "Durban",
                MaxParticipants = 25000,
                EventType = "Run"
            },
            new Event
            {
                OrganiserID = 3,
                EventName = "Cape Town Cycle Tour 2026",
                EventDate = new DateTime(2026, 3, 8),
                RegistrationDeadline = new DateTime(2026, 2, 20),
                Location = "Cape Town",
                MaxParticipants = 35000,
                EventType = "Cycle"
            });
        db.SaveChanges();

        db.EventCategories.AddRange(
            new EventCategory { EventID = 1, CategoryID = 1, Price = 350, MaxParticipantsPerCategory = 5000 },
            new EventCategory { EventID = 1, CategoryID = 4, Price = 750, MaxParticipantsPerCategory = 10000 },
            new EventCategory { EventID = 2, CategoryID = 6, Price = 450, MaxParticipantsPerCategory = 20000 });
        db.SaveChanges();
    }
}