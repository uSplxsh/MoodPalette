using Android.App;
using Android.Content.PM;
using Android.OS;
using Plugin.LocalNotification;
using Microsoft.Maui.ApplicationModel;

namespace MoodPalette;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override async void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);


        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            if (status != PermissionStatus.Granted)
            {
                await Permissions.RequestAsync<Permissions.PostNotifications>();
            }
        }
        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            var readStatus = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
            if (readStatus != PermissionStatus.Granted)
            {
                await Permissions.RequestAsync<Permissions.StorageRead>();
            }

            var writeStatus = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
            if (writeStatus != PermissionStatus.Granted)
            {
                await Permissions.RequestAsync<Permissions.StorageWrite>();
            }
        }
    }
}