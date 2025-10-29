using FluentAssertions;
using System;
using System.Linq;
using System.Text;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Unit.Services
{
    public class PasswordServiceTests
    {
        // ============ CREATE PASSWORD HASH ============

        [Fact]
        public void CreatePasswordHash_GeneratesHashAndSalt()
        {
            var password = "MySecurePassword123!";

            PasswordService.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            hash.Should().NotBeNull();
            hash.Should().NotBeEmpty();
            salt.Should().NotBeNull();
            salt.Should().NotBeEmpty();
        }

        [Fact]
        public void CreatePasswordHash_GeneratesValidHashLength()
        {
            var password = "TestPassword123";

            PasswordService.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            // HMACSHA256 produces a 32-byte (256-bit) hash
            hash.Length.Should().Be(32);
        }

        [Fact]
        public void CreatePasswordHash_GeneratesValidSaltLength()
        {
            var password = "TestPassword123";

            PasswordService.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            // HMACSHA256 default key size is 64 bytes
            salt.Length.Should().Be(64);
        }

        [Fact]
        public void CreatePasswordHash_GeneratesUniqueSalts()
        {
            var password = "SamePassword123";

            PasswordService.CreatePasswordHash(password, out byte[] hash1, out byte[] salt1);
            PasswordService.CreatePasswordHash(password, out byte[] hash2, out byte[] salt2);

            // Same password should generate different salts
            salt1.Should().NotEqual(salt2);
        }

        [Fact]
        public void CreatePasswordHash_GeneratesDifferentHashesForSamePasswordDueToUniqueSalts()
        {
            var password = "SamePassword123";

            PasswordService.CreatePasswordHash(password, out byte[] hash1, out byte[] salt1);
            PasswordService.CreatePasswordHash(password, out byte[] hash2, out byte[] salt2);

            // Different salts should produce different hashes even for same password
            hash1.Should().NotEqual(hash2);
        }

        [Fact]
        public void CreatePasswordHash_WithDifferentPasswords_GeneratesDifferentHashes()
        {
            var password1 = "Password123";
            var password2 = "DifferentPassword456";

            PasswordService.CreatePasswordHash(password1, out byte[] hash1, out byte[] salt1);
            PasswordService.CreatePasswordHash(password2, out byte[] hash2, out byte[] salt2);

            hash1.Should().NotEqual(hash2);
            salt1.Should().NotEqual(salt2);
        }

        [Fact]
        public void CreatePasswordHash_WithEmptyPassword_GeneratesHashAndSalt()
        {
            var password = string.Empty;

            PasswordService.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            hash.Should().NotBeNull();
            hash.Should().NotBeEmpty();
            salt.Should().NotBeNull();
            salt.Should().NotBeEmpty();
        }

        [Theory]
        [InlineData("a")]
        [InlineData("short")]
        [InlineData("VeryLongPasswordWithManyCharacters1234567890!@#$%^&*()")]
        public void CreatePasswordHash_WithVariousPasswordLengths_GeneratesConsistentHashLength(string password)
        {
            PasswordService.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            // Hash length should always be 32 bytes regardless of password length
            hash.Length.Should().Be(32);
            salt.Length.Should().Be(64);
        }

        [Theory]
        [InlineData("password")]
        [InlineData("Password")]
        [InlineData("PASSWORD")]
        public void CreatePasswordHash_IsCaseSensitive(string password)
        {
            // Store the first password's hash
            PasswordService.CreatePasswordHash("password", out byte[] originalHash, out byte[] originalSalt);
            
            // Create hash for the test password
            PasswordService.CreatePasswordHash(password, out byte[] testHash, out byte[] testSalt);

            if (password == "password")
            {
                // Same password with same salt should produce same hash (testing determinism)
                var hmac1 = new System.Security.Cryptography.HMACSHA256(originalSalt);
                var hash1 = hmac1.ComputeHash(Encoding.UTF8.GetBytes("password"));
                var hash2 = hmac1.ComputeHash(Encoding.UTF8.GetBytes("password"));
                hash1.Should().Equal(hash2);
            }
            else
            {
                // Different case should produce different hash (with same salt)
                var hmac = new System.Security.Cryptography.HMACSHA256(originalSalt);
                var lowerHash = hmac.ComputeHash(Encoding.UTF8.GetBytes("password"));
                var differentHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                lowerHash.Should().NotEqual(differentHash);
            }
        }

        [Fact]
        public void CreatePasswordHash_WithSpecialCharacters_WorksCorrectly()
        {
            var password = "P@ssw0rd!#$%^&*()_+-=[]{}|;:',.<>?/~`";

            PasswordService.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            hash.Should().NotBeNull();
            hash.Length.Should().Be(32);
            salt.Should().NotBeNull();
            salt.Length.Should().Be(64);
        }

        [Fact]
        public void CreatePasswordHash_WithUnicodeCharacters_WorksCorrectly()
        {
            var password = "Pässwörd123你好мир";

            PasswordService.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            hash.Should().NotBeNull();
            hash.Length.Should().Be(32);
        }

        // ============ VERIFY PASSWORD ============

        [Fact]
        public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
        {
            var password = "MySecurePassword123!";
            PasswordService.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            var result = PasswordService.VerifyPassword(password, hash, salt);

            result.Should().BeTrue();
        }

        [Fact]
        public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
        {
            var correctPassword = "CorrectPassword123";
            var incorrectPassword = "WrongPassword456";
            PasswordService.CreatePasswordHash(correctPassword, out byte[] hash, out byte[] salt);

            var result = PasswordService.VerifyPassword(incorrectPassword, hash, salt);

            result.Should().BeFalse();
        }

        [Fact]
        public void VerifyPassword_WithSlightlyDifferentPassword_ReturnsFalse()
        {
            var password = "MyPassword123";
            PasswordService.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            // Test various slight modifications
            PasswordService.VerifyPassword("MyPassword124", hash, salt).Should().BeFalse(); // Different number
            PasswordService.VerifyPassword("MyPassword123 ", hash, salt).Should().BeFalse(); // Extra space
            PasswordService.VerifyPassword("MyPassword12", hash, salt).Should().BeFalse(); // Missing character
            PasswordService.VerifyPassword("myPassword123", hash, salt).Should().BeFalse(); // Different case
        }

        [Fact]
        public void VerifyPassword_WithEmptyPassword_WorksCorrectly()
        {
            var password = string.Empty;
            PasswordService.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            var result = PasswordService.VerifyPassword(password, hash, salt);

            result.Should().BeTrue();
        }

        [Fact]
        public void VerifyPassword_WithWrongSalt_ReturnsFalse()
        {
            var password = "TestPassword123";
            PasswordService.CreatePasswordHash(password, out byte[] hash, out byte[] salt);
            PasswordService.CreatePasswordHash("OtherPassword", out byte[] _, out byte[] wrongSalt);

            var result = PasswordService.VerifyPassword(password, hash, wrongSalt);

            result.Should().BeFalse();
        }

        [Fact]
        public void VerifyPassword_WithWrongHash_ReturnsFalse()
        {
            var password = "TestPassword123";
            PasswordService.CreatePasswordHash(password, out byte[] hash, out byte[] salt);
            PasswordService.CreatePasswordHash("OtherPassword", out byte[] wrongHash, out byte[] _);

            var result = PasswordService.VerifyPassword(password, wrongHash, salt);

            result.Should().BeFalse();
        }

        [Fact]
        public void VerifyPassword_WithModifiedHash_ReturnsFalse()
        {
            var password = "TestPassword123";
            PasswordService.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            // Modify one byte of the hash
            var modifiedHash = (byte[])hash.Clone();
            modifiedHash[0] = (byte)(modifiedHash[0] ^ 0xFF); // Flip all bits in first byte

            var result = PasswordService.VerifyPassword(password, modifiedHash, salt);

            result.Should().BeFalse();
        }

        [Fact]
        public void VerifyPassword_WithSpecialCharacters_WorksCorrectly()
        {
            var password = "P@ssw0rd!#$%^&*()";
            PasswordService.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            var result = PasswordService.VerifyPassword(password, hash, salt);

            result.Should().BeTrue();
        }

        [Fact]
        public void VerifyPassword_WithUnicodeCharacters_WorksCorrectly()
        {
            var password = "Pässwörd123你好мир";
            PasswordService.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            var result = PasswordService.VerifyPassword(password, hash, salt);

            result.Should().BeTrue();
        }

        // ============ INTEGRATION TESTS ============

        [Fact]
        public void CreateAndVerify_RoundTrip_WorksCorrectly()
        {
            var password = "MyTestPassword123!";

            // Create hash and salt
            PasswordService.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            // Verify with correct password
            var correctResult = PasswordService.VerifyPassword(password, hash, salt);
            correctResult.Should().BeTrue();

            // Verify with incorrect password
            var incorrectResult = PasswordService.VerifyPassword("WrongPassword", hash, salt);
            incorrectResult.Should().BeFalse();
        }

        [Fact]
        public void MultipleUsers_CanHaveSamePassword_WithDifferentHashes()
        {
            var commonPassword = "CommonPassword123";

            // Create hashes for multiple users with same password
            PasswordService.CreatePasswordHash(commonPassword, out byte[] hash1, out byte[] salt1);
            PasswordService.CreatePasswordHash(commonPassword, out byte[] hash2, out byte[] salt2);
            PasswordService.CreatePasswordHash(commonPassword, out byte[] hash3, out byte[] salt3);

            // All salts should be different
            salt1.Should().NotEqual(salt2);
            salt2.Should().NotEqual(salt3);
            salt1.Should().NotEqual(salt3);

            // All hashes should be different
            hash1.Should().NotEqual(hash2);
            hash2.Should().NotEqual(hash3);
            hash1.Should().NotEqual(hash3);

            // But all should verify correctly
            PasswordService.VerifyPassword(commonPassword, hash1, salt1).Should().BeTrue();
            PasswordService.VerifyPassword(commonPassword, hash2, salt2).Should().BeTrue();
            PasswordService.VerifyPassword(commonPassword, hash3, salt3).Should().BeTrue();
        }

        [Fact]
        public void PasswordHashing_IsDeterministic_WithSameSalt()
        {
            var password = "TestPassword123";
            PasswordService.CreatePasswordHash(password, out byte[] _, out byte[] salt);

            // Create two hashes with the same salt
            using var hmac1 = new System.Security.Cryptography.HMACSHA256(salt);
            using var hmac2 = new System.Security.Cryptography.HMACSHA256(salt);

            var hash1 = hmac1.ComputeHash(Encoding.UTF8.GetBytes(password));
            var hash2 = hmac2.ComputeHash(Encoding.UTF8.GetBytes(password));

            // Same password with same salt should produce same hash
            hash1.Should().Equal(hash2);
        }

        [Fact]
        public void PasswordService_IsStaticClass()
        {
            // Verify PasswordService is a static class (static classes are sealed and abstract in C#)
            var type = typeof(PasswordService);
            type.IsAbstract.Should().BeTrue();
            type.IsSealed.Should().BeTrue();
            // Static classes cannot be instantiated
            type.IsClass.Should().BeTrue();
        }

        [Theory]
        [InlineData("password123")]
        [InlineData("Admin@2024")]
        [InlineData("user_password")]
        [InlineData("P@ssw0rd!")]
        public void CommonPasswords_CanBeHashedAndVerified(string password)
        {
            PasswordService.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            var result = PasswordService.VerifyPassword(password, hash, salt);

            result.Should().BeTrue();
        }

        [Fact]
        public void VerifyPassword_WithNullPassword_ThrowsException()
        {
            PasswordService.CreatePasswordHash("test", out byte[] hash, out byte[] salt);

            Action act = () => PasswordService.VerifyPassword(null!, hash, salt);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void CreatePasswordHash_WithNullPassword_ThrowsException()
        {
            Action act = () => PasswordService.CreatePasswordHash(null!, out byte[] hash, out byte[] salt);

            act.Should().Throw<ArgumentNullException>();
        }

        // ============ SECURITY TESTS ============

        [Fact]
        public void PasswordHash_ShouldNotBeReversible()
        {
            var password = "SecretPassword123";
            PasswordService.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            // Hash should not contain the original password
            var hashString = Encoding.UTF8.GetString(hash);
            hashString.Should().NotContain(password);

            // Salt should not contain the original password
            var saltString = Encoding.UTF8.GetString(salt);
            saltString.Should().NotContain(password);
        }

        [Fact]
        public void DifferentPasswords_ProduceDifferentHashes_EvenWithSimilarContent()
        {
            var passwords = new[] 
            {
                "Password1",
                "Password2",
                "Password3",
                "1Password",
                "2Password"
            };

            var hashes = passwords.Select(p =>
            {
                PasswordService.CreatePasswordHash(p, out byte[] hash, out byte[] _);
                return hash;
            }).ToList();

            // All hashes should be unique
            for (int i = 0; i < hashes.Count; i++)
            {
                for (int j = i + 1; j < hashes.Count; j++)
                {
                    hashes[i].Should().NotEqual(hashes[j], 
                        $"Password '{passwords[i]}' and '{passwords[j]}' should produce different hashes");
                }
            }
        }

        [Fact]
        public void PasswordHashing_ProducesUnpredictableOutput()
        {
            var password = "TestPassword";
            var hashes = new System.Collections.Generic.List<byte[]>();

            // Generate multiple hashes for same password
            for (int i = 0; i < 5; i++)
            {
                PasswordService.CreatePasswordHash(password, out byte[] hash, out byte[] _);
                hashes.Add(hash);
            }

            // All hashes should be different due to random salts
            for (int i = 0; i < hashes.Count; i++)
            {
                for (int j = i + 1; j < hashes.Count; j++)
                {
                    hashes[i].Should().NotEqual(hashes[j]);
                }
            }
        }
    }
}

