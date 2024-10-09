using Newtonsoft.Json;

namespace Delete_Newline.Core.Helpers;

public static class Json
{
    public static async Task<T> ToObjectAsync<T>(string value)
    {
        return await Task.Run(() =>
        {
            T? output = JsonConvert.DeserializeObject<T>(value);
            return output ?? throw new JsonException("Deserialization failed or resulted in null");
        });
    }

    public static async Task<string> StringifyAsync(object value)
    {
        return await Task.Run<string>(() =>
        {
            return JsonConvert.SerializeObject(value);
        });
    }
}
