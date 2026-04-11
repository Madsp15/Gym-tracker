namespace Gym_tracker.Models;

/// <summary>
/// Root storage container for all workout data.
/// </summary>
public class WorkoutStore
{
    public int Version { get; set; } = 1;
    
    /// <summary>Reusable workout templates (name + exercise list).</summary>
    public List<WorkoutTemplate> Templates { get; set; } = new();
    
    /// <summary>Logged workout instances with performance data (date + exercises with sets).</summary>
    public List<LoggedWorkout> LoggedWorkouts { get; set; } = new();
}

