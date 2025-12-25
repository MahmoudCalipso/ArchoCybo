using System;
using System.Security.Cryptography;
using System.Text;

namespace ArchoCybo.SharedKernel.Security;

public static class PasswordHasher
{
    // PBKDF2 hashing
    public static string Hash(string password, int iterations = 100_000)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32);
        var result = new byte[1 + 4 + salt.Length + hash.Length];
        // format: 0x01 | iterations (int) | salt | hash
        result[0] = 0x01;
        Buffer.BlockCopy(BitConverter.GetBytes(iterations), 0, result, 1, 4);
        Buffer.BlockCopy(salt, 0, result, 5, salt.Length);
        Buffer.BlockCopy(hash, 0, result, 5 + salt.Length, hash.Length);
        return Convert.ToBase64String(result);
    }

    public static bool Verify(string password, string hashed)
    {
        try
        {
            var bytes = Convert.FromBase64String(hashed);
            if (bytes.Length < 1 + 4 + 16 + 32) return false;
            if (bytes[0] != 0x01) return false;
            var iterations = BitConverter.ToInt32(bytes, 1);
            var salt = new byte[16];
            Buffer.BlockCopy(bytes, 5, salt, 0, 16);
            var hash = new byte[32];
            Buffer.BlockCopy(bytes, 5 + 16, hash, 0, 32);
            var computed = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(computed, hash);
        }
        catch
        {
            return false;
        }
    }
}
