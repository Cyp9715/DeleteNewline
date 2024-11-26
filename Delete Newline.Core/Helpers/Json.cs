using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Delete_Newline.Core.Helpers;

public static class Json
{
    public static async Task<T> ToObjectAsync<T>(string value, JsonSerializerSettings? settings = null)
    {
        return await Task.Run(() =>
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

    public static async Task<string> StringifyAsync(object value, JsonSerializerSettings? settings = null)
    {
        settings ??= new JsonSerializerSettings
        {
            ContractResolver = new PublicPropertiesOnlyContractResolver(),
            Formatting = Formatting.Indented,
        };

        /*
         * The reason for performing a DeepCopy in this code is due to the repeated calls 
         * to Add and Remove on the regexChains variable during the Drag&Drop swap between GridView items. 
         * This causes a System.InvalidOperationException: 'Collection was modified; enumeration operation may not execute.' error.
         * While it is possible to indirectly ensure thread safety by using await Task.Delay(100), I judged this to be a temporary workaround. 
         * 
         * Therefore, I decided to use DeepCopy, even though it consumes more resources.
         * However, this is not the best solution either. 
         * The optimal solution would be to link the Swap method during the Drag&Drop operation in the GridView.
         */

        var deepCopy = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(value, settings), settings);

        return await Task.Run(() =>
        {
            return JsonConvert.SerializeObject(deepCopy, settings);
        });
    }
}
