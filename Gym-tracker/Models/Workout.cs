namespace Gym_tracker.Models;

public class Workout
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
    public List<Exercise> Exercises { get; set; } = new();
}

