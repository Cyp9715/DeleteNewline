using WinRT.Interop;
using Delete_Newline.Core.Contracts.Services;
using Windows.Storage.Pickers;
using Windows.Storage;

public class FilePickerService : IFilePickerService
{
    private IntPtr _hwnd;

    public void Initialize(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    public async Task<string?> PickSaveFileAsync()
    {
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            DefaultFileExtension = ".json"
        };
        picker.FileTypeChoices.Add("JSON File", new List<string> { ".json" });

        InitializeWithWindow.Initialize(picker, _hwnd);

        StorageFile? file = await picker.PickSaveFileAsync();
        return file?.Path;
    }

    public async Task<string?> PickOpenFileAsync()
    {
        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.List,
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add(".json");

        InitializeWithWindow.Initialize(picker, _hwnd);

        StorageFile? file = await picker.PickSingleFileAsync();
        return file?.Path;
    }
}