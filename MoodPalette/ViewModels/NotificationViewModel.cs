using Plugin.LocalNotification;
using MoodPalette.Models;
using MoodPalette.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MoodPalette.ViewModels
{
    public class NotificationsViewModel : BaseViewModel
    {
        private NotificationService _notificationService;
        private TagService _tagService;

        public ObservableCollection<Notification> Notifications { get; set; }
        public ObservableCollection<Tag> Tags { get; set; }

        private Tag _selectedTag;
        public Tag SelectedTag
        {
            get => _selectedTag;
            set => SetProperty(ref _selectedTag, value);
        }

        private TimeSpan _selectedTime;
        public TimeSpan SelectedTime
        {
            get => _selectedTime;
            set => SetProperty(ref _selectedTime, value);
        }

        public ICommand AddNotificationCommand { get; }
        public ICommand DeleteNotificationCommand { get; }

        public NotificationsViewModel()
        {
            _notificationService = new NotificationService();
            _tagService = new TagService();

            Notifications = new ObservableCollection<Notification>();
            Tags = new ObservableCollection<Tag>();

            AddNotificationCommand = new Command(OnAddNotification);
            DeleteNotificationCommand = new Command<Notification>(OnDeleteNotification);

            LoadNotifications();
            LoadTags();
        }

        private async void LoadNotifications()
        {
            var notifications = await _notificationService.GetNotificationsAsync();
            Notifications.Clear();
            foreach (var notification in notifications)
            {
                notification.Tag = await _tagService.GetTagByIdAsync(notification.TagId);
                Notifications.Add(notification);
            }
        }

        private async void LoadTags()
        {
            var tags = await _tagService.GetTagsAsync();
            Tags.Clear();
            foreach (var tag in tags)
            {
                Tags.Add(tag);
            }
        }

        private async void OnAddNotification()
        {
            if (SelectedTag == null)
            {
                await Shell.Current.DisplayAlert("Помилка", "Будь ласка, виберіть тег", "OK");
                return;
            }

            var notification = new Notification
            {
                TagId = SelectedTag.Id,
                Time = SelectedTime
            };

            await _notificationService.SaveNotificationAsync(notification);
            notification.Tag = SelectedTag;
            Notifications.Add(notification);

            SelectedTag = null;
            SelectedTime = TimeSpan.Zero;

            
            ScheduleNotification(notification);
        }

        private async void OnDeleteNotification(Notification notification)
        {
            bool confirm = await Shell.Current.DisplayAlert("Видалити", "Ви впевнені, що хочете видалити це сповіщення?", "Так", "Ні");
            if (confirm)
            {
                await _notificationService.DeleteNotificationAsync(notification);
                Notifications.Remove(notification);

                
                CancelNotification(notification);
            }
        }

        private void ScheduleNotification(Notification notification)
        {
            var notifyTime = DateTime.Now.AddMinutes(1);

            var request = new NotificationRequest
            {
                NotificationId = notification.Id,
                Title = "Нагадування",
                Description = notification.Tag.Name,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = notifyTime,
                    RepeatType = NotificationRepeat.Daily
                }
            };

            LocalNotificationCenter.Current.Show(request);
        }
        private void CancelNotification(Notification notification)
        {
            LocalNotificationCenter.Current.Cancel(notification.Id);
        }
    }
}