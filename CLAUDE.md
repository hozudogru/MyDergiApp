# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Security note

The repo root contains a **committed OpenSSH private key** (`mydergiapp`, with its public half `mydergiapp.pub`), tracked in git since commit `18d1f2d`. `appsettings.json` also has real PostgreSQL and SMTP credentials committed in plaintext (only `mydergiapp.sql` is gitignored — the key files and other DB dumps at the root are not). Treat these as already compromised; do not add new secrets to tracked files, and flag this to the user if it comes up — do not attempt to rotate/purge history yourself unless asked.

## Project overview

MyDergiApp is an ASP.NET Core MVC (.NET 8) academic journal management system (Turkish-language UI — "Dergi" = journal/magazine). It models a full editorial workflow: authors submit articles, section/chief editors triage and assign reviewers, reviewers submit evaluations, editors make decisions, and accepted articles get published into issues on a public journal site.

- Database: PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL`, accessed through EF Core (`AppDbContext` in `Data/AppDbContext.cs`).
- Auth: ASP.NET Core Identity (`AppUser : IdentityUser` in `Entities/AppUser.cs`), cookie-based, with Razor Pages scaffolded under `Areas/Identity/Pages/Account` (only Login/Register/Logout/ForgotPassword/ResendEmailConfirmation are customized — this is not the full default Identity UI scaffold).
- No automated test project exists in this repo.

## Common commands

```bash
# Restore / build
dotnet restore
dotnet build

# Run locally (applies EF migrations automatically on startup, see Program.cs)
dotnet run

# EF Core migrations
dotnet ef migrations add <Name>
dotnet ef database update
```

There is no separate test project — do not look for a `dotnet test` target.

Local DB connection string and SMTP credentials live in `appsettings.json` (also `appsettings.Development.json`). No secrets manager / user-secrets in use — treat existing values as already-exposed rather than adding your own alongside them.

## Deployment

`.github/workflows/deploy.yml` auto-deploys straight to production on every push to `main`: it builds, publishes, `scp`s a tarball to the server, and restarts a systemd service (`MyDergiApp.service`) at `/var/www/mydergiapp`. There is no CI test gate, no staging environment, and no PR-based review gate on this pipeline — pushing to `main` is a live deploy. `appsettings*.json` and `wwwroot/uploads` are preserved/restored across deploys via a backup step in the script.

## Architecture

### Role-based editorial workflow

This is the core of the app and the thing to understand before touching submission logic. Roles are seeded in `Program.cs` at startup: `Admin`, `Editor`, `ChiefEditor`, `Reviewer`, `Author`, `Reader`. Two pieces of middleware in `Program.cs` run on every authenticated request (excluding login/logout/register/access-denied paths):
1. Any authenticated user with **no** role gets auto-assigned `Author`.
2. Any user with `IsActive == false` is force signed-out and redirected to `/Home/AccessDenied`.

A default admin (`admin@dergi.com` / seeded in `Program.cs`) is created/updated idempotently on every startup.

The submission lifecycle is driven by the `SubmissionStatus` enum (`Models/SubmissionStatus.cs`): `Taslak → Gonderildi → OnKontrolBekliyor → (YazaraIadeEdildi | AlanEditoruneYonlendirildi) → AlanEditorunde → HakemAtamasiBekliyor → HakemDegerlendirmesinde → (RevizyonIstendi → RevizyonYuklendi loop) → KabulEdildi | Reddedildi | GeriCekildi`. `StatusDisplayHelper` (`Helpers/StatusDisplayHelper.cs`) is the single place mapping statuses (and `ReviewerAssignmentStatus`, file type strings) to Turkish display text and Bootstrap badge classes — extend it rather than duplicating switch statements in views.

`Controllers/SubmissionController.cs` (~1900 lines) is the central controller and implements almost the entire workflow above — action-level `[Authorize(Roles = "...")]` attributes are the real source of truth for who can do what at each stage (e.g. `Editor` handles intake/pre-check/reviewer assignment, `ChiefEditor` handles final decisions, `Reviewer` submits reviews, `Author` creates/edits/withdraws/uploads revisions). When making changes to the workflow, grep this file for the relevant status/role combination rather than assuming a single "service layer" exists — there isn't one; controllers talk to `AppDbContext` directly.

Review rounds are modeled explicitly: `Submission.CurrentReviewRound` plus `SubmissionReviewer.ReviewRound` (unique index on `SubmissionId + ReviewerId + ReviewRound`) allow the same reviewer to be reassigned across multiple revision cycles without violating uniqueness.

`Areas/Admin/Controllers/SubmissionManagementController.cs` is a separate, much smaller controller that overlaps in purpose with the root-level `AdminController`/`SubmissionController` — check both when working on admin-facing submission management to avoid editing the wrong one.

### Data layer

`AppDbContext` (`Data/AppDbContext.cs`) extends `IdentityDbContext<AppUser>`. Key relationship notes from `OnModelCreating`:
- `SubmissionReviewer` → `Submission`/`Reviewer` FKs are `Cascade`/`Restrict` respectively; unique index on `(SubmissionId, ReviewerId, ReviewRound)`.
- `Submission.AssignedChiefEditor` / `AssignedSectionEditor` are separate optional FKs to `AppUser`, both `Restrict` on delete.
- `SubmissionFile.UploadedByUser` is `SetNull` on delete (files must survive user deletion).
- `IssueArticle` links a published `Issue` to a `Submission` (`Restrict` on submission delete, `Cascade` on issue delete).

Uploaded files are organized under `wwwroot/upload/submission/{main,published,reviewer-files,revisions,supplementary}/` (these folders are explicitly declared in `MyDergiApp.csproj` so they exist even when empty). Note this is `wwwroot/upload/...` (singular) for submission files, distinct from `wwwroot/uploads/` (plural, gitignored) used elsewhere (issue covers, home page banners, etc.) — check which one an existing feature uses before adding new upload code.

### Views

Views are organized by controller, not by role, but many controllers are themselves role-specific (`Views/Reviewer`, `Views/Author`, `Views/UserManagement`). `Views/Submission/*` mixes Turkish action-named views for different roles in one folder (e.g. `OnKontrolListesi` = editor pre-check queue, `AssignReviewer`/`EditorDashboard` = editor, `Makalelerim`/`YeniMakale` = author's "my articles"/"new article", `MyReviews`/`SubmitReview` = reviewer, `BanaYonlendirilenMakaleler` = "articles routed to me"). `_Layout.cshtml` is the authenticated-app shell; `_FrontLayout.cshtml` is used for the public-facing journal pages (`JournalController`, public `Issues`/`Announcements` views).

### Email

`Services/EmailService.cs` sends mail via the SMTP settings bound from config (`Models/SmtpSettings.cs`); `Services/EmailTemplateService.cs` renders templates from `Templates/` (e.g. `Templates/EditorDecisionEmailTemplate.html`) — used for editor-decision notifications to authors.
