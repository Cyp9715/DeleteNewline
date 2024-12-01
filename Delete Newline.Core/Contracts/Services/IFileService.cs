namespace Delete_Newline.Core.Contracts.Services;

public interface IFileService
{
    Task<string> ReadAsStringAsync(string directory, string fileName);

    Task SaveAsync(string directory, string fileName, string content);

    void Delete(string folderPath, string fileName);
}
