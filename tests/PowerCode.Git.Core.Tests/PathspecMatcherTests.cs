using PowerCode.Git.Abstractions;

namespace PowerCode.Git.Core.Tests;

[TestClass]
public sealed class PathspecMatcherTests
{
    [TestMethod]
    [DataRow("src/foo.cs", "src/foo.cs")]
    [DataRow("src/Foo.cs", "src/foo.cs")]
    [DataRow("src/foo.cs", "src/Foo.CS")]
    public void IsMatch_ExactPath_ReturnsTrue(string filePath, string pattern)
    {
        Assert.IsTrue(PathspecMatcher.IsMatch(filePath, pattern));
    }

    [TestMethod]
    [DataRow("src/foo.cs", "src/bar.cs")]
    [DataRow("src/foo.cs", "other/foo.cs")]
    public void IsMatch_ExactPathNoMatch_ReturnsFalse(string filePath, string pattern)
    {
        Assert.IsFalse(PathspecMatcher.IsMatch(filePath, pattern));
    }

    [TestMethod]
    [DataRow("src/foo.cs", "src/")]
    [DataRow("src/sub/bar.cs", "src/")]
    [DataRow("src/a/b/c.txt", "src/")]
    public void IsMatch_DirectoryPrefix_MatchesFilesUnderDirectory(string filePath, string pattern)
    {
        Assert.IsTrue(PathspecMatcher.IsMatch(filePath, pattern));
    }

    [TestMethod]
    [DataRow("other/foo.cs", "src/")]
    [DataRow("srcfile.cs", "src/")]
    public void IsMatch_DirectoryPrefix_DoesNotMatchOutsideDirectory(string filePath, string pattern)
    {
        Assert.IsFalse(PathspecMatcher.IsMatch(filePath, pattern));
    }

    [TestMethod]
    [DataRow("foo.cs", "*.cs")]
    [DataRow("bar.cs", "*.cs")]
    [DataRow("README.md", "*.md")]
    public void IsMatch_SingleStar_MatchesInCurrentSegment(string filePath, string pattern)
    {
        Assert.IsTrue(PathspecMatcher.IsMatch(filePath, pattern));
    }

    [TestMethod]
    [DataRow("src/foo.cs", "*.cs")]
    [DataRow("a/b/c.cs", "*.cs")]
    public void IsMatch_SingleStar_DoesNotCrossDirectoryBoundary(string filePath, string pattern)
    {
        Assert.IsFalse(PathspecMatcher.IsMatch(filePath, pattern));
    }

    [TestMethod]
    [DataRow("src/foo.cs", "src/*.cs")]
    [DataRow("src/bar.cs", "src/*.cs")]
    public void IsMatch_SingleStarInDirectory_MatchesWithinThatDirectory(string filePath, string pattern)
    {
        Assert.IsTrue(PathspecMatcher.IsMatch(filePath, pattern));
    }

    [TestMethod]
    [DataRow("src/sub/foo.cs", "src/*.cs")]
    public void IsMatch_SingleStarInDirectory_DoesNotMatchSubdirectory(string filePath, string pattern)
    {
        Assert.IsFalse(PathspecMatcher.IsMatch(filePath, pattern));
    }

    [TestMethod]
    [DataRow("foo.cs", "**/*.cs")]
    [DataRow("src/foo.cs", "**/*.cs")]
    [DataRow("a/b/c.cs", "**/*.cs")]
    [DataRow("a/b/c/d/e.cs", "**/*.cs")]
    public void IsMatch_DoubleStar_MatchesAcrossDirectoryBoundaries(string filePath, string pattern)
    {
        Assert.IsTrue(PathspecMatcher.IsMatch(filePath, pattern));
    }

    [TestMethod]
    [DataRow("foo.txt", "**/*.cs")]
    [DataRow("src/foo.txt", "**/*.cs")]
    public void IsMatch_DoubleStar_DoesNotMatchWrongExtension(string filePath, string pattern)
    {
        Assert.IsFalse(PathspecMatcher.IsMatch(filePath, pattern));
    }

