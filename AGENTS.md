# Gym Tracker – Agent Guide

## Project Overview
Blazor WebAssembly PWA targeting **.NET 10** (single project, no backend). Users log workouts, track progress, and manage goals—all client-side. The Home page (`/`) has three cards: **Templates** and **Logged Workouts** are fully implemented; **Progress** remains a placeholder.

## Architecture
```
Gym-tracker/          ← sole C# project (SDK: Microsoft.NET.Sdk.BlazorWebAssembly)
  App.razor           ← router root; DefaultLayout = MainLayout
  _Imports.razor      ← global @using directives for all .razor files
  Pages/              ← routable components (@page directive)
  Layout/             ← MainLayout.razor + NavMenu.razor (+ paired .razor.css)
  Models/             ← plain C# data models (WorkoutStore, WorkoutTemplate, LoggedWorkout, …)
  Services/           ← WorkoutService.cs + WgerService.cs
  Shared/             ← reusable non-routable components (ExerciseAutocomplete, MuscleBodyDiagram)
  wwwroot/
    css/app.css       ← global styles (dark theme + BEM utility classes)
    js/fileStorage.js ← window.gymTracker JS interop (File System Access API + IndexedDB)
    manifest.json     ← PWA manifest (standalone, portrait, theme #e05c00)
    service-worker.js ← offline PWA caching
```

**Namespace quirk**: the project folder is `Gym-tracker` but the C# namespace is `Gym_tracker` (hyphens → underscores). Use `@using Gym_tracker` / `namespace Gym_tracker` in all new files.

There is no API server or database. All data is persisted client-side by `WorkoutService` using a **dual-mode storage strategy**: if the browser supports the File System Access API (`showSaveFilePicker`), data is saved to a user-chosen JSON file (handle persisted in IndexedDB `gym_tracker_fs`); otherwise it falls back to `localStorage` under the key `gym_tracker_workouts`. The serialised root object is always `WorkoutStore`.

### Data models (`Models/`)
| Type | Role |
|------|------|
| `WorkoutStore` | Root container: `Version`, `List<WorkoutTemplate> Templates`, `List<LoggedWorkout> LoggedWorkouts` |
| `WorkoutTemplate` | Reusable plan: `Guid Id`, `string Name`, `List<Exercise> Exercises` |
| `Exercise` | Template exercise: `Name`, `ExerciseType`, `string? MuscleGroup`, `int Sets` (default count), optional `Reps`/`WeightKg`/`DurationSeconds` (first-use defaults), plus last-session data: `List<ExerciseSet> LastSets`, `string? LastNotes`, `bool LastProgressiveOverload`, `DateTime? LastPerformed` |
| `LoggedWorkout` | A completed session: `Guid Id`, `string Name`, `DateTime Date`, `Guid? TemplateId`, `List<LoggedExercise>` |
| `LoggedExercise` | Exercise within a session: `Name`, `ExerciseType`, `List<ExerciseSet> Sets`, `string? Notes`, `bool ProgressiveOverload` |
| `ExerciseSet` | One set: `int? Reps`, `double? WeightKg`, `int? DurationSeconds` |
| `ExerciseType` | Enum: `RepsWeight` \| `Timed` |
| `Workout` | **[Obsolete]** – legacy flat model; use `LoggedWorkout` instead |

### Page routes
| Route | Component | Notes |
|-------|-----------|-------|
| `/` | `Pages/Home.razor` | Dashboard |
| `/templates` | `Pages/Templates.razor` | List / delete / start-workout from templates |
| `/templates/new`, `/templates/{id:guid}` | `Pages/TemplateEdit.razor` | Create / edit template with per-exercise defaults and last-session hints |
| `/workouts` | `Pages/Workouts.razor` | List / delete logged workouts; **calendar view is the default tab** (list also available via toggle) |
| `/workouts/log` | `Pages/LogWorkout.razor` | Template picker → log new workout |
| `/workouts/log/{templateId:guid}` | `Pages/LogWorkout.razor` | Deep-link: start workout directly from a specific template |
| `/workouts/{id:guid}` | `Pages/LogWorkout.razor` | Edit existing logged workout |
| `/workouts-legacy/...` | `Pages/WorkoutEdit.razor` | **Deprecated** – do not use |

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
- Dark theme palette: background `#0f1420`, surface `#192033`, border `#283a5a`, text `#D0DCF1`, muted `#7d90b5`
- Accent colours: purple `#6A5BC2` (primary interactive), blue `#2F57A0` (secondary / info)
- Use `@bind:culture="@System.Globalization.CultureInfo.InvariantCulture"` on all `<input type="number">` fields bound to `double` or `double?` to avoid decimal-separator issues across locales.

### Bootstrap
Bootstrap CSS is loaded via `wwwroot/lib/bootstrap`. Use Bootstrap utility classes for layout and spacing; reserve custom CSS for gym-specific components. Use Bootstrap `input-group` + `input-group-text` to attach unit labels (kg, s) to number inputs, styled via scoped CSS to match the dark theme.

