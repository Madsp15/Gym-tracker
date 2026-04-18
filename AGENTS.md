# Gym Tracker – Agent Guide

## Project Overview
Blazor WebAssembly PWA targeting **.NET 10** (single project, no backend). Users log workouts, track progress, and manage goals—all client-side. The Home page (`/`) has three cards: **Templates**, **Logged Workouts**, and **Progress** — all fully implemented.

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
| `LoggedWorkout` | A completed session: `Guid Id`, `string Name`, `DateTime Date`, `Guid? TemplateId`, `List<LoggedExercise>`, `int? DurationMinutes` (stopwatch-recorded session length) |
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
| `/progress` | `Pages/Progress.razor` | Date-range filter (30d/90d/1y/All), summary tiles, streak + best-streak chips, activity heatmap (53 weeks), muscle-group filter, per-exercise cards with weight/duration/distance/pace bars, est. 1RM, expandable line/pace/volume charts, real PR badge (🏆) + Progressive-overload chip (PO) |
| `/settings` | `Pages/Settings.razor` | Storage management (incl. **reconnect file** when permission expires), export/import backup, PWA install, default rest timer, weight/distance units, transient toast feedback, clear all data |
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

### NavMenu icons
Each nav link in `Layout/NavMenu.razor` uses a `<span class="bi bi-{name}-nav-menu">` element for its icon. The icon is rendered as a CSS `background-image` (inline SVG, `fill='white'`) defined in `Layout/NavMenu.razor.css`. **When adding a new nav link, you must add a matching `.bi-{name}-nav-menu` rule to `NavMenu.razor.css`** — the span without a CSS rule renders as an invisible blank space.

Current icon classes and their Bootstrap Icons source:
| CSS class | Icon |
|-----------|------|
| `.bi-house-door-fill-nav-menu` | `bi-house-door-fill` |
| `.bi-grid-fill-nav-menu` | `bi-grid-fill` |
| `.bi-activity-nav-menu` | `bi-activity` |
| `.bi-graph-up-nav-menu` | `bi-graph-up` |
| `.bi-gear-fill-nav-menu` | `bi-gear-fill` |

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
- `ExportJsonAsync()` – returns the full `WorkoutStore` serialised as a JSON string (used by the export button in `Workouts.razor`)
- Obsolete: `GetAllAsync` / `SaveAsync` / `DeleteAsync` (wrap `Workout`; do not use in new code)

