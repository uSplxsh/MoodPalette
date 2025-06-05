using MoodPalette.Models;
using MoodPalette.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MoodPalette.ViewModels
{
    public class TagsViewModel : BaseViewModel
    {
        private TagService _tagService;

        public ObservableCollection<Tag> Tags { get; set; }

        private string _newTagName;
        public string NewTagName
        {
            get => _newTagName;
            set => SetProperty(ref _newTagName, value);
        }

        public ICommand AddTagCommand { get; }
        public ICommand DeleteTagCommand { get; }

        public TagsViewModel()
        {
            _tagService = new TagService();
            Tags = new ObservableCollection<Tag>();

            AddTagCommand = new Command(OnAddTag);
            DeleteTagCommand = new Command<Tag>(OnDeleteTag);

            LoadTags();
        }

        public async void LoadTags()
        {
            var tags = await _tagService.GetTagsAsync();
            Tags.Clear();
            foreach (var tag in tags)
            {
                Tags.Add(tag);
            }
        }

        private async void OnAddTag()
        {
            if (string.IsNullOrEmpty(NewTagName))
            {
                await Shell.Current.DisplayAlert("Помилка", "Будь ласка, введіть назву тегу", "OK");
                return;
            }

            var tag = new Tag { Name = NewTagName };
            await _tagService.SaveTagAsync(tag);
            Tags.Add(tag);

            NewTagName = string.Empty;
        }

        private async void OnDeleteTag(Tag tag)
        {
            bool confirm = await Shell.Current.DisplayAlert("Видалити", "Ви впевнені, що хочете видалити цей тег?", "Так", "Ні");
            if (confirm)
            {
                await _tagService.DeleteTagAsync(tag);
                Tags.Remove(tag);
            }
        }
    }
}