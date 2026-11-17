using FluentAssertions;
using ParkingSystem.Security;
using Xunit;

namespace ParkingSystem.Tests;

public class SecurityTests
{
    [Fact]
    public void HashPassword_ShouldReturnValidSaltedPbkdf2Hash()
    {
        // Arrange
        string rawPassword = "SuperSecretPassword123!";

        // Act
        string hash = PasswordHasher.HashPassword(rawPassword);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        string[] parts = hash.Split(':');
        parts.Should().HaveCount(3, "Format hash harus terdiri dari 3 segmen {iterasi}:{salt}:{hash}");
        int.Parse(parts[0]).Should().BeGreaterThanOrEqualTo(100_000);
        Convert.FromBase64String(parts[1]).Length.Should().Be(16, "Ukuran salt harus 16 byte");
        Convert.FromBase64String(parts[2]).Length.Should().Be(32, "Ukuran hash harus 32 byte");
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        string password = "adminpassword";
        string hash = PasswordHasher.HashPassword(password);

        // Act
        bool isValid = PasswordHasher.VerifyPassword(password, hash);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ShouldReturnFalse()
    {
        // Arrange
        string password = "adminpassword";
        string hash = PasswordHasher.HashPassword(password);

        // Act
        bool isValid = PasswordHasher.VerifyPassword("wrongpassword", hash);

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void VerifyPassword_WithNullOrEmpty_ShouldReturnFalse(string? invalidInput)
    {
        // Act & Assert
        PasswordHasher.VerifyPassword(invalidInput!, "100000:salt:hash").Should().BeFalse();
    }
}