### HttpClient
A scoped `HttpClient` with `BaseAddress = HostEnvironment.BaseAddress` is pre-registered in `Program.cs`. Inject it in components to fetch static JSON from `wwwroot/sample-data/`.

### WorkoutService
`WorkoutService` is registered as **scoped** in `Program.cs`. It must be initialised before accessing data:

```csharp
@inject WorkoutService WorkoutService

protected override async Task OnInitializedAsync()
{
    await WorkoutService.InitializeAsync(); // detects File API / localStorage
    _templates = await WorkoutService.GetTemplatesAsync();
}
```

Key members:
- `IsFileApiSupported` / `NeedsFileSetup` / `IsInitialized` – storage state flags
- `PickFileAsync()` – must be called from a user-gesture handler (button click)
- `ClearFileHandleAsync()` – lets the user switch to a different file
- CRUD: `GetTemplatesAsync`, `SaveTemplateAsync`, `DeleteTemplateAsync`
- CRUD: `GetLoggedWorkoutsAsync`, `SaveLoggedWorkoutAsync`, `DeleteLoggedWorkoutAsync`
- Obsolete: `GetAllAsync` / `SaveAsync` / `DeleteAsync` (wrap `Workout`; do not use in new code)

### WgerService
`WgerService` is registered as **scoped** in `Program.cs`. It hits the free [wger Workout Manager API](https://wger.de/api/v2/) to provide exercise name autocomplete suggestions. Results are cached in-memory for the session.

Key member:
- `SearchExercisesAsync(string term)` – returns `List<WgerExerciseInfo>` (up to 8 results) for the given search term (min 2 chars). Each result has `Name` and `Category`. Category names from the API are normalised to canonical English muscle group names (`Core`, `Arms`, `Back`, `Calves`, `Chest`, `Legs`, `Shoulders`) regardless of the browser locale.

### Shared Components (`Shared/`)

#### `ExerciseAutocomplete`
Wraps a text `<input>` with a debounced dropdown powered by `WgerService`. Used in `TemplateEdit` for the exercise name field.

| Parameter | Type | Purpose |
|-----------|------|---------|
| `Value` / `ValueChanged` | `string` | Two-way binding for the text value |
| `OnExerciseSelected` | `EventCallback<WgerExerciseInfo>` | Fires only when the user picks from the dropdown; carries `Name` + `Category` |
| `Placeholder` | `string` | Input placeholder text |
| `CssClass` | `string` | Extra CSS class(es) on the `<input>` |

Usage pattern – after a pick, copy `Category` → `Exercise.MuscleGroup`:
```razor
<ExerciseAutocomplete @bind-Value="ex.Name"
                      OnExerciseSelected="info => ex.MuscleGroup = info.Category" />
```

#### `MuscleBodyDiagram`
Interactive front/back SVG body diagram. Clickable muscle regions highlight in purple when selected. Used in `TemplateEdit` to visually tag an exercise's primary muscle group.

| Parameter | Type | Purpose |
|-----------|------|---------|
| `MuscleGroup` / `MuscleGroupChanged` | `string?` | Two-way binding for the active muscle group name |

Supported region names (toggled on click): `Chest`, `Back`, `Lower Back`, `Traps`, `Shoulders`, `Biceps`, `Triceps`, `Forearms`, `Core`, `Quads`, `Hamstrings`, `Glutes`, `Calves`.
Front view shows: Chest, Core, Shoulders, Biceps, Forearms, Quads, Calves. Back view shows: Back, Lower Back, Traps, Glutes, Hamstrings, plus Shoulders, Triceps, Forearms, Calves (shared with front).
`WgerService` maps the broad wger "Arms" category → `Biceps` and "Legs" → `Quads` (all language variants). `Triceps`, `Hamstrings`, `Traps`, and `Lower Back` have no wger equivalent and must be set manually on the diagram or via the pill buttons.

### JS interop (`window.gymTracker`)
Defined in `wwwroot/js/fileStorage.js`. Called exclusively by `WorkoutService` via `IJSRuntime`:
| JS function | Returns | Purpose |
|-------------|---------|---------|
| `gymTracker.isSupported()` | `bool` | `'showSaveFilePicker' in window` |
| `gymTracker.hasHandle()` | `bool` | IndexedDB has a persisted file handle |
| `gymTracker.pickFile()` | `bool` | Opens OS "Save As" picker; stores handle |
| `gymTracker.readFile()` | `string?` | Reads JSON text from the picked file |
| `gymTracker.writeFile(json)` | `bool` | Writes JSON text to the picked file |
| `gymTracker.clearHandle()` | – | Deletes stored handle from IndexedDB |

### Template → Workout flow
1. `/workouts/log` shows a **template picker** (card grid). Picking a template or using the deep-link route calls `ApplyTemplate`.
2. `ApplyTemplate` checks `Exercise.LastSets`: if non-empty it clones those exact sets as the starting point; otherwise it generates `Exercise.Sets` sets pre-filled with the first-use defaults (`Reps`, `WeightKg`, `DurationSeconds`). It also sets `_workout.TemplateId` and builds `_templateExByName` (a `Dictionary<string, Exercise>`) for O(1) last-time hint lookups.
3. While logging, each exercise row shows a **last-time hint bar** (date · set summary · 🏆 if PO · note) sourced from `_templateExByName`. Below the sets section are a **Progressive overload** pill toggle and a **Notes** textarea, both bound to the `LoggedExercise`.
4. On **Save**, `WriteBackToTemplateAsync` is called: it writes `LastSets`, `LastNotes`, `LastProgressiveOverload`, `LastPerformed`, and the updated `Sets` count back into the matching `Exercise` in the template and calls `SaveTemplateAsync`. This means the template always reflects the last real session.
5. **`TemplateEdit`** displays a read-only last-session hint below the defaults row for any exercise that has been logged at least once.

### Razor / Blazor pitfalls
- **`@{ }` code blocks** inside nested `@else if { }` markup blocks cause `RZ1010` parse errors. Use C# properties or helper methods instead of inline variable declarations.
- **Block lambdas in `@onclick`**: `@onclick="() => { ... }"` can confuse the Razor parser. Always extract to a named method: `@onclick="() => MyMethod(arg)"`.
- **`double?` with `@bind`** on `<input type="number">**: always add `@bind:culture="@System.Globalization.CultureInfo.InvariantCulture"` to prevent the browser blanking the field after re-render on locales that use a comma decimal separator.
- **`::placeholder` on dark backgrounds**: Bootstrap's default `--bs-secondary-color` renders near-black, making placeholders invisible on dark surfaces. Always add an explicit `::placeholder` rule in the component's scoped CSS, plus `opacity: 1` to override Firefox's default `0.54` reduction:
  ```css
  .my-input::placeholder { color: #4a5a78; opacity: 1; }
  ```

## Key Files
| File | Purpose |
|------|---------|
| `Gym-tracker/Program.cs` | DI setup and Blazor host entry point |
| `Gym-tracker/_Imports.razor` | Add new global namespaces here |
| `Gym-tracker/wwwroot/css/app.css` | All global / theme tokens |
| `Gym-tracker/wwwroot/manifest.json` | PWA metadata |
| `Gym-tracker/wwwroot/js/fileStorage.js` | `window.gymTracker` JS interop implementation |
| `Gym-tracker/Services/WorkoutService.cs` | All data access; the only place `IJSRuntime` is used |
| `Gym-tracker/Services/WgerService.cs` | wger API client for exercise autocomplete; normalises category names to canonical English muscle groups |
| `Gym-tracker/Models/WorkoutStore.cs` | Root serialised object (stored as JSON) |
| `Gym-tracker/Models/Exercise.cs` | Template exercise + `MuscleGroup` + first-use defaults + last-session data |
| `Gym-tracker/Models/LoggedExercise.cs` | Per-session exercise data including Notes and ProgressiveOverload |
| `Gym-tracker/Models/LoggedWorkout.cs` | Completed session; carries `TemplateId?` for write-back |
| `Gym-tracker/Pages/Home.razor` | Dashboard; Templates & Logged Workouts cards are live, Progress is placeholder |
| `Gym-tracker/Pages/LogWorkout.razor` | Template picker, set-level logging, last-time hints, PO toggle, write-back on save |
| `Gym-tracker/Pages/LogWorkout.razor.css` | Scoped styles: template picker, view toggle, stepper, last-time bar, PO toggle, notes (incl. `::placeholder` fix) |
| `Gym-tracker/Pages/TemplateEdit.razor` | Create/edit template with per-exercise defaults (Sets, Reps, Weight/Duration), `MuscleBodyDiagram`, `ExerciseAutocomplete`, and last-session read-only hint |
| `Gym-tracker/Pages/TemplateEdit.razor.css` | Scoped styles: exercise-defaults row, last-time hint |
| `Gym-tracker/Pages/Templates.razor` | List templates; has ▶ Start button that navigates to `/workouts/log/{templateId}` |
| `Gym-tracker/Pages/Workouts.razor` | List / delete logged workouts; **calendar is the default view** (`_viewMode = "calendar"`); list view available via toggle |
| `Gym-tracker/Pages/Workouts.razor.css` | Scoped styles: view toggle, calendar grid, day cells, selected-date detail panel |
| `Gym-tracker/Shared/ExerciseAutocomplete.razor` | Debounced exercise name input with wger-powered dropdown; `OnExerciseSelected` callback carries `WgerExerciseInfo` |
| `Gym-tracker/Shared/ExerciseAutocomplete.razor.css` | Scoped styles: dropdown, active item highlight |
| `Gym-tracker/Shared/MuscleBodyDiagram.razor` | Interactive front/back SVG body map; emits muscle group name via `MuscleGroupChanged` |
| `Gym-tracker/Shared/MuscleBodyDiagram.razor.css` | Scoped styles: diagram layout, region hover/active states, label |
