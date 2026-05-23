namespace QLCongViec.Models
{
    public class StatisticsViewModel
    {
        public int TotalTasks { get; set; }

        public int NotStartedTasks { get; set; }

        public int InProgressTasks { get; set; }

        public int CompletedTasks { get; set; }

        public int HighPriorityTasks { get; set; }

        public int OverdueTasks { get; set; }

        public int LowPriorityTasks { get; set; }

        public int MediumPriorityTasks { get; set; }
    }
}