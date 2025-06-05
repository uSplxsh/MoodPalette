using Microcharts;
using MoodPalette.ViewModels;

namespace MoodPalette
{
    public partial class StatisticsPage : ContentPage
    {
        public StatisticsPage()
        {
            InitializeComponent();
            MessagingCenter.Subscribe<StatisticsViewModel>(this, "ChartUpdated", (sender) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Console.WriteLine("ChartView updated via ChartUpdated message");

                    ChartContainer.Content = null;
                    ChartContainer.Content = MoodChartView;

                    MoodChartView.Chart = sender.MoodChart;

                    MoodChartView.InvalidateSurface();
                });
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            MessagingCenter.Unsubscribe<StatisticsViewModel>(this, "ChartUpdated");
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            (BindingContext as MoodViewModel)?.RefreshTags();
        }
    }
}