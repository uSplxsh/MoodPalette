using MoodPalette.Models;
using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoodPalette.Services
{
    public class TagService
    {
        private SQLiteAsyncConnection _database;

        public TagService()
        {
            _database = new SQLiteAsyncConnection(Path.Combine(FileSystem.AppDataDirectory, "tags.db3"));
            _database.CreateTableAsync<Tag>().Wait();
        }


        public Task<List<Tag>> GetTagsAsync()
        {
            return _database.Table<Tag>().ToListAsync();
        }


        public async Task<Tag> GetTagByIdAsync(int tagId)
        {
            return await _database.Table<Tag>().FirstOrDefaultAsync(t => t.Id == tagId);
        }


        public Task<int> SaveTagAsync(Tag tag)
        {
            return _database.InsertAsync(tag);
        }


        public Task<int> DeleteTagAsync(Tag tag)
        {
            return _database.DeleteAsync(tag);
        }
    }
}