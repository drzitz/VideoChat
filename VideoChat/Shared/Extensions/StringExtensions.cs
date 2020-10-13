using Newtonsoft.Json;

namespace VideoChat.Shared.Extensions
{
    public static class StringExtensions
    {
        public static T FromJson<T>(this string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
