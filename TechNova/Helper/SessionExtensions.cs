using System.Text.Json;

namespace TechNova.Helper
{
    public static class SessionExtensions
    {
        private static readonly JsonSerializerOptions _opts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public static void SetObject<T>(this ISession session, string key, T value)
            => session.SetString(key, JsonSerializer.Serialize(value, _opts));

        public static T? GetObject<T>(this ISession session, string key)
        {
            var json = session.GetString(key);
            return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json, _opts);
        }
    }
}
