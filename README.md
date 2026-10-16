# RaceDay.API
# RaceDay API

![RaceDay CI](https://github.com/YOUR_USERNAME/RaceDay/actions/workflows/dotnet.yml/badge.svg)

A RESTful API for the RaceDay Event Management System, built with ASP.NET Core 8 Web API and Entity Framework Core (Code-First).

  Overview

The RaceDay API powers a race event management platform where organisers create events and participants enrol in race categories. It handles authentication, event management, enrolments, results, and provides role-based access control for two user types.

  Roles

| Role | Permissions |
|---|---|
| Public (no auth) | View events, categories, results, weather |
| Participant| Register/login, view/edit own profile, enrol in events, view own results |
| Organiser| All Participant permissions + create/update/delete events, manage categories, capture results, view all enrolments |

  Tech Stack

- Framework: ASP.NET Core 8 Web API
- Database:SQL Server (LocalDB for development)
- FORM: Entity Framework Core 8 (Code-First)
- Authentication: JWT Bearer tokens with server-side session revocation
- Password hashing: BCrypt
- API documentation: Swagger (Swashbuckle)
- Testing:xUnit, FluentAssertions, EF Core InMemory
- CI/CD: GitHub Actions

  Project Structure
  
  Getting Started

Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (or VS Code)
- SQL Server LocalDB (comes with Visual Studio)

 Setup Steps

Clone the repository

   ```bash
   git clone https://github.com/YOUR_USERNAME/RaceDay.git
   cd RaceDay
