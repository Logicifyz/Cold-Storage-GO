using MySqlX.XDevAPI;

public static class CookieService
{
    public static void SetCookie(HttpContext httpContext, string key, string value, int expiryMinutes = 30)
    {
        httpContext.Response.Cookies.Append(key, value, new CookieOptions
        {
            HttpOnly = true,    // Make the cookie accessible only through HTTP requests// Ensure the cookie is only sent over HTTPS
            Secure = true,  // Only if you are not using HTTPS in local development
            SameSite = SameSiteMode.None,  // Prevent CSRF attacks
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes)  // Set an expiration time for the session
        });
    }
    public static void RemoveCookie(HttpContext httpContext, string key)
    {
        httpContext.Response.Cookies.Append(key, string.Empty, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,  // Ensure it only applies in secure environments
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(-1)  // Set expiration date in the past to remove the cookie
        });
    }
}
