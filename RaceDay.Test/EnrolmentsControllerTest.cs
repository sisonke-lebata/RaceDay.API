using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Controllers;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.Test.Helpers;

namespace RaceDay.API;

public class EnrolmentsControllerTests
{
    private static EnrolmentsController MakeController(
        RaceDay.API.Data.AppDbContext db, int userId)
    {
        var ctrl = new EnrolmentsController(db);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim("uid", userId.ToString()),
                    new Claim(ClaimTypes.Role, "Participant")
                }))
            }
        };
        return ctrl;
    }

    [Fact]
    public async Task Enrol_ValidEvent_Returns201()
    {
        var db = TestDbFactory.Create();

        var ev = new Event
        {
            OrganiserID = 99,
            EventName = "Test Event",
            EventDate = DateTime.UtcNow.AddDays(10),
            RegistrationDeadline = DateTime.UtcNow.AddDays(5),
            Location = "Cape Town",
            MaxParticipants = 100,
            EventType = "Run"
        };
        db.Events.Add(ev);

        var cat = new Category { CategoryName = "5km", Distance = 5 };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        db.EventCategories.Add(new EventCategory
        {
            EventID = ev.EventID,
            CategoryID = cat.CategoryID,
            Price = 100,
            MaxParticipantsPerCategory = 50
        });
        await db.SaveChangesAsync();

        var ctrl = MakeController(db, 1);
        var result = await ctrl.Enrol(ev.EventID, new EnrolmentCreateDto(cat.CategoryID));

        (result as ObjectResult)!.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Enrol_NonExistentEvent_Returns404()
    {
        var ctrl = MakeController(TestDbFactory.Create(), 1);
        var result = await ctrl.Enrol(9999, new EnrolmentCreateDto(1));
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}