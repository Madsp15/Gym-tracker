namespace Gym_tracker.Models;

/// <summary>
/// A single set within a logged exercise.
/// Stores actual performance data (reps/weight for RepsWeight, duration for Timed).
/// </summary>
public class ExerciseSet
{
    // RepsWeight fields
    public int? Reps { get; set; }
    public double? WeightKg { get; set; }

    // Timed field
    public int? DurationSeconds { get; set; }
}

