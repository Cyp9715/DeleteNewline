
namespace Delete_Newline.Contracts.Services
{
    public interface INotificationService
    {
        Task InitializeAsync();
        void ShowNotification(string title, string message, bool force = false, bool tag = true);

        Task SetEnableNotificationAsync(bool enable);
        bool GetEnableNotification();
    }
}
