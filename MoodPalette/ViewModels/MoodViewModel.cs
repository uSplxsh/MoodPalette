using MoodPalette.Models;
using MoodPalette.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using MoodPalette.ViewModels;
using Microsoft.Maui.Graphics;

namespace MoodPalette.ViewModels
{
    public class MoodViewModel : BaseViewModel
    {
        private DatabaseService _databaseService;

        public ObservableCollection<Mood> Moods { get; set; }
        public ObservableCollection<Color> AvailableColors { get; set; }

        private Color _selectedColor;
        public Color SelectedColor
        {
            get => _selectedColor;
            set => SetProperty(ref _selectedColor, value);
        }

        private Frame _selectedColorFrame;
        public Frame SelectedColorFrame
        {
            get => _selectedColorFrame;
            set => SetProperty(ref _selectedColorFrame, value);
        }

        private string _note;
        public string Note
        {
            get => _note;
            set => SetProperty(ref _note, value);
        }

        public async void LoadMoods()
        {
            var moods = await _databaseService.GetMoodsAsync();
            var tags = await _tagService.GetTagsAsync();

            Moods.Clear();
            foreach (var mood in moods)
            {
                var tag = tags.FirstOrDefault(t => t.Id == mood.TagId);
                mood.TagName = tag != null ? tag.Name : "Без тега";
                Moods.Add(mood);
            }
        }



        private Tag _selectedTag;
        public Tag SelectedTag
        {
            get => _selectedTag;
            set => SetProperty(ref _selectedTag, value);
        }

        public ObservableCollection<Tag> Tags { get; set; }

        public ICommand SaveMoodCommand { get; }
        public ICommand DeleteMoodCommand { get; }
        public ICommand SelectColorCommand { get; }

        private readonly TagService _tagService;

        public async void LoadTags()
        {
            var tags = await _tagService.GetTagsAsync();
            Tags.Clear();
            foreach (var tag in tags)
            {
                Tags.Add(tag);
            }
        }
        public MoodViewModel()
        {
            _databaseService = new DatabaseService();
            _tagService = new TagService();

            Moods = new ObservableCollection<Mood>();
            Tags = new ObservableCollection<Tag>();

            _databaseService = new DatabaseService();
            Moods = new ObservableCollection<Mood>();

            AvailableColors = new ObservableCollection<Color>
            {
                Color.FromArgb("#4CAF50"),
                Color.FromArgb("#8BC34A"),
                Color.FromArgb("#FFEB3B"),
                Color.FromArgb("#FF9800"),
                Color.FromArgb("#F44336")
            };

            SaveMoodCommand = new Command(OnSaveMood);
            DeleteMoodCommand = new Command<Mood>(OnDeleteMood);
            SelectColorCommand = new Command<Frame>(OnSelectColor);

            LoadMoods();
            LoadTags();
        }

        public async void RefreshTags()
        {
            var tags = await _tagService.GetTagsAsync();
            Tags.Clear();
            foreach (var tag in tags)
            {
                Tags.Add(tag);
            }
        }


        private void OnSelectColor(Frame colorFrame)
        {
            
            if (SelectedColorFrame != null)
            {
                SelectedColorFrame.Opacity = 1;
                SelectedColorFrame.BorderColor = Colors.Transparent;
            }

            
            SelectedColorFrame = colorFrame;
            SelectedColor = colorFrame.BackgroundColor;

            
            SelectedColorFrame.Opacity = 0.7;
            SelectedColorFrame.BorderColor = Colors.Black;
            SelectedColorFrame.HasShadow = true;
        }

        private async void OnSaveMood()
        {
            if (SelectedColor == null || string.IsNullOrEmpty(Note))
            {
                await Shell.Current.DisplayAlert("Помилка", "Будь ласка, виберіть колір та введіть нотатку", "OK");
                return;
            }

            var mood = new Mood
            {
                Color = SelectedColor.ToHex(),
                Note = Note,
                Date = DateTime.Now,
                TagId = SelectedTag?.Id ?? 0
            };

            await _databaseService.SaveMoodAsync(mood);
            Moods.Add(mood);

            // Оповестить другие ViewModel об изменении
            MessagingCenter.Send(this, "MoodSaved");

            SelectedColor = null;
            Note = string.Empty;
            SelectedTag = null;

            Console.WriteLine($"Сохранена запись: Color={mood.Color}, Note={mood.Note}, Date={mood.Date}, TagId={mood.TagId}");
        }

        private async void OnDeleteMood(Mood mood)
        {
            bool confirm = await Shell.Current.DisplayAlert("Видалити", "Ви впевнені, що хочете видалити цей запис?", "Так", "Ні");
            if (confirm)
            {
                await _databaseService.DeleteMoodAsync(mood);
                Moods.Remove(mood);
            }
        }
    }
}