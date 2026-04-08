using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace InternshipManagementSystem.Helpers
{
    public class MultiSessionCookieManager : ICookieManager
    {
        private readonly ICookieManager _baseManager;

        public MultiSessionCookieManager()
        {
            _baseManager = new ChunkingCookieManager();
        }

        private string GetSessionScopedKey(HttpContext context, string key)
        {
            // Extract 'sid' from path /sid/controller/action
            var path = context.Request.Path.Value?.Trim('/');
            if (string.IsNullOrEmpty(path)) return key;

            var segments = path.Split('/');
            var firstSegment = segments[0];

            // Only use as SID if it starts with 's' and followed by alphanumeric
            if (firstSegment.StartsWith("s") && firstSegment.Length > 1)
            {
                return $"{key}.{firstSegment}";
            }

            // Fallback to query string if not in path (useful for AJAX or initial login)
            var sid = context.Request.Query["sid"].ToString();
            if (!string.IsNullOrEmpty(sid))
            {
                return $"{key}.{sid}";
            }

            return key;
        }

        public string? GetRequestCookie(HttpContext context, string key)
        {
            return _baseManager.GetRequestCookie(context, GetSessionScopedKey(context, key));
        }

        public void AppendResponseCookie(HttpContext context, string key, string? value, CookieOptions options)
        {
            _baseManager.AppendResponseCookie(context, GetSessionScopedKey(context, key), value, options);
        }

        public void DeleteCookie(HttpContext context, string key, CookieOptions options)
        {
            _baseManager.DeleteCookie(context, GetSessionScopedKey(context, key), options);
        }
    }
}
