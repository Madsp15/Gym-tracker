namespace Gym_tracker.Models;

public enum ExerciseType
{
    RepsWeight,
    Timed
}

public class Exercise
{
    public string Name { get; set; } = string.Empty;
    public ExerciseType Type { get; set; } = ExerciseType.RepsWeight;

    // RepsWeight fields
    public int? Reps { get; set; }
    public double? WeightKg { get; set; }

    // Timed field
    public int? DurationSeconds { get; set; }
}

