using Newtonsoft.Json;

namespace VideoChat.Shared.Extensions
{
    public static class ObjectExtensions
    {
        public static string ToJson(this object obj, bool indented = true)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = indented ? Formatting.Indented : Formatting.None
            };

            return JsonConvert.SerializeObject(obj, settings);
        }
    }
}
