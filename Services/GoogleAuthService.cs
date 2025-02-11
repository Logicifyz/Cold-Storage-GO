using Google.Apis.Auth;
using System;
using System.Threading.Tasks;




namespace Cold_Storage_GO.Services
{
    public class GoogleAuthService
{
    private readonly string _clientId = "869557804479-pv18rpo94fbpd6hatmns6m4nes5adih8.apps.googleusercontent.com"; // Replace with your actual Google client ID

    // Method to verify and decode the Google ID token
    public async Task<GoogleJsonWebSignature.Payload> VerifyGoogleTokenAsync(string idToken)
    {
        try
        {
            // Validate the ID token and get the payload
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _clientId }  // Verify that the token was meant for your client
            });

            // Return the decoded payload
            return payload;
        }
        catch (Exception ex)
        {
            // Handle error if token validation fails
            throw new Exception("Invalid Google token", ex);
        }
    }
}
}
