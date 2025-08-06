using System.Security.Cryptography;
using System.Text;

namespace ImageScrambler.Core;

public static class CryptographicConstants
{
    // Salt derivation constants
    public const string DefaultSaltDerivationKey = "CSC_SALT_DERIVE";

    public const string DefaultSaltDerivationPattern = "CSC_SALT_DERIVE_{0}";

    // ChromaShiftCipher constants
    public const string DefaultCSCSaltContext = "CSC_SALT_DERIVATION";

    public const string DefaultDCTPermutationContext = "DCT_PERMUTATION";
    public const string DefaultBlocksContext = "BLOCKS";
}

public static class CryptographicCore
{
    private const int DefaultSaltSize = 32;

    public static byte[] DeriveSaltFromPassword(byte[] password, string? context = null, string? saltDerivationKey = null, string? saltDerivationPattern = null)
    {
        // Use default constants if not provided (for performance, no validation when using defaults)
        context ??= CryptographicConstants.DefaultCSCSaltContext;
        saltDerivationKey ??= CryptographicConstants.DefaultSaltDerivationKey;
        saltDerivationPattern ??= CryptographicConstants.DefaultSaltDerivationPattern;

        // Validate custom parameters if provided (check if any were originally provided)
        bool hasCustomContext = context != CryptographicConstants.DefaultCSCSaltContext;
        bool hasCustomKey = saltDerivationKey != CryptographicConstants.DefaultSaltDerivationKey;
        bool hasCustomPattern = saltDerivationPattern != CryptographicConstants.DefaultSaltDerivationPattern;

        if (hasCustomContext && string.IsNullOrWhiteSpace(context))
            throw new ArgumentException("Context cannot be null or whitespace.", nameof(context));
        if (hasCustomKey && string.IsNullOrWhiteSpace(saltDerivationKey))
            throw new ArgumentException("Salt derivation key cannot be null or whitespace.", nameof(saltDerivationKey));
        if (hasCustomPattern)
        {
            if (string.IsNullOrWhiteSpace(saltDerivationPattern))
                throw new ArgumentException("Salt derivation pattern cannot be null or whitespace.", nameof(saltDerivationPattern));
            if (!saltDerivationPattern.Contains("{0}"))
                throw new ArgumentException("Salt derivation pattern must contain '{0}' placeholder.", nameof(saltDerivationPattern));
        }

        var contextBytes = Encoding.UTF8.GetBytes(context);
        byte[] seed = SHA256.HashData([.. password, .. contextBytes]);
        byte[] prk = HMACSHA256.HashData(Encoding.UTF8.GetBytes(saltDerivationKey), seed);

        using var hmac = new HMACSHA256(prk);
        var result = new List<byte>(DefaultSaltSize * 2);
        int counter = 1;

        while (result.Count < DefaultSaltSize)
        {
            var info = Encoding.UTF8.GetBytes(string.Format(saltDerivationPattern, counter)).Concat(seed).ToArray();
            result.AddRange(HMACSHA256.HashData(prk, info));
            counter++;
        }
        return [.. result.Take(DefaultSaltSize)];
    }

    public static int GenerateSecureSeed(byte[] password, byte[] salt, string? context = null, int length = 0)
    {
        // Use default constant if not provided (for performance, no validation when using default)
        context ??= CryptographicConstants.DefaultBlocksContext;

        // Validate custom context if provided
        if (context != CryptographicConstants.DefaultBlocksContext && string.IsNullOrWhiteSpace(context))
            throw new ArgumentException("Context cannot be null or whitespace.", nameof(context));

        byte[] prk = HMACSHA256.HashData(salt, password);
        var info = Encoding.UTF8.GetBytes($"{context}_{length}");
        byte[] seedBytes = HMACSHA256.HashData(prk, info);

        return (int)(BitConverter.ToUInt32(seedBytes, 0) & 0x7FFFFFFF);
    }
}