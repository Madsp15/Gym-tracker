# Gym Tracker – Agent Guide

## Project Overview
Blazor WebAssembly PWA targeting **.NET 10** (single project, no backend). Users log workouts, track progress, and manage goals—all client-side. The app is in early development; the Home page contains three placeholder cards ("Today's Workout", "Progress", "Goals") that represent the primary feature areas to be built.

## Architecture
```
Gym-tracker/          ← sole C# project (SDK: Microsoft.NET.Sdk.BlazorWebAssembly)
  App.razor           ← router root; DefaultLayout = MainLayout
  _Imports.razor      ← global @using directives for all .razor files
  Pages/              ← routable components (@page directive)
  Layout/             ← MainLayout.razor + NavMenu.razor (+ paired .razor.css)
  wwwroot/
    css/app.css       ← global styles (dark theme + BEM utility classes)
    manifest.json     ← PWA manifest (standalone, portrait, theme #e05c00)
    service-worker.js ← offline PWA caching
```

**Namespace quirk**: the project folder is `Gym-tracker` but the C# namespace is `Gym_tracker` (hyphens → underscores). Use `@using Gym_tracker` / `namespace Gym_tracker` in all new files.

There is no API server or database today. Data will live in-browser (local storage / IndexedDB) until a backend is added.

## Developer Workflows

| Task | Command |
|------|---------|
| Run (dev, HTTP) | `dotnet run --project Gym-tracker/Gym-tracker.csproj` → http://localhost:5295 |
| Run (HTTPS) | same; also serves https://localhost:7255 |
| Hot reload | `dotnet watch --project Gym-tracker/Gym-tracker.csproj` |
| Build | `dotnet build Gym-tracker.sln` |
| Publish | `dotnet publish -c Release` |

The project uses Blazor dev-server (`Microsoft.AspNetCore.Components.WebAssembly.DevServer`), not a separate host.

## Conventions & Patterns

### New pages
Place routable components in `Pages/`, add a `@page "/route"` directive, and register the nav link in `Layout/NavMenu.razor`. Pages inherit `MainLayout` by default.

### Styling
- **Global** CSS → `wwwroot/css/app.css`
- **Component-scoped** CSS → create a sibling `MyComponent.razor.css` (automatically scoped by Blazor CSS isolation)
- Follow the existing **BEM-like** naming in `app.css`: block (`.home-card`), element (`__icon`, `__label`), modifier (`--placeholder`)
- Dark theme base: background `#1a1a1a`, surface `#2a2a2a`, text `#f0f0f0`
- Brand accent (orange): `#e05c00` – used for hero gradients, theme-color, PWA manifest

### Bootstrap
Bootstrap CSS is loaded via `wwwroot/lib/bootstrap`. Use Bootstrap utility classes for layout and spacing; reserve custom CSS for gym-specific components.

### HttpClient
A scoped `HttpClient` with `BaseAddress = HostEnvironment.BaseAddress` is pre-registered in `Program.cs`. Inject it in components to fetch static JSON from `wwwroot/sample-data/`.

## Key Files
| File | Purpose |
|------|---------|
| `Gym-tracker/Program.cs` | DI setup and Blazor host entry point |
| `Gym-tracker/_Imports.razor` | Add new global namespaces here |
| `Gym-tracker/wwwroot/css/app.css` | All global / theme tokens |
| `Gym-tracker/wwwroot/manifest.json` | PWA metadata |
| `Gym-tracker/Pages/Home.razor` | Dashboard entry point with placeholder feature cards |

