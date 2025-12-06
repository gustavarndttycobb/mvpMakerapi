using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using MvpMakerApi.Application.Interfaces;

namespace MvpMakerApi.Application.Services;

public class EncryptionService : IEncryptionService
{
    private readonly string _key;

    public EncryptionService(IConfiguration configuration)
    {
        _key = configuration["Encryption:Key"] ?? throw new ArgumentNullException("Encryption:Key is missing in configuration");
    }

    public string EncryptData(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        var iv = new byte[16];
        byte[] array;

        using (var aes = Aes.Create())
        {
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(_key));
            aes.IV = iv;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (var memoryStream = new MemoryStream())
            {
                using (var cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    using (var streamWriter = new StreamWriter((Stream)cryptoStream))
                    {
                        streamWriter.Write(plainText);
                    }

                    array = memoryStream.ToArray();
                }
            }
        }

        return Convert.ToBase64String(array);
    }

    public string DecryptData(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        var iv = new byte[16];
        var buffer = Convert.FromBase64String(cipherText);

        using (var aes = Aes.Create())
        {
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(_key));
            aes.IV = iv;
            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using (var memoryStream = new MemoryStream(buffer))
            {
                using (var cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                {
                    using (var streamReader = new StreamReader((Stream)cryptoStream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
        }
    }
}
