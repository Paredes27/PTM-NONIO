# PTM-NONIO · Automated Tutoring & Mentoring Platform

Final-year project for the Project Lab in Computer Engineering (LPEI) at UTAD, 2025/2026.
Authors: João Gonçalves and Valentim Silva.

The university's tutoring and mentoring programme was coordinated by hand in spreadsheets, repeated for every enrolment phase. PTM-NONIO automates it end to end.

## Features
- Assignment algorithm by course and enrolment phase: mentors to faculty tutors, then students to mentors, balancing load and respecting each mentor's capacity
- Backtracking search to maximise overall student–mentor compatibility, plus a weighted mentor recommendation engine
- Session scheduling, feedback collection and aggregated metrics for coordinators
- REST API with JWT authentication and role-based access control
- Automated functional tests (`MentoriaAPI.Validation`)

## Stack
ASP.NET Core (.NET 10) · C# · Entity Framework Core · SQLite · JWT

## Running locally
```bash
cd MentoriaAPI
dotnet run
```
Set a long random value for `Jwt:Key` (for example with `dotnet user-secrets`) before running. The SQLite database is created on first run.

Documentation is in [`docs/`](docs).

> **Note:** the app interface and the academic documentation in `docs/` are in Portuguese, as the project was developed at a Portuguese university.


---
Built with AI-assisted development (Claude): we defined the requirements, guided the implementation and reviewed and tested the result.
