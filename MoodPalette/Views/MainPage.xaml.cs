using MoodPalette.ViewModels;
namespace MoodPalette
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            (BindingContext as MoodViewModel)?.RefreshTags();
        }
    }

}
