using Microsoft.JSInterop;
using System.Text.Json;
using Gym_tracker.Models;

namespace Gym_tracker.Services;

public class WorkoutService(IJSRuntime js)
{
    private const string LocalStorageKey = "gym_tracker_workouts";

    private readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    // True = File System Access API available in this browser
    public bool IsFileApiSupported { get; set; }

    // True = API supported but user hasn't picked a file yet
    public bool NeedsFileSetup { get; set; }

    public bool IsInitialized { get; private set; }

    /// <summary>Call once on page load (before rendering data).</summary>
    public async Task InitializeAsync()
    {
        if (IsInitialized) return;
        IsFileApiSupported = await js.InvokeAsync<bool>("gymTracker.isSupported");
        if (IsFileApiSupported)
            NeedsFileSetup = !await js.InvokeAsync<bool>("gymTracker.hasHandle");
        IsInitialized = true;
    }

    /// <summary>
    /// Opens the OS "Save As" picker. Must be called from a user-gesture handler.
    /// </summary>
    public async Task<bool> PickFileAsync()
    {
        var picked = await js.InvokeAsync<bool>("gymTracker.pickFile");
        if (!picked) return false;

        NeedsFileSetup = false;
        return true;
    }

    /// <summary>Forget the stored file handle (lets user pick a different file).</summary>
    public async Task ClearFileHandleAsync()
    {
        await js.InvokeVoidAsync("gymTracker.clearHandle");
        NeedsFileSetup = true;
    }

    // ── Public CRUD: Templates ──────────────────────────────────────────────

    public async Task<List<WorkoutTemplate>> GetTemplatesAsync()
    {
        var store = await ReadStoreAsync();
        return store.Templates;
    }

    public async Task SaveTemplateAsync(WorkoutTemplate template)
    {
        var store = await ReadStoreAsync();
        var idx = store.Templates.FindIndex(t => t.Id == template.Id);
        if (idx >= 0) store.Templates[idx] = template;
        else store.Templates.Add(template);
        await PersistStoreAsync(store);
    }

    public async Task DeleteTemplateAsync(Guid id)
    {
        var store = await ReadStoreAsync();
        store.Templates.RemoveAll(t => t.Id == id);
        await PersistStoreAsync(store);
    }

    // ── Public CRUD: Logged Workouts ─────────────────────────────────────

    public async Task<List<LoggedWorkout>> GetLoggedWorkoutsAsync()
    {
        var store = await ReadStoreAsync();
        return store.LoggedWorkouts;
    }

    public async Task SaveLoggedWorkoutAsync(LoggedWorkout workout)
    {
        var store = await ReadStoreAsync();
        var idx = store.LoggedWorkouts.FindIndex(w => w.Id == workout.Id);
        if (idx >= 0) store.LoggedWorkouts[idx] = workout;
        else store.LoggedWorkouts.Add(workout);
        await PersistStoreAsync(store);
    }

    public async Task DeleteLoggedWorkoutAsync(Guid id)
    {
        var store = await ReadStoreAsync();
        store.LoggedWorkouts.RemoveAll(w => w.Id == id);
        await PersistStoreAsync(store);
    }

    // ── Legacy: Support old Workout model ────────────────────────────────
    [Obsolete("Use GetLoggedWorkoutsAsync() instead.")]
    public async Task<List<Workout>> GetAllAsync()
    {
        var logged = await GetLoggedWorkoutsAsync();
        // Convert LoggedWorkout back to Workout for backwards compatibility
        return logged.Select(lw => new Workout
        {
            Id = lw.Id,
            Name = lw.Name,
            Date = lw.Date,
            Exercises = lw.Exercises.Select(le => new Exercise
            {
                Name = le.Name,
                Type = le.Type,
                Reps = le.Sets.FirstOrDefault()?.Reps,
                WeightKg = le.Sets.FirstOrDefault()?.WeightKg,
                DurationSeconds = le.Sets.FirstOrDefault()?.DurationSeconds
            }).ToList()
        }).ToList();
    }

    [Obsolete("Use SaveLoggedWorkoutAsync() instead.")]
    public async Task SaveAsync(Workout workout)
    {
        var logged = new LoggedWorkout
        {
            Id = workout.Id,
            Name = workout.Name,
            Date = workout.Date,
            Exercises = workout.Exercises.Select(ex => new LoggedExercise
            {
                Name = ex.Name,
                Type = ex.Type,
                Sets = new List<ExerciseSet>
                {
                    new ExerciseSet
                    {
                        Reps = ex.Reps,
                        WeightKg = ex.WeightKg,
                        DurationSeconds = ex.DurationSeconds
                    }
                }
            }).ToList()
        };
        await SaveLoggedWorkoutAsync(logged);
    }

    [Obsolete("Use DeleteLoggedWorkoutAsync() instead.")]
    public async Task DeleteAsync(Guid id)
    {
        await DeleteLoggedWorkoutAsync(id);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<WorkoutStore> ReadStoreAsync()
    {
        if (IsFileApiSupported && !NeedsFileSetup)
            return await ReadStoreFromFileAsync();

        return await ReadStoreFromLocalStorageAsync();
    }

    private async Task PersistStoreAsync(WorkoutStore store)
    {
        if (IsFileApiSupported && !NeedsFileSetup)
            await WriteStoreToFileAsync(store);
        else
            await WriteStoreToLocalStorageAsync(store);
    }

    private async Task<WorkoutStore> ReadStoreFromFileAsync()
    {
        var text = await js.InvokeAsync<string?>("gymTracker.readFile");
        if (string.IsNullOrWhiteSpace(text)) return new WorkoutStore();
        try
        {
            return JsonSerializer.Deserialize<WorkoutStore>(text, _json) ?? new WorkoutStore();
        }
        catch { return new WorkoutStore(); }
    }

    private async Task WriteStoreToFileAsync(WorkoutStore store)
    {
        var json = JsonSerializer.Serialize(store, _json);
        await js.InvokeAsync<bool>("gymTracker.writeFile", json);
    }

    private async Task<WorkoutStore> ReadStoreFromLocalStorageAsync()
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", LocalStorageKey);
        if (string.IsNullOrEmpty(json)) return new WorkoutStore();
        try { return JsonSerializer.Deserialize<WorkoutStore>(json, _json) ?? new WorkoutStore(); }
        catch { return new WorkoutStore(); }
    }

    private async Task WriteStoreToLocalStorageAsync(WorkoutStore store)
    {
        var json = JsonSerializer.Serialize(store, _json);
        await js.InvokeVoidAsync("localStorage.setItem", LocalStorageKey, json);
    }
}

