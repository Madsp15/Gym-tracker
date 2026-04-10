namespace Gym_tracker.Models;

public class WorkoutStore
{
    public int Version { get; set; } = 1;
    public List<Workout> Workouts { get; set; } = new();
}

