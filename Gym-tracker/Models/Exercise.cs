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

    /// <summary>Primary muscle group (e.g. "Chest", "Back"). Populated from wger or set manually.</summary>
    public string? MuscleGroup { get; set; }

    /// <summary>Number of sets to pre-populate on first use (before any session history).</summary>
    public int Sets { get; set; } = 3;

    // First-use defaults (used when LastSets is empty)
    public int? Reps { get; set; }
    public double? WeightKg { get; set; }
    public int? DurationSeconds { get; set; }

    // ── Last-session data (written back after each logged workout) ──
    public List<ExerciseSet> LastSets { get; set; } = new();
    public string? LastNotes { get; set; }
    public bool LastProgressiveOverload { get; set; }
    public DateTime? LastPerformed { get; set; }
}
