# ️ Gym Tracker

A **Progressive Web App (PWA)** for logging gym workouts, tracking progress, and managing workout templates — all client-side with no backend required.

Built with **Blazor WebAssembly** targeting **.NET 10**.

---

##  Using the App (GitHub Pages)

The app is hosted at: **https://madsp15.github.io/Gym-tracker/**

### Step 1 — Install it to your home screen

Installing the app gives you a full-screen experience with no browser chrome, just like a native app.

**Android (Chrome or Edge)**
1. Open the link above in Chrome or Edge
2. Tap the **"Install app"** banner that appears at the bottom, or tap ⋮ → *Add to Home screen*
3. Tap **Install** in the prompt

**iPhone / iPad (Safari)**
1. Open the link above in Safari *(must be Safari — Chrome/Firefox on iOS cannot install PWAs)*
2. Tap the **Share** button (the box with an arrow at the bottom of the screen)
3. Scroll down and tap **Add to Home Screen**
4. Tap **Add**

**Desktop (Chrome or Edge)**
1. Open the link in Chrome or Edge
2. Click the install icon (⊕) in the address bar, or click ⋮ → *Install Gym Tracker…*

> Once installed, the app works **fully offline** — no internet connection is needed after the first load.

---

### Step 2 — Set up data storage

The first time you open the app you'll be asked where to save your data.

| Browser | What happens |
|---------|-------------|
| **Chrome / Edge desktop** | A "Choose save location" prompt appears — pick any folder (e.g. iCloud Drive, OneDrive, or a local folder). Your data is saved as a `workouts.json` file and syncs across devices if you use a cloud folder. |
| **Firefox / Safari / iOS** | Data is stored automatically in your browser's local storage — no setup needed. |

> **File permission on relaunch:** Chrome/Edge asks for file permission once per session. A **"Reconnect to file"** banner appears at the top of the app when needed — tap it once and you're good for the whole session.

---

### Step 3 — Back up your data

Go to **Settings** at any time to:
- **Export** all your data as a JSON file (great for backups or moving to another device)
- **Import** a previously exported backup
- Switch between **kg / lbs** and **km / mi**

---

## Features

- **Workout Templates** – Create reusable workout plans with per-exercise defaults (sets, reps, weight/duration) and an interactive muscle-group body diagram.
- **Workout Logging** – Log sessions from a template or start a blank workout. Automatically pre-fills sets from your last session and shows last-time hints for every exercise.
- **Stopwatch** – Auto-starts when you begin a workout; saves the session duration on completion.
- **Progressive Overload tracking** – Mark exercises as progressive overload per session; the flag is written back to the template for next-session reference.
- **Calendar & List views** – Browse logged workouts in a calendar or list. Workouts are colour-coded by day.
- **Progress page** – Per-exercise stats and charts showing strength and volume trends over time.
- **Weight & distance unit preferences** – Toggle between kg/lbs and km/mi; all values are stored in metric and converted for display only.
- **Exercise autocomplete** – Powered by the [wger Workout Manager API](https://wger.de/api/v2/) with in-session caching.
- **Offline-ready** – Service worker caches the app shell for full offline use.
- **Installable** – Add to home screen on Android (Chrome/Edge) and iOS (Safari).

---

## Data Storage

There is no server or database. All data is persisted client-side using a **dual-mode strategy**:

| Mode | Mechanism |
|------|-----------|
| **File API** (Chrome/Edge desktop) | User picks a `.json` file via the File System Access API; the handle is stored in IndexedDB so the file is read/written automatically on each session. |
| **localStorage fallback** | Used when the File System Access API is unavailable (Firefox, Safari, iOS). Data is stored under the key `gym_tracker_workouts`. |

The serialised root object is always `WorkoutStore`, which contains all templates and logged workouts.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | Blazor WebAssembly (.NET 10) |
| Styling | Bootstrap 5 + custom dark-theme CSS |
| Storage | File System Access API / localStorage / IndexedDB |
| Exercise data | wger Workout Manager REST API |
| PWA | Web App Manifest + Service Worker |

---

## Project Structure

```
Gym-tracker/
  App.razor               ← Router root
  _Imports.razor          ← Global @using directives
  Pages/                  ← Routable page components
  Layout/                 ← MainLayout + NavMenu
  Models/                 ← C# data models
  Services/               ← WorkoutService, WgerService
  Shared/                 ← Reusable components
  wwwroot/
    css/app.css           ← Global styles (dark theme)
    js/fileStorage.js     ← JS interop (File API + IndexedDB)
    manifest.json         ← PWA manifest
    service-worker.js     ← Offline caching
```

### Pages

| Route | Page | Description |
|-------|------|-------------|
| `/` | Home | Dashboard with quick-nav cards |
| `/templates` | Templates | List, create, edit, duplicate, and delete workout templates |
| `/templates/new` | TemplateEdit | Create a new template |
| `/templates/{id}` | TemplateEdit | Edit an existing template |
| `/workouts` | Workouts | Calendar and list views of logged workouts |
| `/workouts/log` | LogWorkout | Pick a template and log a new session |
| `/workouts/log/{templateId}` | LogWorkout | Deep-link: start directly from a specific template |
| `/workouts/{id}` | LogWorkout | Edit an existing logged workout |
| `/progress` | Progress | Per-exercise stats, charts, and global summary |
| `/settings` | Settings | Storage mode, export/import, unit preferences, rest timer, clear data |

### Data Models

| Model | Description |
|-------|-------------|
| `WorkoutStore` | Root object: holds all templates and logged workouts |
| `WorkoutTemplate` | A reusable workout plan with a list of exercises |
| `Exercise` | Template exercise with defaults and last-session data |
| `LoggedWorkout` | A completed session (date, duration, exercises) |
| `LoggedExercise` | One exercise within a logged session |
| `ExerciseSet` | A single set: reps, weight (kg), or duration (seconds) |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Run locally

```bash
# HTTP (recommended for development)
dotnet run --project Gym-tracker/Gym-tracker.csproj
# → http://localhost:5295

# With hot reload
dotnet watch --project Gym-tracker/Gym-tracker.csproj
```

### Build

```bash
dotnet build Gym-tracker.sln
```

### Publish

```bash
dotnet publish -c Release
```

Deploy the contents of `bin/Release/net10.0/publish/wwwroot` to any static file host (GitHub Pages, Azure Static Web Apps, Netlify, etc.).

> **Note:** The app must be served over **HTTPS** for the PWA install prompt and the File System Access API to work.

---

## Settings & Preferences

All preferences are stored in `localStorage`:

| Key | Values | Purpose |
|-----|--------|---------|
| `gym_tracker_weight_unit` | `kg` / `lbs` | Weight display unit |
| `gym_tracker_distance_unit` | `km` / `mi` | Distance display unit |
| `gym_tracker_rest_default` | seconds (int) | Default rest timer between sets |

Export and import your full workout data as JSON from the **Settings** page.

---

## License

This project is for personal use. No license is currently specified.

