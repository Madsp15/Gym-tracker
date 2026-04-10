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
    /// Migrates any existing localStorage data into the new file automatically.
    /// </summary>
    public async Task<bool> PickFileAsync()
    {
        var picked = await js.InvokeAsync<bool>("gymTracker.pickFile");
        if (!picked) return false;

        NeedsFileSetup = false;

        // Migrate existing localStorage data into the new file
        var existing = await ReadFromLocalStorageAsync();
        if (existing.Count > 0)
            await WriteToFileAsync(existing);

        return true;
    }

    /// <summary>Forget the stored file handle (lets user pick a different file).</summary>
    public async Task ClearFileHandleAsync()
    {
        await js.InvokeVoidAsync("gymTracker.clearHandle");
        NeedsFileSetup = true;
    }

    // ── Public CRUD ──────────────────────────────────────────────────────────

    public async Task<List<Workout>> GetAllAsync()
    {
        if (IsFileApiSupported && !NeedsFileSetup)
            return await ReadFromFileAsync();

        return await ReadFromLocalStorageAsync();
    }

    public async Task SaveAsync(Workout workout)
    {
        var workouts = await GetAllAsync();
        var idx = workouts.FindIndex(w => w.Id == workout.Id);
        if (idx >= 0) workouts[idx] = workout;
        else workouts.Add(workout);
        await PersistAsync(workouts);
    }

    public async Task DeleteAsync(Guid id)
    {
        var workouts = await GetAllAsync();
        workouts.RemoveAll(w => w.Id == id);
        await PersistAsync(workouts);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task PersistAsync(List<Workout> workouts)
    {
        if (IsFileApiSupported && !NeedsFileSetup)
            await WriteToFileAsync(workouts);
        else
            await WriteToLocalStorageAsync(workouts);
    }

    private async Task<List<Workout>> ReadFromFileAsync()
    {
        var text = await js.InvokeAsync<string?>("gymTracker.readFile");
        if (string.IsNullOrWhiteSpace(text)) return [];
        try
        {
            var store = JsonSerializer.Deserialize<WorkoutStore>(text, _json);
            return store?.Workouts ?? [];
        }
        catch { return []; }
    }

    private async Task WriteToFileAsync(List<Workout> workouts)
    {
        var store = new WorkoutStore { Workouts = workouts };
        var json = JsonSerializer.Serialize(store, _json);
        await js.InvokeAsync<bool>("gymTracker.writeFile", json);
    }

    private async Task<List<Workout>> ReadFromLocalStorageAsync()
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", LocalStorageKey);
        if (string.IsNullOrEmpty(json)) return [];
        try { return JsonSerializer.Deserialize<List<Workout>>(json, _json) ?? []; }
        catch { return []; }
    }

    private async Task WriteToLocalStorageAsync(List<Workout> workouts)
    {
        var json = JsonSerializer.Serialize(workouts, _json);
        await js.InvokeVoidAsync("localStorage.setItem", LocalStorageKey, json);
    }
}

