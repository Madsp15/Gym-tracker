namespace Gym_tracker.Models;

/// <summary>
/// A logged workout instance with actual performance data.
/// Independent of templates; represents a completed workout on a specific date.
/// </summary>
public class LoggedWorkout
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
    public List<LoggedExercise> Exercises { get; set; } = new();
    /// <summary>Set when this workout was logged from a template; enables write-back on save.</summary>
    public Guid? TemplateId { get; set; }
    /// <summary>Total session duration in minutes, recorded by the stopwatch while logging.</summary>
    public int? DurationMinutes { get; set; }
}

