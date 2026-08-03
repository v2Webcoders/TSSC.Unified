namespace QUIZAPP.ViewModel
{
    public class DashboardViewModel
    {
        public int TwoWheelerCount { get; set; }
        public int FourWheelerCount { get; set; }

        public int TwoWheelerToday { get; set; }
        public int FourWheelerToday { get; set; }

        public List<DateWiseEntryVM> DateWiseEntries { get; set; }
        // ⭐ Add these new lists
        public List<StateWiseEntryVM> StateWise { get; set; }
        public List<TopEntryVM> TopTwoWheelerEntries { get; set; }
        public List<TopEntryVM> TopFourWheelerEntries { get; set; }
    }
    public class DateWiseEntryVM
    {
        public DateTime Date { get; set; }
        public int TwoWheelerCount { get; set; }
        public int FourWheelerCount { get; set; }

        public int Total => TwoWheelerCount + FourWheelerCount;
    }

    // ⭐ NEW Class
    public class StateWiseEntryVM
    {
        public string State { get; set; }
        public int TwoWheelerCount { get; set; }
        public int FourWheelerCount { get; set; }

        public int Total => TwoWheelerCount + FourWheelerCount;
    }
    public class TopEntryVM
    {
        public string State { get; set; }
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

}
