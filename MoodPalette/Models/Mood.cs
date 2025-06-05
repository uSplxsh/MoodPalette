using SQLite;

namespace MoodPalette.Models
{
    public class Mood
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Color { get; set; }
        public string Note { get; set; }
        public DateTime Date { get; set; }

        
        public int TagId { get; set; }
        [Ignore]
        public string TagName { get; set; }
    }
}