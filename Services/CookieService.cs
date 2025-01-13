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
}
