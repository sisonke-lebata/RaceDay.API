using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Controllers;
using RaceDay.API.DTOs;
using RaceDay.Test.Helpers;

namespace RaceDay.Test;

public class EventsControllerTests
{
    private static EventsController MakeController(int userId, string role)
    {
        var ctrl = new EventsController(TestDbFactory.Create());
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim("uid", userId.ToString()),
                    new Claim(ClaimTypes.Role, role)
                }))
            }
        };
        return ctrl;
    }

    [Fact]
    public async Task Create_ValidRunEvent_Returns201()
    {
        var ctrl = MakeController(1, "Organiser");
        var result = await ctrl.Create(new EventCreateDto(
            "Test Run", "Desc",
            DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(20),
            "Cape Town", 100, "Run"));

        (result as ObjectResult)!.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Create_InvalidEventType_Returns400()
    {
        var ctrl = MakeController(1, "Organiser");
        var result = await ctrl.Create(new EventCreateDto(
            "Bad", null, DateTime.UtcNow, DateTime.UtcNow, "X", 10, "Swim"));

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Get_NonExistentEvent_Returns404()
    {
        var ctrl = MakeController(1, "Organiser");
        var result = await ctrl.Get(9999);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var ctrl = MakeController(1, "Organiser");
        var result = await ctrl.GetAll(null, 1, 20);
        result.Should().BeOfType<OkObjectResult>();
    }
}