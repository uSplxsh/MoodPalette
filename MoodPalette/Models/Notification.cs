using SQLite;

namespace MoodPalette.Models
{
    public class Notification
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int TagId { get; set; }
        public TimeSpan Time { get; set; }

        [Ignore]
        public Tag Tag { get; set; }
    }
}