    [TestMethod]
    [DataRow("x/foo/bar.txt", "**/foo/bar.*")]
    [DataRow("a/b/foo/bar.cs", "**/foo/bar.*")]
    [DataRow("foo/bar.md", "**/foo/bar.*")]
    public void IsMatch_DoubleStarWithDirectoryAndWildcard_Matches(string filePath, string pattern)
    {
        Assert.IsTrue(PathspecMatcher.IsMatch(filePath, pattern));
    }

    [TestMethod]
    [DataRow("x/baz/bar.txt", "**/foo/bar.*")]
    [DataRow("foo/baz.txt", "**/foo/bar.*")]
    public void IsMatch_DoubleStarWithDirectoryAndWildcard_DoesNotMatch(string filePath, string pattern)
    {
        Assert.IsFalse(PathspecMatcher.IsMatch(filePath, pattern));
    }

    [TestMethod]
    [DataRow("a.cs", "?.cs")]
    [DataRow("z.cs", "?.cs")]
    public void IsMatch_QuestionMark_MatchesSingleCharacter(string filePath, string pattern)
    {
        Assert.IsTrue(PathspecMatcher.IsMatch(filePath, pattern));
    }

    [TestMethod]
    [DataRow("ab.cs", "?.cs")]
    [DataRow(".cs", "?.cs")]
    public void IsMatch_QuestionMark_DoesNotMatchMultipleOrZeroCharacters(string filePath, string pattern)
    {
        Assert.IsFalse(PathspecMatcher.IsMatch(filePath, pattern));
    }

    [TestMethod]
    public void IsMatch_MultiplePatterns_MatchesIfAnyMatches()
    {
        string[] patterns = ["*.txt", "*.cs"];

        Assert.IsTrue(PathspecMatcher.IsMatch("foo.cs", patterns));
        Assert.IsTrue(PathspecMatcher.IsMatch("readme.txt", patterns));
        Assert.IsFalse(PathspecMatcher.IsMatch("image.png", patterns));
    }

    [TestMethod]
    public void IsMatch_BackslashPaths_NormalizedToForwardSlash()
    {
        Assert.IsTrue(PathspecMatcher.IsMatch(@"src\Models\Foo.cs", "**/*.cs"));
        Assert.IsTrue(PathspecMatcher.IsMatch(@"src\foo.cs", "src/"));
    }

    [TestMethod]
    public void CompilePatterns_PrecompiledRegexes_MatchCorrectly()
    {
        string[] patterns = ["**/*.cs", "docs/"];
        var compiled = PathspecMatcher.CompilePatterns(patterns);

        Assert.IsTrue(PathspecMatcher.IsMatch("src/foo.cs", compiled));
        Assert.IsTrue(PathspecMatcher.IsMatch("docs/readme.md", compiled));
        Assert.IsFalse(PathspecMatcher.IsMatch("src/foo.txt", compiled));
    }

    [TestMethod]
    [DataRow("src/file.cs", "src/**")]
    [DataRow("src/sub/file.cs", "src/**")]
    [DataRow("src/a/b/c.txt", "src/**")]
    public void IsMatch_DoubleStarAtEnd_MatchesEverythingUnderDirectory(string filePath, string pattern)
    {
        Assert.IsTrue(PathspecMatcher.IsMatch(filePath, pattern));
    }

    [TestMethod]
    [DataRow("other/file.cs", "src/**")]
    public void IsMatch_DoubleStarAtEnd_DoesNotMatchOtherDirectory(string filePath, string pattern)
    {
        Assert.IsFalse(PathspecMatcher.IsMatch(filePath, pattern));
    }

    [TestMethod]
    [DataRow("file.with.dots.cs", "*.cs")]
    [DataRow("src/file.with.dots.cs", "**/*.cs")]
    public void IsMatch_FileWithMultipleDots_MatchesCorrectly(string filePath, string pattern)
    {
        Assert.IsTrue(PathspecMatcher.IsMatch(filePath, pattern));
    }

    [TestMethod]
    [DataRow("src/[special].cs", "src/[special].cs")]
    public void IsMatch_RegexSpecialCharactersInPattern_EscapedCorrectly(string filePath, string pattern)
    {
        Assert.IsTrue(PathspecMatcher.IsMatch(filePath, pattern));
    }
}
