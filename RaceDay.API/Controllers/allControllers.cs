using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.DTOs
{
    public record RegisterDto(string Email, string Password, string FirstName, string LastName,
        string Role, string? PhoneNumber, DateTime? DateOfBirth);
    public record LoginDto(string Email, string Password);
    public record UserDto(int UserID, string Email, string FirstName, string LastName,
        string Role, string? PhoneNumber, DateTime? DateOfBirth, string? Bio);
    public record UpdateProfileDto(string FirstName, string LastName, string? PhoneNumber,
        DateTime? DateOfBirth, string? Bio);
    public record ChangePasswordDto(string CurrentPassword, string NewPassword);
    public record EventCreateDto(string EventName, string? Description, DateTime EventDate,
        DateTime RegistrationDeadline, string Location, int MaxParticipants, string EventType);
    public record CategoryCreateDto(string CategoryName, decimal Distance, string? AgeRestriction);
    public record EnrolmentCreateDto(int CategoryID);
    public record ResultCreateDto(TimeSpan? FinishTime, string Status);
}

namespace RaceDay.API.Controllers
{
    using RaceDay.API.DTOs;

    [ApiController]
    [Route("api/authorize")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IJwtService _jwt;
        public AuthController(AppDbContext db, IJwtService jwt) { _db = db; _jwt = jwt; }

        /// <summary>Register a new Participant or Organiser.</summary>
        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (dto.Role != "Organiser" && dto.Role != "Participant")
                return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "Role must be Organiser or Participant" } });
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                return Conflict(new { error = new { code = "DUPLICATE_EMAIL", message = "Email already exists" } });

            var user = new User
            {
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = dto.Role
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            _db.UserProfiles.Add(new UserProfile
            {
                UserID = user.UserID,
                PhoneNumber = dto.PhoneNumber,
                DateOfBirth = dto.DateOfBirth
            });

            var token = _jwt.GenerateToken(user);
            _db.UserSessions.Add(new UserSession
            {
                UserID = user.UserID,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(8)
            });
            await _db.SaveChangesAsync();

            return StatusCode(201, new { token, user = new { user.UserID, user.Email, user.Role } });
        }

        /// <summary>Login and receive JWT.</summary>
        [HttpPost("login"), AllowAnonymous]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _db.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { error = new { code = "INVALID_CREDENTIALS", message = "Invalid credentials" } });

            var token = _jwt.GenerateToken(user);
            _db.UserSessions.Add(new UserSession
            {
                UserID = user.UserID,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(8)
            });
            await _db.SaveChangesAsync();

            return Ok(new
            {
                token,
                user = new UserDto(user.UserID, user.Email, user.FirstName,
                user.LastName, user.Role, user.Profile?.PhoneNumber, user.Profile?.DateOfBirth, user.Profile?.Bio)
            });
        }

        /// <summary>Get current authenticated user.</summary>
        [HttpGet("me"), Authorize]
        public async Task<IActionResult> Me()
        {
            var uid = int.Parse(User.FindFirstValue("uid")!);
            var u = await _db.Users.Include(x => x.Profile).FirstOrDefaultAsync(x => x.UserID == uid);
            return u == null ? NotFound() : Ok(new UserDto(u.UserID, u.Email, u.FirstName,
                u.LastName, u.Role, u.Profile?.PhoneNumber, u.Profile?.DateOfBirth, u.Profile?.Bio));
        }

        /// <summary>Revoke current session.</summary>
        [HttpPost("logout"), Authorize]
        public async Task<IActionResult> Logout()
        {
            var auth = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var s = await _db.UserSessions.FirstOrDefaultAsync(x => x.Token == auth);
            if (s != null) { s.IsRevoked = true; await _db.SaveChangesAsync(); }
            return Ok(new { message = "Logged out successfully" });
        }
    }

    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        public UsersController(AppDbContext db) => _db = db;
        private int Uid => int.Parse(User.FindFirstValue("uid")!);

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var u = await _db.Users.Include(x => x.Profile).FirstOrDefaultAsync(x => x.UserID == id);
            if (u == null) return NotFound();
            return Ok(new
            {
                u.UserID,
                u.Email,
                u.FirstName,
                u.LastName,
                u.Role,
                PhoneNumber = u.Profile?.PhoneNumber,
                DateOfBirth = u.Profile?.DateOfBirth,
                Bio = id == Uid ? u.Profile?.Bio : null
            });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateProfileDto dto)
        {
            if (id != Uid) return Forbid();
            var u = await _db.Users.Include(x => x.Profile).FirstOrDefaultAsync(x => x.UserID == id);
            if (u == null) return NotFound();
            u.FirstName = dto.FirstName; u.LastName = dto.LastName;
            u.Profile ??= new UserProfile { UserID = id };
            u.Profile.PhoneNumber = dto.PhoneNumber;
            u.Profile.DateOfBirth = dto.DateOfBirth;
            u.Profile.Bio = dto.Bio;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Profile updated" });
        }

        [HttpPut("{id:int}/password")]
        public async Task<IActionResult> ChangePassword(int id, ChangePasswordDto dto)
        {
            if (id != Uid) return Forbid();
            var u = await _db.Users.FindAsync(id);
            if (u == null) return NotFound();
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, u.PasswordHash))
                return BadRequest(new { error = new { code = "INVALID_PASSWORD", message = "Current password incorrect" } });
            u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Password updated" });
        }

        [HttpGet("{id:int}/enrolment")]
        public async Task<IActionResult> Enrolments(int id) =>
            Ok(await _db.Enrolments.Where(e => e.UserID == id && e.IsActive)
                .Select(e => new {
                    e.EnrolmentID,
                    e.EventCategory.Event.EventName,
                    e.EventCategory.Category.CategoryName,
                    e.PaymentStatus
                }).ToListAsync());

        [HttpGet("{id:int}/results")]
        public async Task<IActionResult> Results(int id) =>
            Ok(await _db.Results.Where(r => r.Enrolment.UserID == id)
                .Select(r => new { r.ResultID, r.FinishTime, r.OverallPosition, r.Status }).ToListAsync());
    }

    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public EventsController(AppDbContext db) => _db = db;
        private int Uid => int.Parse(User.FindFirstValue("uid") ?? "0");

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] string? location,
            [FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            var q = _db.Events.Include(e => e.Organiser).Where(e => e.IsActive);
            if (!string.IsNullOrEmpty(location)) q = q.Where(e => e.Location.Contains(location));
            return Ok(await q.OrderBy(e => e.EventDate).Skip((page - 1) * limit).Take(limit).ToListAsync());
        }

        [HttpGet("{id:int}"), AllowAnonymous]
        public async Task<IActionResult> Get(int id)
        {
            var e = await _db.Events.Include(x => x.Organiser).FirstOrDefaultAsync(x => x.EventID == id);
            return e == null ? NotFound() : Ok(e);
        }

        [HttpGet("upcoming"), AllowAnonymous]
        public async Task<IActionResult> Upcoming() =>
            Ok(await _db.Events.Where(e => e.EventDate > DateTime.UtcNow && e.IsActive).ToListAsync());

        [HttpGet("my-events"), Authorize(Roles = "Organiser")]
        public async Task<IActionResult> MyEvents() =>
            Ok(await _db.Events.Where(e => e.OrganiserID == Uid).ToListAsync());

        [HttpPost, Authorize(Roles = "Organiser")]
        public async Task<IActionResult> Create(EventCreateDto dto)
        {
            if (!new[] { "Run", "Walk", "Cycle" }.Contains(dto.EventType))
                return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "EventType must be Run, Walk or Cycle" } });
            var e = new Event
            {
                OrganiserID = Uid,
                EventName = dto.EventName,
                Description = dto.Description,
                EventDate = dto.EventDate,
                RegistrationDeadline = dto.RegistrationDeadline,
                Location = dto.Location,
                MaxParticipants = dto.MaxParticipants,
                EventType = dto.EventType
            };
            _db.Events.Add(e);
            await _db.SaveChangesAsync();
            return StatusCode(201, e);
        }

        [HttpPut("{id:int}"), Authorize(Roles = "Organiser")]
        public async Task<IActionResult> Update(int id, EventCreateDto dto)
        {
            var e = await _db.Events.FindAsync(id);
            if (e == null) return NotFound();
            if (e.OrganiserID != Uid) return Forbid();
            e.EventName = dto.EventName; e.Description = dto.Description;
            e.EventDate = dto.EventDate; e.RegistrationDeadline = dto.RegistrationDeadline;
            e.Location = dto.Location; e.MaxParticipants = dto.MaxParticipants;
            e.EventType = dto.EventType;
            await _db.SaveChangesAsync();
            return Ok(e);
        }

        [HttpDelete("{id:int}"), Authorize(Roles = "Organiser")]
        public async Task<IActionResult> Delete(int id)
        {
            var e = await _db.Events.FindAsync(id);
            if (e == null) return NotFound();
            if (e.OrganiserID != Uid) return Forbid();
            e.IsActive = false;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Event cancelled" });
        }
    }

    [ApiController]
    [Route("api")]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public CategoriesController(AppDbContext db) => _db = db;

        [HttpGet("events/{eventId:int}/categories"), AllowAnonymous]
        public async Task<IActionResult> ForEvent(int eventId) =>
            Ok(await _db.EventCategories.Where(ec => ec.EventID == eventId)
                .Select(ec => new {
                    ec.EventCategoryID,
                    ec.Category.CategoryName,
                    ec.Category.Distance,
                    ec.Category.AgeRestriction,
                    ec.Price,
                    ec.MaxParticipantsPerCategory,
                    ec.CurrentParticipants
                }).ToListAsync());

        [HttpGet("categories"), AllowAnonymous]
        public async Task<IActionResult> All() =>
            Ok(await _db.Categories.Where(c => c.IsActive).ToListAsync());

        [HttpPost("categories"), Authorize(Roles = "Organiser")]
        public async Task<IActionResult> Create(CategoryCreateDto dto)
        {
            var c = new Category
            {
                CategoryName = dto.CategoryName,
                Distance = dto.Distance,
                AgeRestriction = dto.AgeRestriction
            };
            _db.Categories.Add(c);
            await _db.SaveChangesAsync();
            return StatusCode(201, c);
        }
    }

    [ApiController]
    [Route("api")]
    public class EnrolmentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public EnrolmentsController(AppDbContext db) => _db = db;
        private int Uid => int.Parse(User.FindFirstValue("uid")!);

        [HttpPost("events/{eventId:int}/enrol"), Authorize]
        public async Task<IActionResult> Enrol(int eventId, EnrolmentCreateDto dto)
        {
            var ec = await _db.EventCategories.Include(x => x.Event).Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.EventID == eventId && x.CategoryID == dto.CategoryID);
            if (ec == null) return NotFound(new { error = new { code = "NOT_FOUND", message = "Event or category not found" } });
            if (ec.CurrentParticipants >= ec.MaxParticipantsPerCategory)
                return BadRequest(new { error = new { code = "CATEGORY_FULL", message = "Category is full" } });
            if (await _db.Enrolments.AnyAsync(en => en.UserID == Uid && en.EventCategoryID == ec.EventCategoryID))
                return Conflict(new { error = new { code = "ALREADY_ENROLLED", message = "Already enrolled" } });

            var en = new Enrolment { UserID = Uid, EventCategoryID = ec.EventCategoryID };
            _db.Enrolments.Add(en);
            ec.CurrentParticipants++;
            ec.Event.CurrentParticipants++;
            await _db.SaveChangesAsync();
            return StatusCode(201, en);
        }

        [HttpGet("me/enrolments"), Authorize]
        public async Task<IActionResult> Mine() =>
            Ok(await _db.Enrolments.Where(e => e.UserID == Uid && e.IsActive).ToListAsync());

        [HttpDelete("enrolments/{id:int}"), Authorize]
        public async Task<IActionResult> Cancel(int id)
        {
            var en = await _db.Enrolments.FindAsync(id);
            if (en == null) return NotFound();
            if (en.UserID != Uid) return Forbid();
            en.IsActive = false;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Enrolment cancelled" });
        }
    }

    [ApiController]
    [Route("api")]
    public class ResultsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ResultsController(AppDbContext db) => _db = db;
        private int Uid => int.Parse(User.FindFirstValue("uid")!);

        [HttpPost("enrolments/{enrolmentId:int}/results"), Authorize(Roles = "Organiser")]
        public async Task<IActionResult> Create(int enrolmentId, ResultCreateDto dto)
        {
            var en = await _db.Enrolments.Include(e => e.EventCategory).ThenInclude(ec => ec.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentID == enrolmentId);
            if (en == null) return NotFound();
            if (en.EventCategory.Event.OrganiserID != Uid) return Forbid();
            if (await _db.Results.AnyAsync(r => r.EnrolmentID == enrolmentId))
                return Conflict(new { error = new { code = "RESULT_EXISTS", message = "Result already exists" } });

            var r = new Result { EnrolmentID = enrolmentId, FinishTime = dto.FinishTime, Status = dto.Status };
            _db.Results.Add(r);
            await _db.SaveChangesAsync();
            return StatusCode(201, r);
        }

        [HttpGet("results/{id:int}"), AllowAnonymous]
        public async Task<IActionResult> Get(int id)
        {
            var r = await _db.Results.Include(x => x.Enrolment).FirstOrDefaultAsync(x => x.ResultID == id);
            return r == null ? NotFound() : Ok(r);
        }

        [HttpGet("events/{eventId:int}/results"), AllowAnonymous]
        public async Task<IActionResult> ForEvent(int eventId) =>
            Ok(await _db.Results.Include(r => r.Enrolment).ThenInclude(e => e.EventCategory)
                .Where(r => r.Enrolment.EventCategory.EventID == eventId)
                .OrderBy(r => r.OverallPosition).ToListAsync());

        [HttpGet("events/{eventId:int}/leaderboard"), AllowAnonymous]
        public async Task<IActionResult> Leaderboard(int eventId) =>
            Ok(await _db.Results.Include(r => r.Enrolment).ThenInclude(e => e.EventCategory)
                .Where(r => r.Enrolment.EventCategory.EventID == eventId && r.Status == "Finished")
                .OrderBy(r => r.FinishTime).ToListAsync());

        [HttpDelete("results/{id:int}"), Authorize(Roles = "Organiser")]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _db.Results.Include(x => x.Enrolment)
                .ThenInclude(e => e.EventCategory).ThenInclude(ec => ec.Event)
                .FirstOrDefaultAsync(x => x.ResultID == id);
            if (r == null) return NotFound();
            if (r.Enrolment.EventCategory.Event.OrganiserID != Uid) return Forbid();
            _db.Results.Remove(r);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Result voided" });
        }
    }
}