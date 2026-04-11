namespace Gym_tracker.Models;

/// <summary>
/// A reusable workout template containing a list of exercises.
/// Users can create logged workouts from templates.
/// </summary>
public class WorkoutTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public List<Exercise> Exercises { get; set; } = new();
}

