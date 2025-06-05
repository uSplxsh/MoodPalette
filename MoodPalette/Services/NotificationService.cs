using MoodPalette.Models;
using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoodPalette.Services
{
    public class NotificationService
    {
        private SQLiteAsyncConnection _database;

        public NotificationService()
        {
            _database = new SQLiteAsyncConnection(Path.Combine(FileSystem.AppDataDirectory, "notifications.db3"));
            _database.CreateTableAsync<Notification>().Wait();
        }

        public Task<List<Notification>> GetNotificationsAsync()
        {
            return _database.Table<Notification>().ToListAsync();
        }

        public Task<int> SaveNotificationAsync(Notification notification)
        {
            return _database.InsertAsync(notification);
        }

        public Task<int> DeleteNotificationAsync(Notification notification)
        {
            return _database.DeleteAsync(notification);
        }
    }
}