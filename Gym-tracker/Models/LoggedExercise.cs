namespace Gym_tracker.Models;

/// <summary>
/// An exercise instance within a logged workout.
/// Tracks multiple sets of actual performance data.
/// </summary>
public class LoggedExercise
{
    public string Name { get; set; } = string.Empty;
    public ExerciseType Type { get; set; } = ExerciseType.RepsWeight;
    
    /// <summary>List of sets completed for this exercise.</summary>
    public List<ExerciseSet> Sets { get; set; } = new();
    public string? Notes { get; set; }
    public bool ProgressiveOverload { get; set; }
}
