using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace IV.ManagementHub.ApiService.Security
{
    public sealed class RootTokenService(RootAuthOptions options)
    {
        public RootAccessToken CreateAccessToken(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException("User name is required.", nameof(userName));
            }

            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.AddMinutes(options.AccessTokenMinutes);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userName),
                new(JwtRegisteredClaimNames.UniqueName, userName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new("role", AuthRoles.Root)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var jwt = new JwtSecurityToken(
                issuer: options.Issuer,
                audience: options.Audience,
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: expiresAt.UtcDateTime,
                signingCredentials: credentials);

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);
            var expiresInSeconds = (int)Math.Max(1, (expiresAt - now).TotalSeconds);

            return new RootAccessToken(token, expiresInSeconds);
        }
    }

    public sealed record RootAccessToken(string Token, int ExpiresInSeconds);
}
