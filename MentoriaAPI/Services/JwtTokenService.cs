using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MentoriaAPI.Models;
using Microsoft.IdentityModel.Tokens;

namespace MentoriaAPI.Services
{
	public class JwtTokenService
	{
		private readonly IConfiguration _configuration;

		public JwtTokenService(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public (string Token, DateTime ExpiresAt) GenerateToken(User user, IEnumerable<string> roles)
		{
			var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured.");
			var issuer = _configuration["Jwt:Issuer"] ?? "MentoriaAPI";
			var audience = _configuration["Jwt:Audience"] ?? "MentoriaAPI.Client";
			var expiresMinutes = int.TryParse(_configuration["Jwt:ExpiresMinutes"], out var parsedMinutes) ? parsedMinutes : 120;
			var expiresAt = DateTime.UtcNow.AddMinutes(expiresMinutes);

			var claims = new List<Claim>
			{
				new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
				new(JwtRegisteredClaimNames.Email, user.Email),
				new(JwtRegisteredClaimNames.UniqueName, user.Name)
			};

			claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

			var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
			var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

			var token = new JwtSecurityToken(
				issuer: issuer,
				audience: audience,
				claims: claims,
				expires: expiresAt,
				signingCredentials: credentials);

			return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
		}
	}
}
