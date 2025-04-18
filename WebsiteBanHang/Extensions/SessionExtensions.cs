using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace WebsiteBanHang.Extensions
{
    public static class SessionExtensions
    {
        // Lưu đối tượng vào session dưới dạng JSON
        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        // Lấy đối tượng từ session, giải mã từ JSON
        public static T? Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
} 