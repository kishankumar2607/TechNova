using System.Text.Json;

namespace TechNova.Helper
{
    // Session helpers to save and read any object in/from session
    public static class SessionExtensions
    {
        // Reusable JSON settings: camelCase keys, compact output
        private static readonly JsonSerializerOptions _opts = new JsonSerializerOptions
        {
            // use camelCase for property names
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // no extra spaces/new lines
            WriteIndented = false
        };

        // Save an object to session as JSON at the given key
        public static void SetObject<T>(this ISession session, string key, T value)
            => session.SetString(key, JsonSerializer.Serialize(value, _opts));

        // Get an object from session by key (or default if missing)
        public static T? GetObject<T>(this ISession session, string key)
        {
            // read the JSON string from session
            var json = session.GetString(key);
            // if empty return default, else convert JSON back to the object
            return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json, _opts);
        }
    }
}
