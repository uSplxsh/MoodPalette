using MoodPalette.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace MoodPalette.Services
{
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _database;

        public event Action OnDataChanged;

        public DatabaseService()
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "moods.db3");

            _database = new SQLiteAsyncConnection(dbPath);


            _database.CreateTableAsync<Mood>().Wait();
            _database.CreateTableAsync<Notification>().Wait();
        }

        // Mood
        public async Task<List<Mood>> GetMoodsAsync()
        {
            var moods = await _database.Table<Mood>().ToListAsync();
            foreach (var mood in moods)
            {
                Console.WriteLine($"Mood ID: {mood.Id}, Color: {mood.Color}, Note: {mood.Note}, Date: {mood.Date}");
            }
            return moods;
        }

        public async Task<int> SaveMoodAsync(Mood mood)
        {
            int result = await _database.InsertAsync(mood);
            OnDataChanged?.Invoke();
            return result;
        }

        public async Task<int> DeleteMoodAsync(Mood mood)
        {
            int result = await _database.DeleteAsync(mood);
            OnDataChanged?.Invoke();
            return result;
        }

        // Notification
        public async Task<List<Notification>> GetNotificationsAsync()
        {
            return await _database.Table<Notification>().ToListAsync();
        }

        public async Task<int> SaveNotificationAsync(Notification notification)
        {
            int result = await _database.InsertAsync(notification);
            OnDataChanged?.Invoke();
            return result;
        }

        public async Task<int> DeleteNotificationAsync(Notification notification)
        {
            int result = await _database.DeleteAsync(notification);
            OnDataChanged?.Invoke();
            return result;
        }
    }
}