### WgerService
`WgerService` is registered as **scoped** in `Program.cs`. It hits the free [wger Workout Manager API](https://wger.de/api/v2/) to provide exercise name autocomplete suggestions. Results are cached in-memory for the session.

Key member:
- `SearchExercisesAsync(string term)` – returns `List<WgerExerciseInfo>` (up to 8 results) for the given search term (min 2 chars). Each result has `Name` and `Category`. Category names from the API are normalised to canonical English muscle group names (`Core`, `Arms`, `Back`, `Calves`, `Chest`, `Legs`, `Shoulders`) regardless of the browser locale.

### Shared Components (`Shared/`)

#### `PwaInstallBanner`
Shown once per session at the bottom of every page (rendered from `MainLayout`). Detects install eligibility via `gymTracker` JS helpers and shows an appropriate prompt:
- **Chrome/Edge/Samsung**: shows "Install app" button that triggers `showInstallPrompt()`.
- **iOS Safari**: shows manual "tap Share → Add to Home Screen" instructions.
- Hidden if the app is already running in standalone mode, or if the user dismissed it this session (`sessionStorage` key `pwa_banner_dismissed`).

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
Defined in `wwwroot/js/fileStorage.js`. Called via `IJSRuntime`:
| JS function | Returns | Purpose |
|-------------|---------|---------|
| `gymTracker.isSupported()` | `bool` | `'showSaveFilePicker' in window` |
| `gymTracker.hasHandle()` | `bool` | IndexedDB has a persisted file handle |
| `gymTracker.pickFile()` | `bool` | Opens OS "Save As" picker; stores handle |
| `gymTracker.readFile()` | `string?` | Reads JSON text from the picked file |
| `gymTracker.writeFile(json)` | `bool` | Writes JSON text to the picked file |
| `gymTracker.clearHandle()` | – | Deletes stored handle from IndexedDB |
| `gymTracker.playChime()` | – | Plays a short 3-tone chime (rest timer end) |
| `gymTracker.downloadJson(content, filename)` | – | Triggers a browser download of `content` as a `.json` file |
| `gymTracker.isInStandaloneMode()` | `bool` | `true` if the app is running as an installed PWA |
| `gymTracker.isIos()` | `bool` | `true` on iPhone/iPad (no `beforeinstallprompt` support) |
| `gymTracker.isInstallable()` | `bool` | `true` if a `beforeinstallprompt` event was captured and app is not yet installed |
| `gymTracker.showInstallPrompt()` | `string` | Triggers the browser install prompt; returns `'accepted'` or `'dismissed'` |

### Template → Workout flow
1. `/workouts/log` shows a **template picker** (card grid). Picking a template or using the deep-link route calls `ApplyTemplate`.
2. `ApplyTemplate` checks `Exercise.LastSets`: if non-empty it clones those exact sets as the starting point; otherwise it generates `Exercise.Sets` sets pre-filled with the first-use defaults (`Reps`, `WeightKg`, `DurationSeconds`). It also sets `_workout.TemplateId` and builds `_templateExByName` (a `Dictionary<string, Exercise>`) for O(1) last-time hint lookups.
3. A **stopwatch** starts automatically when logging begins (template selected, blank workout started, or deep-link navigation). Elapsed time is displayed as a `MM:SS` chip in the page header. On **Save**, the elapsed time is stored as `LoggedWorkout.DurationMinutes` (rounded, minimum 1 min). Editing an existing workout does **not** start the stopwatch.
4. While logging, each exercise row shows a **last-time hint bar** (date · set summary · if PO · note) sourced from `_templateExByName`. Below the sets section are a **Progressive overload** pill toggle and a **Notes** textarea, both bound to the `LoggedExercise`.
5. On **Save**, `WriteBackToTemplateAsync` is called: it writes `LastSets`, `LastNotes`, `LastProgressiveOverload`, `LastPerformed`, and the updated `Sets` count back into the matching `Exercise` in the template and calls `SaveTemplateAsync`. This means the template always reflects the last real session.
6. **`TemplateEdit`** displays a read-only last-session hint below the defaults row for any exercise that has been logged at least once.

### Weight unit preference
All weights are **stored as kg** in the JSON (in `ExerciseSet.WeightKg` and `Exercise.WeightKg`). The unit is display-only and controlled by the `localStorage` key `gym_tracker_weight_unit` (value `"kg"` or `"lbs"`). The conversion factor is `lbs = kg × 2.20462`.

All distances are **stored as km** in the JSON (in `ExerciseSet.DistanceKm` and `Exercise.DistanceKm`). The unit is display-only and controlled by the `localStorage` key `gym_tracker_distance_unit` (value `"km"` or `"mi"`). The conversion factor is `mi = km × 0.621371`. Pace (stored as min/km) converts to min/mi via `min/mi = min/km × 1.60934`.

Every component that shows or accepts weight values must:
1. Inject `@inject IJSRuntime JS` and add a `private bool _isMetric = true;` field.
2. Load the preference in `OnInitializedAsync`: `_isMetric = await JS.InvokeAsync<string?>("localStorage.getItem", "gym_tracker_weight_unit") != "lbs";`
3. Expose `private string WeightUnit => _isMetric ? "kg" : "lbs";`
4. Use `WeightForDisplay(double? kg)` to convert kg → display value for rendering, and `WeightFromInput(double? display)` to convert back on `@onchange`.
5. Because `@bind` cannot transform values, weight inputs must use `value="@WeightForDisplay(set.WeightKg)"` + `@onchange="e => set.WeightKg = WeightFromInput(ParseDouble(e.Value))"` instead of `@bind`.
6. Any `FormatWeight`/`FormatVolume`/`FormatLastSets`/`WeightDeltaText` helpers that include a unit suffix must be **instance methods** (not `static`) so they can access `_isMetric`/`WeightUnit`.

The same pattern applies to distance: add `_isKm`, `DistanceUnit`, `DistForDisplay`, `DistFromInput`, and use `value`/`@onchange` on distance inputs. Any `FormatDistance`/`FormatPace`/`DistanceDeltaText`/`PaceDeltaText` helpers must also be instance methods.

Components currently implementing this pattern: `LogWorkout.razor`, `TemplateEdit.razor`, `Progress.razor`, `Settings.razor`.

### User preferences (localStorage)
| Key | Type | Purpose |
|-----|------|---------|
| `gym_tracker_rest_default` | `int` (seconds) | Default rest timer preset; loaded in `LogWorkout.OnInitializedAsync` |
| `gym_tracker_weight_unit` | `"kg"` \| `"lbs"` | Display unit for all weight values; loaded in `OnInitializedAsync` of every weight-showing page |
| `gym_tracker_distance_unit` | `"km"` \| `"mi"` | Display unit for all cardio distance and pace values; loaded in `OnInitializedAsync` of every distance-showing page |
| `gym_tracker_progress_range` | `int` (days, `0` = all) | Date-range filter on the Progress page; valid values `0`/`30`/`90`/`365` |

### App version
The version string shown on the Settings page is read at runtime from `AssemblyInformationalVersionAttribute` (with the `+<gitHash>` build-metadata suffix stripped). The single source of truth is the **`<Version>` MSBuild property in `Gym-tracker/Gym-tracker.csproj`** — bump it there, never hard-code a constant in `Settings.razor`. The SDK auto-emits `AssemblyVersion`, `FileVersion`, and `AssemblyInformationalVersion` from that one property. The full informational version (including hash, when available) is exposed via the `title="…"` tooltip on the version row for support diagnostics.

### Import-validation caps
`Pages/Settings.razor` defines `MaxTemplates` (500), `MaxLoggedWorkouts` (10 000), `MaxNameLength` (200), `MaxExercisesPerItem` (200), and `MaxImportFileBytes` (10 MB) as named constants in the `@code` block. These are sanity caps that reject obviously corrupt or hostile backup files at the import-confirm step. Bump them only if real users legitimately exceed them; do not reintroduce the original magic numbers inline.

### Progress page
`Pages/Progress.razor` builds an in-memory list of `ExerciseStat` objects from `_workouts` via `BuildStats()`. All per-exercise stats and summary tiles are scoped to the active **date range** (`_rangeDays`: `0`=all, `30`, `90`, `365`, persisted as `gym_tracker_progress_range`). Changing the range clears `_chartDataCache`, `_expandedCharts`, `_expandedPaceCharts`, and `_expandedVolumeCharts` before rebuilding so no stale per-exercise SVG paths leak across ranges.

Stats surfaced on each card:
- **PR badge (🏆)** — `PrCount` is the number of sessions whose best metric strictly beats every prior session (highest weight/duration/distance, lowest pace). Distinct from `PoCount`, which is the number of sessions the user manually flagged as **Progressive overload** (shown as a separate **PO ×N** chip).
- **Streak chips** in the highlights row are computed in `ComputeStreaks()` (consecutive ISO weeks containing ≥1 workout). `MondayOf(DateTime)` is the helper used everywhere a week bucket is needed.
- **Activity heatmap** is a 53-week × 7-day grid of `<rect>` cells coloured via the `.heatmap-cell--lvl0..4` CSS classes (purple ramp on-theme); rendered inside a `<details>` so it's collapsed by default and keyboard-toggleable.
- **Charts** (`<svg>`) all carry `role="img"` + `aria-label` + `<title>` for screen readers; per-dot tooltips remain via `<title>` on each `<circle>`.

When adding new range-scoped stats, derive them from `WorkoutsInRange` (or directly inside `BuildStats`) — never from raw `_workouts`. The four existing summary tiles + streak chips are intentionally **range-independent** (lifetime overview); only per-exercise cards and the body of `BuildStats` are filtered.

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
| `Gym-tracker/Models/LoggedWorkout.cs` | Completed session; carries `TemplateId?` for write-back and `DurationMinutes?` recorded by stopwatch |
| `Gym-tracker/Pages/Home.razor` | Dashboard; Templates, Logged Workouts, and Progress cards all link to live pages |
| `Gym-tracker/Pages/LogWorkout.razor` | Template picker, set-level logging, stopwatch timer, last-time hints, PO toggle, write-back on save |
| `Gym-tracker/Pages/LogWorkout.razor.css` | Scoped styles: template picker, view toggle, stepper, last-time bar, PO toggle, notes, stopwatch chip (incl. `::placeholder` fix) |
| `Gym-tracker/Pages/TemplateEdit.razor` | Create/edit template with per-exercise defaults (Sets, Reps, Weight/Duration), `MuscleBodyDiagram`, `ExerciseAutocomplete`, last-session read-only hint, and drag-to-reorder exercises |
| `Gym-tracker/Pages/TemplateEdit.razor.css` | Scoped styles: exercise-defaults row, last-time hint |
| `Gym-tracker/Pages/Templates.razor` | List templates; has ▶ Start, ✎ Edit, ⧉ Duplicate, and 🗑 Delete buttons per card |
| `Gym-tracker/Pages/Workouts.razor` | List / delete logged workouts; shows duration; **calendar is the default view** (`_viewMode = "calendar"`); has Export JSON button |
| `Gym-tracker/Pages/Workouts.razor.css` | Scoped styles: view toggle, calendar grid, day cells, selected-date detail panel |
| `Gym-tracker/Pages/Progress.razor` | Date-range filter, summary tiles, streak chips, activity heatmap, muscle filter, multi-sort, per-exercise stat cards (weight/duration/distance/pace + 1RM), expandable line/pace/volume charts, real PR + PO badges |
| `Gym-tracker/Pages/Progress.razor.css` | Scoped styles: summary tiles, highlights, range/sort/filter chips, exercise cards, metric blocks, progress bars, SVG chart elements, activity heatmap (`heatmap-cell--lvl0..4` purple ramp), empty state |
| `Gym-tracker/Pages/Settings.razor` | Storage management (incl. **reconnect file** branch via `WorkoutService.RequestPermissionAsync` + page reload), export/import backup with auto-clearing success banners and re-keyed `<InputFile>`, PWA install prompt (respects `accepted`/`dismissed` outcome), default rest timer preference, **weight unit toggle (kg/lbs)**, distance unit toggle (km/mi), version row sourced from assembly metadata, clear all data |
| `Gym-tracker/Pages/Settings.razor.css` | Scoped styles: section cards, setting rows, preset pills, import confirm, danger zone |
| `Gym-tracker/Shared/ExerciseAutocomplete.razor` | Debounced exercise name input with wger-powered dropdown; `OnExerciseSelected` callback carries `WgerExerciseInfo` |
| `Gym-tracker/Shared/ExerciseAutocomplete.razor.css` | Scoped styles: dropdown, active item highlight |
| `Gym-tracker/Shared/MuscleBodyDiagram.razor` | Interactive front/back SVG body map; emits muscle group name via `MuscleGroupChanged` |
| `Gym-tracker/Shared/MuscleBodyDiagram.razor.css` | Scoped styles: diagram layout, region hover/active states, label |
| `Gym-tracker/Shared/PwaInstallBanner.razor` | PWA install prompt shown once per session; Chrome/Edge shows "Install app" button; iOS shows manual Share→Add instructions |
| `Gym-tracker/Shared/PwaInstallBanner.razor.css` | Scoped styles: fixed bottom banner with purple top border |
