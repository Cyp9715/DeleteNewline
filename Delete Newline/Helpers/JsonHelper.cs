using Newtonsoft.Json;

namespace Delete_Newline.Core.Helpers;

public static class JsonHelper
{
    public static Task<T> DeserializeAsync<T>(string value, JsonSerializerSettings? settings = null)
    {
        return Task.Run(() =>
        {
            settings ??= new JsonSerializerSettings
            {
                ContractResolver = new PublicPropertiesOnlyContractResolver(),
                ObjectCreationHandling = ObjectCreationHandling.Replace,
            };

            T? output = JsonConvert.DeserializeObject<T>(value, settings);
            return output ?? throw new JsonException("Deserialization failed or resulted in null");
        });
    }

    public static Task<string> SerializeAsync(object value, JsonSerializerSettings? settings = null)
    {
        settings ??= new JsonSerializerSettings
        {
            ContractResolver = new PublicPropertiesOnlyContractResolver(),
            Formatting = Formatting.Indented,
        };


        var deepCopy = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(value, settings), settings);

        return Task.Run(() =>
        {
            return JsonConvert.SerializeObject(deepCopy, settings);
        });
    }
}
