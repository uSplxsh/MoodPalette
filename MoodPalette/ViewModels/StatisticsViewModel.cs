using MoodPalette.Models;
using MoodPalette.Services;
using System.Collections.ObjectModel;
using Microcharts;
using SkiaSharp;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;

namespace MoodPalette.ViewModels
{
    public class StatisticsViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly TagService _tagService;
        private bool _isLoadingMoods;

        public ObservableCollection<Mood> Moods { get; set; }
        public ObservableCollection<ColorTile> ColorTiles { get; set; }
        public ObservableCollection<Tag> Tags { get; set; }

        private DateTime _selectedDate = DateTime.Now;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                SetProperty(ref _selectedDate, value);
                if (!_isLoadingMoods)
                {
                    Task.Run(async () => await LoadMoodsAsync());
                }
            }
        }

        private Tag _selectedFilterTag;
        public Tag SelectedFilterTag
        {
            get => _selectedFilterTag;
            set
            {
                SetProperty(ref _selectedFilterTag, value);
                if (!_isLoadingMoods)
                {
                    Task.Run(async () => await LoadMoodsAsync());
                }
            }
        }

        private bool _isFilterByTagEnabled = true;
        public bool IsFilterByTagEnabled
        {
            get => _isFilterByTagEnabled;
            set
            {
                SetProperty(ref _isFilterByTagEnabled, value);
                if (!_isLoadingMoods)
                {
                    Task.Run(async () => await LoadMoodsAsync());
                }
            }
        }

        public Chart MoodChart { get; set; }

        public ICommand PreviousMonthCommand { get; }
        public ICommand NextMonthCommand { get; }
        public ICommand ShowMoodDetailsCommand { get; }

        public StatisticsViewModel()
        {
            _databaseService = new DatabaseService();
            _tagService = new TagService();

            Moods = new ObservableCollection<Mood>();
            ColorTiles = new ObservableCollection<ColorTile>();
            Tags = new ObservableCollection<Tag>();

            PreviousMonthCommand = new Command(OnPreviousMonth);
            NextMonthCommand = new Command(OnNextMonth);
            ShowMoodDetailsCommand = new Command<ColorTile>(OnShowMoodDetails);

            MessagingCenter.Subscribe<MoodViewModel>(this, "MoodSaved", async (sender) =>
            {
                if (!_isLoadingMoods)
                {
                    await LoadMoodsAsync();
                }
            });

            Task.Run(async () =>
            {
                await LoadTagsAsync();
                await LoadMoodsAsync();
            });
        }

        public async Task LoadMoodsAsync()
        {
            if (_isLoadingMoods)
                return;

            _isLoadingMoods = true;
            try
            {
                MoodChart = null;
                OnPropertyChanged(nameof(MoodChart));

                var moods = await _databaseService.GetMoodsAsync();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Moods.Clear();
                    foreach (var mood in moods)
                    {
                        bool isInMonth = mood.Date.Month == SelectedDate.Month && mood.Date.Year == SelectedDate.Year;
                        bool isTagMatch = !IsFilterByTagEnabled || SelectedFilterTag == null || mood.TagId == SelectedFilterTag.Id;

                        if (isInMonth && isTagMatch)
                        {
                            Moods.Add(mood);
                        }
                    }

                    GenerateChart();
                    GenerateColorTiles();
                });
            }
            finally
            {
                _isLoadingMoods = false;
                MessagingCenter.Send(this, "ChartUpdated");
            }
        }

        public async Task LoadTagsAsync()
        {
            var tags = await _tagService.GetTagsAsync();
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Tags.Clear();
                foreach (var tag in tags)
                {
                    Tags.Add(tag);
                }
            });
        }

        public void GenerateChart()
        {
            MoodChart = null;
            OnPropertyChanged(nameof(MoodChart));

            var monthMoods = Moods
                .Where(m => m.Date.Month == SelectedDate.Month && m.Date.Year == SelectedDate.Year)
                .ToList();

            if (!monthMoods.Any())
                return;

            var moodColors = new Dictionary<string, string>
            {
                { "#4CAF50", "Чудово" },
                { "#8BC34A", "Добре" },
                { "#FFEB3B", "Нормально" },
                { "#FF9800", "Погано" },
                { "#F44336", "Жахливо" }
            };

            var entries = monthMoods
                .GroupBy(m => m.Color)
                .Select(g =>
                {
                    string colorHex = g.Key.ToUpper();
                    string label = moodColors.ContainsKey(colorHex) ? moodColors[colorHex] : "Інше";

                    return new ChartEntry(g.Count())
                    {
                        Label = label,
                        ValueLabel = g.Count().ToString(),
                        Color = SKColor.Parse(colorHex)
                    };
                })
                .ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MoodChart = new PieChart
                {
                    Entries = entries,
                    LabelTextSize = 30,
                    BackgroundColor = SKColors.Transparent
                };

                OnPropertyChanged(nameof(MoodChart));
            });
        }

        public void GenerateColorTiles()
        {
            ColorTiles.Clear();
            var daysInMonth = DateTime.DaysInMonth(SelectedDate.Year, SelectedDate.Month);

            for (int day = 1; day <= daysInMonth; day++)
            {
                var moodsOfDay = Moods.Where(m => m.Date.Day == day).ToList();

                ColorTiles.Add(new ColorTile
                {
                    Day = day,
                    Color = moodsOfDay.FirstOrDefault() != null ? Color.FromHex(moodsOfDay.First().Color) : Colors.Transparent,
                    MoodsOfDay = moodsOfDay
                });
            }

            OnPropertyChanged(nameof(ColorTiles));
        }

        private void OnPreviousMonth()
        {
            SelectedDate = SelectedDate.AddMonths(-1);
        }

        private void OnNextMonth()
        {
            SelectedDate = SelectedDate.AddMonths(1);
        }

        private async void OnShowMoodDetails(ColorTile tile)
        {
            if (tile?.MoodsOfDay == null || tile.MoodsOfDay.Count == 0)
                return;

            var grouped = tile.MoodsOfDay
                .GroupBy(m => m.Color.ToUpper())
                .ToDictionary(g => g.Key, g => g.Count());

            var moodLabels = new Dictionary<string, string>
            {
                { "#4CAF50", "Чудово" },
                { "#8BC34A", "Добре" },
                { "#FFEB3B", "Нормально" },
                { "#FF9800", "Погано" },
                { "#F44336", "Жахливо" }
            };

            var details = grouped
                .Select(kvp =>
                {
                    var label = moodLabels.TryGetValue(kvp.Key, out var name) ? name : "Інше";
                    return $"{label}: {kvp.Value}";
                })
                .ToList();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Application.Current.MainPage.DisplayAlert(
                    $"Статистика на {tile.Day}.{SelectedDate:MM.yyyy}",
                    string.Join("\n", details),
                    "OK");
            });
        }
    }

    public class ColorTile
    {
        public int Day { get; set; }
        public Color Color { get; set; }
        public List<Mood> MoodsOfDay { get; set; } = new();
    }
}
