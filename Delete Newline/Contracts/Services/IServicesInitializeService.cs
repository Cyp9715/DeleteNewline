namespace Delete_Newline.Contracts.Services;

public interface IServicesInitializeService
{
    int InitializeOrder { get; }
    void Initialize(IntPtr hwnd);
}
