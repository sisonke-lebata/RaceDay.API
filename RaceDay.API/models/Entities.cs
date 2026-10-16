using
    System.ComponentModel.DataAnnotations;
namespace RaceDay.API.Models;

public class User
{
    [Key]
    public int UserID { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Role { get; set; } = "Participant";
    public bool IsActive { get; set; } = true;
    public UserProfile? Profile { get; set; }
    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}

public class UserProfile
{
    [Key]
    public int ProfileID { get; set; }
    public int UserID { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Bio { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public User User { get; set; } = null!;
}

public class UserSession
{
    [Key]
    public int SessionID { get; set; }
    public int UserID { get; set; }
    public string Token { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public User User { get; set; } = null!;
}

public class Event
{
    [Key]
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string EventName { get; set; } = "";
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public string Location { get; set; } = "";
    public int MaxParticipants { get; set; }
    public int CurrentParticipants { get; set; }
    public string EventType { get; set; } = "Run";
    public bool IsActive { get; set; } = true;
    public User Organiser { get; set; } = null!;
    public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
}

public class Category
{
    [Key]
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal Distance { get; set; }
    public string? AgeRestriction { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
}

public class EventCategory
{
    [Key]
    public int EventCategoryID { get; set; }
    public int EventID { get; set; }
    public int CategoryID { get; set; }
    public decimal Price { get; set; }
    public int MaxParticipantsPerCategory { get; set; }
    public int CurrentParticipants { get; set; }
    public Event Event { get; set; } = null!;
    public Category Category { get; set; } = null!;
}

public class Enrolment
{
    [Key]
    public int EnrolmentID { get; set; }
    public int UserID { get; set; }
    public int EventCategoryID { get; set; }
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public string PaymentStatus { get; set; } = "Pending";
    public bool IsActive { get; set; } = true;
    public User User { get; set; } = null!;
    public EventCategory EventCategory { get; set; } = null!;
    public Result? Result { get; set; }
}

public class Result
{
    [Key]
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public string Status { get; set; } = "Registered";
    public Enrolment Enrolment { get; set; } = null!;
}

public class WeatherLog
{
    [Key]
    public int WeatherLogID { get; set; }
    public int EventID { get; set; }
    public decimal? Temperature { get; set; }
    public string? Conditions { get; set; }
    public Event Event { get; set; } = null!;
}
