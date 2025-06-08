using Delete_Newline.Core.Contracts.Services;

namespace Delete_Newline.Core.Services;

public class FileService : IFileService
{
    public async Task<string> ReadAsStringAsync(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);
        if (!File.Exists(path))
            return string.Empty;
        return await File.ReadAllTextAsync(path).ConfigureAwait(false);
    }

    public async Task SaveAsync(string directory, string fileName, string content)
    {
        var filePath = Path.Combine(directory, fileName);
        await File.WriteAllTextAsync(filePath, content).ConfigureAwait(false);
    }

    public void Delete(string folderPath, string fileName)
    {
        var path = Path.Combine(folderPath, fileName);

        if (File.Exists(path) == false)
            throw new FileNotFoundException($"The file '{fileName}' does not exist in the folder '{folderPath}'.", path);

        File.Delete(path);
    }
}