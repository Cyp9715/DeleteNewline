using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace Delete_Newline.Core.Helpers;

public class PublicPropertiesOnlyContractResolver : DefaultContractResolver
{
    protected override List<MemberInfo> GetSerializableMembers(Type objectType)
    {
        // get all "public" members
        var members = objectType.GetMembers(BindingFlags.Instance | BindingFlags.Public);

        // select properties
        var properties = members
            .Where(m => m.MemberType == MemberTypes.Property)
            .OfType<PropertyInfo>()
            .Where(p => p.CanRead && p.CanWrite) // include CanRead && CanWrite
            .Cast<MemberInfo>()
            .ToList();

        return properties;
    }
}
