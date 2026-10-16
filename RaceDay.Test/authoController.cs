using Castle.Core.Configuration;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RaceDay.API.Controllers;
using RaceDay.API.DTOs;
using RaceDay.API.Services;
using RaceDay.Test.Helpers;

namespace RaceDay.Test;

public class AuthControllerTests
{
    private static Microsoft.Extensions.Configuration. IConfiguration Cfg() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Jwt:Key"] = "TestKeyForUnitTestingMustBeLongEnough1234567890",
            ["Jwt:Issuer"] = "Test",
            ["Jwt:Audience"] = "Test"
        }).Build();

    [Fact]
    public async Task Register_ValidParticipant_Returns201()
    {
        var ctrl = new AuthController(TestDbFactory.Create(), new JwtService(Cfg()));
        var result = await ctrl.Register(new RegisterDto(
            "test@test.com", "Pass123!", "Test", "User", "Participant", null, null));

        (result as ObjectResult)!.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var db = TestDbFactory.Create();
        var ctrl = new AuthController(db, new JwtService(Cfg()));

        await ctrl.Register(new RegisterDto("dup@test.com", "Pass123!",
            "A", "B", "Participant", null, null));
        var result = await ctrl.Register(new RegisterDto("dup@test.com", "Pass123!",
            "C", "D", "Participant", null, null));

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var ctrl = new AuthController(TestDbFactory.Create(), new JwtService(Cfg()));
        var result = await ctrl.Register(new RegisterDto(
            "bad@test.com", "Pass123!", "A", "B", "Admin", null, null));

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var db = TestDbFactory.Create();
        var ctrl = new AuthController(db, new JwtService(Cfg()));

        await ctrl.Register(new RegisterDto("a@b.com", "Pass123!",
            "A", "B", "Participant", null, null));

        var result = await ctrl.Login(new LoginDto("a@b.com", "Pass123!"));
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var db = TestDbFactory.Create();
        var ctrl = new AuthController(db, new JwtService(Cfg()));

        await ctrl.Register(new RegisterDto("a@b.com", "Correct1!",
            "A", "B", "Participant", null, null));

        var result = await ctrl.Login(new LoginDto("a@b.com", "Wrong1!"));
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }
}