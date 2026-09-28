using System.Security.Cryptography;
using System.Text;

namespace MentoriaAPI.Services
{
	public class PasswordService
	{
		public string HashPassword(string password)
		{
			var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
			var hash = ComputeHash(password, salt);
			return $"{salt}:{hash}";
		}

		public bool VerifyPassword(string password, string passwordHash)
		{
			var parts = passwordHash.Split(':', 2);
			if (parts.Length != 2)
			{
				return false;
			}

			var computedHash = ComputeHash(password, parts[0]);
			return computedHash == parts[1];
		}

		private static string ComputeHash(string password, string salt)
		{
			var bytes = Encoding.UTF8.GetBytes($"{salt}:{password}");
			var hashBytes = SHA256.HashData(bytes);
			return Convert.ToBase64String(hashBytes);
		}
	}
}
