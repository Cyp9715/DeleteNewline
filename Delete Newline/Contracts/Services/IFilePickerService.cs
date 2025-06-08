namespace Delete_Newline.Core.Contracts.Services;

public interface IFilePickerService
{
    public void Initialize(IntPtr hwnd);

    public Task<string?> PickSaveFileAsync();

    public Task<string?> PickOpenFileAsync();
}
