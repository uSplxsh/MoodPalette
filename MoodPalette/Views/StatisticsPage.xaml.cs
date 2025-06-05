using Microcharts;
using MoodPalette.ViewModels;

namespace MoodPalette
{
    public partial class StatisticsPage : ContentPage
    {
        public StatisticsPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is StatisticsViewModel vm)
            {
                _ = vm.LoadMoodsAsync();
            }
        }
    }
}