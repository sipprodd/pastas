namespace Pastas.Domain.Interfaces;

public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string encryptedText);
}
