using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;

namespace pylorak.Utilities
{
    public class Pbkdf2
    {
        private const int ITERATIONS_MIN = 100_000;
        private const int ITERATIONS_MAX = 10_000_000;
        private const int MAX_STORAGE_CHARS = 512;
        private const int MIN_SALT_LENGTH = 8;

        public enum StorageFormat
        {
            Legacy, // TODO: Deprecated in TinyWall v3.5.2, possibly remove support in a future version
            Tw352
        }

        public enum HashFunction
        {
            SHA_1,
            SHA_256,
            SHA_512
        }

        public readonly HashFunction Algorithm;
        public readonly int Iterations;
        public readonly byte[] Salt;
        public byte[]? Hash;

        public Pbkdf2(HashFunction algorithm, int iterations, byte[] salt, byte[] hash)
        {
            if (salt is null)
                throw new ArgumentNullException(nameof(salt));
            if (salt.Length < MIN_SALT_LENGTH)
                throw new ArgumentException("Salt too short.", nameof(salt));
            if (hash is null)
                throw new ArgumentNullException(nameof(hash));
            if ((iterations < ITERATIONS_MIN) || (iterations > ITERATIONS_MAX))
                throw new ArgumentOutOfRangeException(nameof(iterations));

            Algorithm = algorithm;
            Iterations = iterations;
            Salt = salt;
            Hash = hash;
        }

        public Pbkdf2(HashFunction algorithm, int iterations, byte[] salt, string clearText)
        {
            if (salt is null)
                throw new ArgumentNullException(nameof(salt));
            if (salt.Length < MIN_SALT_LENGTH)
                throw new ArgumentException("Salt too short.", nameof(salt));
            if ((iterations < ITERATIONS_MIN) || (iterations > ITERATIONS_MAX))
                throw new ArgumentOutOfRangeException(nameof(iterations));

            Algorithm = algorithm;
            Iterations = iterations;
            Salt = salt;
            Hash = GetHash(clearText);
        }

        public static string Algo2Str(HashFunction algo)
        {
            return algo switch
            {
                HashFunction.SHA_1 => "SHA-1",
                HashFunction.SHA_256 => "SHA-256",
                HashFunction.SHA_512 => "SHA-512",
                _ => throw new ArgumentException("Invalid hash function.", nameof(algo))
            };
        }

        public static HashFunction Str2Algo(string algo)
        {
            return algo switch
            {
                "SHA-1" => HashFunction.SHA_1,
                "SHA-256" => HashFunction.SHA_256,
                "SHA-512" => HashFunction.SHA_512,
                _ => throw new ArgumentException("Invalid hash function.", nameof(algo))
            };
        }

        public static int GetExpectedNumHashBytes(HashFunction algo)
        {
            return algo switch
            {
                HashFunction.SHA_1 => 16,  // This is 16 (instead of 20) on purpose for compatibiltty with older TinyWall versions that used 16
                HashFunction.SHA_256 => 32,
                HashFunction.SHA_512 => 64,
                _ => throw new ArgumentException("Invalid hash function.", nameof(algo))
            };
        }

        public static Pbkdf2 Parse_LegacyFormat(string storedHash)
        {
            // Format: Rfc2898;salt;iterations;numHashBytes;hash

            try
            {
                // SHA-1 implicit
                var algo = HashFunction.SHA_1;

                // Validate total length
                if (string.IsNullOrEmpty(storedHash) || (storedHash.Length > MAX_STORAGE_CHARS))
                    throw new FormatException();

                // Validate number of fields
                var parts = storedHash.Split(';');
                if (parts.Length != 5)
                    throw new FormatException();

                // Validate format tag
                var format = parts[0];
                if (format != "Rfc2898")
                    throw new FormatException();

                // Validate salt
                var salt = Encoding.UTF8.GetBytes(parts[1]); // in legacy format salt is guaranteed to be only alphanumeric characters

                // Validate iterations
                var iterations = int.Parse(parts[2]);

                // Validate numHashBytes
                var expectedHashBytes = int.Parse(parts[3]);
                if (expectedHashBytes != GetExpectedNumHashBytes(algo))
                    throw new FormatException();

                // Validate hash length matches expected numBytes
                var hashBytes = Convert.FromBase64String(parts[4]);
                if (hashBytes.Length != expectedHashBytes)
                    throw new FormatException();

                return new Pbkdf2(algo, iterations, salt, hashBytes);
            }
            catch (Exception inner)
            {
                throw new FormatException("Invalid password hash.", inner);
            }
        }

