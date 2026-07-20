using FluentAssertions;
using ObsidianRagEngine.Console.Common.Utility;

namespace ObsidianRagEngine.Tests;

public class HashUtilityTests
{
    [Fact]
    public void ComputeHash_SameContent_ReturnsIdenticalHash()
    {
        var first = HashUtility.ComputeHash("obsidian note body");
        var second = HashUtility.ComputeHash("obsidian note body");

        first.Should().Be(second);
    }

    [Fact]
    public void ComputeHash_DifferentContent_ReturnsDifferentHashes()
    {
        var first = HashUtility.ComputeHash("note A");
        var second = HashUtility.ComputeHash("note B");

        first.Should().NotBe(second);
    }

    [Fact]
    public void ComputeHash_KnownInput_ReturnsExpectedSha256Hex()
    {
        // SHA-256("hello") as lowercase hex
        var hash = HashUtility.ComputeHash("hello");

        hash.Should().Be("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[0-9a-f]+$");
    }
}
