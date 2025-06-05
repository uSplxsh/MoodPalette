using Microcharts;
using MoodPalette.Models;
using MoodPalette.Services;
using System.Collections.ObjectModel;
using SkiaSharp;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace MoodPalette.ViewModels
{
    public enum TimePeriod
    {
        Day,
        Week,
        Month
    }

    public class TimePeriodItem
    {
        public TimePeriod Period { get; set; }
        public string DisplayName { get; set; }
    }

    public class StatisticsViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly TagService _tagService;
        private bool _isLoadingMoods;

        public ObservableCollection<Mood> Moods { get; } = new ObservableCollection<Mood>();
        public ObservableCollection<ColorTile> ColorTiles { get; } = new ObservableCollection<ColorTile>();
        public ObservableCollection<Tag> Tags { get; } = new ObservableCollection<Tag>();
        public ObservableCollection<TimePeriodItem> TimePeriods { get; } = new ObservableCollection<TimePeriodItem>
        {
            new TimePeriodItem { Period = TimePeriod.Day, DisplayName = "День" },
            new TimePeriodItem { Period = TimePeriod.Week, DisplayName = "Тиждень" },
            new TimePeriodItem { Period = TimePeriod.Month, DisplayName = "Місяць" }
        };

        private DateTime _selectedDate = DateTime.Now;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value) && !_isLoadingMoods)
                {
                    Task.Run(LoadMoodsAsync);
                }
            }
        }

        private TimePeriodItem _selectedTimePeriod;
        public TimePeriodItem SelectedTimePeriod
        {
            get => _selectedTimePeriod;
            set
            {
                if (SetProperty(ref _selectedTimePeriod, value) && !_isLoadingMoods)
                {
                    Task.Run(LoadMoodsAsync);
                }
            }
        }

        private Tag _selectedFilterTag;
        public Tag SelectedFilterTag
        {
            get => _selectedFilterTag;
            set
            {
                if (SetProperty(ref _selectedFilterTag, value) && !_isLoadingMoods)
                {
                    Task.Run(LoadMoodsAsync);
                }
            }
        }

        private bool _isFilterByTagEnabled = false;
        public bool IsFilterByTagEnabled
        {
            get => _isFilterByTagEnabled;
            set
            {
                if (SetProperty(ref _isFilterByTagEnabled, value) && !_isLoadingMoods)
                {
                    Task.Run(LoadMoodsAsync);
                }
            }
        }

        private Chart _moodChart;
        public Chart MoodChart
        {
            get => _moodChart;
            private set
            {
                if (SetProperty(ref _moodChart, value))
                {
                    ChartKey = Guid.NewGuid().ToString();
                    OnPropertyChanged(nameof(MoodChart));
                }
            }
        }

        private string _chartKey = Guid.NewGuid().ToString();
        public string ChartKey
        {
            get => _chartKey;
            private set => SetProperty(ref _chartKey, value);
        }

        public string PeriodDisplayText => SelectedTimePeriod?.Period switch
        {
            TimePeriod.Day => SelectedDate.ToString("dd.MM.yyyy"),
            TimePeriod.Week => GetWeekRangeText(SelectedDate),
            TimePeriod.Month => SelectedDate.ToString("MMMM yyyy"),
            _ => string.Empty
        };

        public string ChartTitle => SelectedTimePeriod?.Period switch
        {
            TimePeriod.Day => $"Настрій за {SelectedDate:dd.MM.yyyy}",
            TimePeriod.Week => $"Настрій за тиждень {GetWeekRangeText(SelectedDate)}",
            TimePeriod.Month => $"Настрій за {SelectedDate:MMMM yyyy}",
            _ => "Кругова діаграма настрою"
        };

        public bool ShowDateNavigation => SelectedTimePeriod?.Period is TimePeriod.Day or TimePeriod.Week;
        public bool ShowMonthNavigation => SelectedTimePeriod?.Period == TimePeriod.Month;
        public bool ShowColorTiles => SelectedTimePeriod?.Period == TimePeriod.Month;

        public ICommand PreviousPeriodCommand { get; }
        public ICommand NextPeriodCommand { get; }
        public ICommand PreviousMonthCommand { get; }
        public ICommand NextMonthCommand { get; }
        public ICommand ShowMoodDetailsCommand { get; }

        public StatisticsViewModel()
        {
            _databaseService = new DatabaseService();
            _tagService = new TagService();

            SelectedTimePeriod = TimePeriods.FirstOrDefault(p => p.Period == TimePeriod.Month);

            PreviousPeriodCommand = new Command(OnPreviousPeriod);
            NextPeriodCommand = new Command(OnNextPeriod);
            PreviousMonthCommand = new Command(OnPreviousMonth);
            NextMonthCommand = new Command(OnNextMonth);
            ShowMoodDetailsCommand = new Command<ColorTile>(OnShowMoodDetails);

            MessagingCenter.Subscribe<MoodViewModel>(this, "MoodSaved", async (sender) =>
            {
                if (!_isLoadingMoods) await LoadMoodsAsync();
            });

            Task.Run(async () =>
            {
                await LoadTagsAsync();
                await LoadMoodsAsync();
            });
        }

        private string GetWeekRangeText(DateTime date)
        {
            var startOfWeek = date.StartOfWeek(DayOfWeek.Monday);
            var endOfWeek = startOfWeek.AddDays(6);
            return $"{startOfWeek:dd.MM} - {endOfWeek:dd.MM.yyyy}";
        }

        public async Task LoadMoodsAsync()
        {
            if (_isLoadingMoods) return;

            _isLoadingMoods = true;
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MoodChart = null;
                    ColorTiles.Clear();
                });

                var allMoods = await _databaseService.GetMoodsAsync();
                var filteredMoods = FilterMoodsByPeriodAndTag(allMoods);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Moods.Clear();
                    foreach (var mood in filteredMoods)
                    {
                        Moods.Add(mood);
                    }

                    GenerateChart();
                    GenerateColorTiles();
                });
            }
            finally
            {
                _isLoadingMoods = false;
            }
        }

        private List<Mood> FilterMoodsByPeriodAndTag(IEnumerable<Mood> moods)
        {
            return moods.Where(m =>
            {
                bool periodMatch = SelectedTimePeriod?.Period switch
                {
                    TimePeriod.Day => m.Date.Date == SelectedDate.Date,
                    TimePeriod.Week => m.Date.IsInSameWeek(SelectedDate),
                    TimePeriod.Month => m.Date.Month == SelectedDate.Month && m.Date.Year == SelectedDate.Year,
                    _ => true
                };

                bool tagMatch = !IsFilterByTagEnabled || SelectedFilterTag == null || m.TagId == SelectedFilterTag.Id;

                return periodMatch && tagMatch;
            }).ToList();
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

        private void GenerateChart()
        {
            var periodMoods = Moods.ToList();
            if (!periodMoods.Any())
            {
                MoodChart = null;
                return;
            }

            var moodColors = new Dictionary<string, string>
            {
                { "#4CAF50", "Чудово" },
                { "#8BC34A", "Добре" },
                { "#FFEB3B", "Нормально" },
                { "#FF9800", "Погано" },
                { "#F44336", "Жахливо" }
            };

            var entries = periodMoods
                .GroupBy(m => m.Color?.ToUpperInvariant() ?? "#FFFFFF")
                .Select(g =>
                {
                    string colorHex = g.Key;
                    string label = moodColors.TryGetValue(colorHex, out var name) ? name : "Інше";
                    float percentage = (float)g.Count() / periodMoods.Count * 100;

                    return new ChartEntry((float)g.Count())
                    {
                        Label = label,
                        ValueLabel = $"{percentage:F1}% ({g.Count()})",
                        Color = SKColor.Parse(colorHex)
                    };
                })
                .OrderByDescending(e => e.Value)
                .ToList();

            MoodChart = new PieChart
            {
                Entries = entries,
                LabelTextSize = 16,
                BackgroundColor = SKColors.White,
                IsAnimated = true
            };
        }

        private void GenerateColorTiles()
        {
            if (SelectedTimePeriod?.Period != TimePeriod.Month)
            {
                ColorTiles.Clear();
                OnPropertyChanged(nameof(ColorTiles));
                return;
            }

            var daysInMonth = DateTime.DaysInMonth(SelectedDate.Year, SelectedDate.Month);
            var newTiles = new ObservableCollection<ColorTile>();

            for (int day = 1; day <= daysInMonth; day++)
            {
                var dateForDay = new DateTime(SelectedDate.Year, SelectedDate.Month, day);
                var moodsOfDay = Moods.Where(m => m.Date.Date == dateForDay.Date).ToList();
                var dominantMood = moodsOfDay
                    .GroupBy(m => m.Color?.ToUpperInvariant() ?? "#FFFFFF")
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();

                var tile = new ColorTile
                {
                    Day = day,
                    Color = dominantMood != null ? Color.FromHex(dominantMood.Key) : Colors.Transparent,
                    MoodsOfDay = moodsOfDay
                };

                newTiles.Add(tile);
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ColorTiles.Clear();
                foreach (var tile in newTiles)
                {
                    ColorTiles.Add(tile);
                }
                OnPropertyChanged(nameof(ColorTiles));
                OnPropertyChanged(nameof(ShowColorTiles));
            });
        }

        private void OnPreviousPeriod()
        {
            if (SelectedTimePeriod == null) return;

            SelectedDate = SelectedTimePeriod.Period switch
            {
                TimePeriod.Day => SelectedDate.AddDays(-1),
                TimePeriod.Week => SelectedDate.AddDays(-7),
                _ => SelectedDate
            };
        }

        private void OnNextPeriod()
        {
            if (SelectedTimePeriod == null) return;

            SelectedDate = SelectedTimePeriod.Period switch
            {
                TimePeriod.Day => SelectedDate.AddDays(1),
                TimePeriod.Week => SelectedDate.AddDays(7),
                _ => SelectedDate
            };
        }

        private void OnPreviousMonth() => SelectedDate = SelectedDate.AddMonths(-1);
        private void OnNextMonth() => SelectedDate = SelectedDate.AddMonths(1);

        private async void OnShowMoodDetails(ColorTile tile)
        {
            if (tile?.MoodsOfDay == null || tile.MoodsOfDay.Count == 0) return;

            var moodLabels = new Dictionary<string, string>
            {
                { "#4CAF50", "Чудово" },
                { "#8BC34A", "Добре" },
                { "#FFEB3B", "Нормально" },
                { "#FF9800", "Погано" },
                { "#F44336", "Жахливо" },
                { "#FFFFFF", "Немає даних" }
            };

            var details = tile.MoodsOfDay
                .GroupBy(m => m.Color?.ToUpperInvariant() ?? "#FFFFFF")
                .Select(g =>
                {
                    var label = moodLabels.TryGetValue(g.Key, out var name) ? name : "Інше";
                    return $"{label}: {g.Count()}";
                });

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Application.Current.MainPage.DisplayAlert(
                    $"Статистика на {tile.Day}.{SelectedDate:MM.yyyy}",
                    string.Join("\n", details),
                    "OK");
            });
        }
    }

    public static class DateTimeExtensions
    {
        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        public static bool IsInSameWeek(this DateTime date, DateTime referenceDate)
        {
            var startOfWeek = referenceDate.StartOfWeek(DayOfWeek.Monday);
            var endOfWeek = startOfWeek.AddDays(6);
            return date.Date >= startOfWeek && date.Date <= endOfWeek;
        }
    }

    public class ColorTile
    {
        public int Day { get; set; }
        public Color Color { get; set; }
        public List<Mood> MoodsOfDay { get; set; } = new List<Mood>();
    }
}