        public static Pbkdf2 Parse_Tw352Format(string storedHash)
        {
            // Format: Rfc2898-TWv352;algo;salt;iterations;hash

            try
            {
                // Validate total length
                if (string.IsNullOrEmpty(storedHash) || (storedHash.Length > MAX_STORAGE_CHARS))
                    throw new FormatException();

                // Validate number of fields
                var parts = storedHash.Split(';');
                if (parts.Length != 5)
                    throw new FormatException();

                // Validate format tag
                var format = parts[0];
                if (format != "Rfc2898-TWv352")
                    throw new FormatException();

                // Validate algorithm
                var algo = Str2Algo(parts[1]);

                // Validate salt
                var salt = Convert.FromBase64String(parts[2]);

                // Validate iterations range
                var iterations = int.Parse(parts[3]);

                // Validate hash length matches expected numBytes
                byte[] hashBytes = Convert.FromBase64String(parts[4]);
                if (hashBytes.Length != GetExpectedNumHashBytes(algo))
                    throw new FormatException();

                return new Pbkdf2(algo, iterations, salt, hashBytes);
            }
            catch (Exception inner)
            {
                throw new FormatException("Invalid password hash.", inner);
            }
        }

        public static Pbkdf2 Parse(string storedHash, StorageFormat format)
        {
            return format switch
            {
                StorageFormat.Legacy => Parse_LegacyFormat(storedHash),
                StorageFormat.Tw352 => Parse_Tw352Format(storedHash),
                _ => throw new ArgumentException("Invalid hash storage format.", nameof(format))
            };
        }

        public byte[] GetHash(string clearText)
        {
            var algo = Algorithm switch
            {
                HashFunction.SHA_1 => HashAlgorithmName.SHA1,
                HashFunction.SHA_256 => HashAlgorithmName.SHA256,
                HashFunction.SHA_512 => HashAlgorithmName.SHA512,
                _ => throw new InvalidOperationException("Invalid algorithm.")
            };
            using var timer = new HierarchicalStopwatch("Password hash calculation");
            using var hasher = new Rfc2898DeriveBytes(clearText, Salt, Iterations, algo);
            return hasher.GetBytes(GetExpectedNumHashBytes(Algorithm));
        }

        public bool IsHashOf(string clearText)
        {
            if (Hash is null)
                throw new InvalidOperationException("Hash has not yet been calculated.");

            var expectedHash = GetHash(clearText);

            // Comparison is not constant-time, but this is fine because the hash is readable from the filesystem anyway,
            // so potential leakage through timing attacks poses no additional risk in our use-case.
            return StructuralComparisons.StructuralEqualityComparer.Equals(Hash, expectedHash);
        }

        public string ToString(StorageFormat format)
        {
            if (Hash is null)
                throw new InvalidOperationException("Hash has not yet been calculated.");

            if ((format == StorageFormat.Legacy) && (Algorithm != HashFunction.SHA_1))
                throw new InvalidOperationException("Legacy format only supports SHA-1.");

            string stored = format switch
            {
                StorageFormat.Legacy => string.Format("Rfc2898;{0};{1};{2};{3}",
                    Encoding.UTF8.GetString(Salt),  // in legacy format salt is guaranteed to be only alphanumeric characters
                    Iterations,
                    Hash.Length,
                    Convert.ToBase64String(Hash)),
                StorageFormat.Tw352 => string.Format("Rfc2898-TWv352;{0};{1};{2};{3}",
                    Algo2Str(Algorithm),
                    Convert.ToBase64String(Salt),
                    Iterations,
                    Convert.ToBase64String(Hash)),
                _ => throw new ArgumentException("Invalid hash storage format.", nameof(format))
            };

            if (stored.Length > MAX_STORAGE_CHARS)
                throw new ArgumentException("Salt too long.");

            return stored;
        }
    }
}
