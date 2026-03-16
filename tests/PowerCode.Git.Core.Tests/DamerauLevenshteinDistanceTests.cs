using PowerCode.Git.Abstractions;

namespace PowerCode.Git.Core.Tests;

[TestClass]
public sealed class DamerauLevenshteinDistanceTests
{
    // ── Compute (raw distance) ───────────────────────────────────────────────

    [TestMethod]
    public void Compute_IdenticalStrings_ReturnsZero()
    {
        Assert.AreEqual(0, DamerauLevenshteinDistance.Compute("kitten", "kitten"));
    }

    [TestMethod]
    public void Compute_BothEmpty_ReturnsZero()
    {
        Assert.AreEqual(0, DamerauLevenshteinDistance.Compute("", ""));
    }

    [TestMethod]
    public void Compute_EmptySource_ReturnsTargetLength()
    {
        Assert.AreEqual(3, DamerauLevenshteinDistance.Compute("", "abc"));
    }

    [TestMethod]
    public void Compute_EmptyTarget_ReturnsSourceLength()
    {
        Assert.AreEqual(4, DamerauLevenshteinDistance.Compute("abcd", ""));
    }

    [TestMethod]
    public void Compute_SingleInsertion_ReturnsOne()
    {
        // "abc" → "abcd"
        Assert.AreEqual(1, DamerauLevenshteinDistance.Compute("abc", "abcd"));
    }

    [TestMethod]
    public void Compute_SingleDeletion_ReturnsOne()
    {
        // "abcd" → "abc"
        Assert.AreEqual(1, DamerauLevenshteinDistance.Compute("abcd", "abc"));
    }

    [TestMethod]
    public void Compute_SingleSubstitution_ReturnsOne()
    {
        // "ab" → "ac"
        Assert.AreEqual(1, DamerauLevenshteinDistance.Compute("ab", "ac"));
    }

    [TestMethod]
    public void Compute_SingleTransposition_ReturnsOne()
    {
        // OSA feature: adjacent transposition costs 1, not 2.
        // "ab" → "ba"
        Assert.AreEqual(1, DamerauLevenshteinDistance.Compute("ab", "ba"));
    }

    [TestMethod]
    public void Compute_KittenToSitting_ReturnsThree()
    {
        // Classic example: kitten → sitting = 3 operations
        // (k→s, e→i, +g)
        Assert.AreEqual(3, DamerauLevenshteinDistance.Compute("kitten", "sitting"));
    }

    [TestMethod]
    public void Compute_CompletelyDifferentSameLength_ReturnsLength()
    {
        // "abc" → "xyz": three substitutions
        Assert.AreEqual(3, DamerauLevenshteinDistance.Compute("abc", "xyz"));
    }

    [TestMethod]
    [DataRow("var x = 42;", "var x = 43;", 1)]  // single digit change
    [DataRow("foo", "bar", 3)]                    // fully different 3-char
    [DataRow("hello", "helo", 1)]                 // single deletion
    [DataRow("ca", "abc", 3)]                    // insert 'b', move 'a'
    public void Compute_KnownPairs_ReturnsExpectedDistance(string source, string target, int expected)
    {
        Assert.AreEqual(expected, DamerauLevenshteinDistance.Compute(source, target));
    }

    // ── ComputeNormalized ────────────────────────────────────────────────────

    [TestMethod]
    public void ComputeNormalized_IdenticalStrings_ReturnsOne()
    {
        Assert.AreEqual(1.0, DamerauLevenshteinDistance.ComputeNormalized("hello", "hello"), delta: 0.0001);
    }

    [TestMethod]
    public void ComputeNormalized_BothEmpty_ReturnsOne()
    {
        Assert.AreEqual(1.0, DamerauLevenshteinDistance.ComputeNormalized("", ""), delta: 0.0001);
    }

    [TestMethod]
    public void ComputeNormalized_EmptyVsNonEmpty_ReturnsZero()
    {
        Assert.AreEqual(0.0, DamerauLevenshteinDistance.ComputeNormalized("", "abc"), delta: 0.0001);
        Assert.AreEqual(0.0, DamerauLevenshteinDistance.ComputeNormalized("xyz", ""), delta: 0.0001);
    }

    [TestMethod]
    public void ComputeNormalized_SingleEditOnTwoCharString_ReturnsHalf()
    {
        // "ab" → "ac": distance=1, max=2, similarity=0.5
        var sim = DamerauLevenshteinDistance.ComputeNormalized("ab", "ac");
        Assert.AreEqual(0.5, sim, delta: 0.0001);
    }

    [TestMethod]
    public void ComputeNormalized_AlwaysInRange()
    {
        string[] words = ["", "a", "ab", "kitten", "sitting", "var x = 42;", "Console.WriteLine(\"hello\");"];

        foreach (var s in words)
        {
            foreach (var t in words)
            {
                var sim = DamerauLevenshteinDistance.ComputeNormalized(s, t);
                Assert.IsTrue(sim is >= 0.0 and <= 1.0, $"Similarity out of range for '{s}' vs '{t}': {sim}");
            }
        }
    }

    [TestMethod]
    public void ComputeNormalized_SymmetricProperty()
    {
        // Similarity should be the same in both directions.
        var ab = DamerauLevenshteinDistance.ComputeNormalized("kitten", "sitting");
        var ba = DamerauLevenshteinDistance.ComputeNormalized("sitting", "kitten");
        Assert.AreEqual(ab, ba, delta: 0.0001);
    }

    [TestMethod]
    public void ComputeNormalized_SmallEditOnMediumLine_ReturnsHighSimilarity()
    {
        // "var x = 42;" → "var x = 43;" — one char differs on an 11-char line → ~0.91
        var sim = DamerauLevenshteinDistance.ComputeNormalized("var x = 42;", "var x = 43;");
        Assert.IsGreaterThan(0.7, sim);
    }

    [TestMethod]
    [DataRow("abc", "xyz")]  // fully different → zero similarity
    public void ComputeNormalized_CompletelyDifferent_ReturnsLowSimilarity(string source, string target)
    {
        var sim = DamerauLevenshteinDistance.ComputeNormalized(source, target);
        Assert.IsLessThan(0.3, sim);
    }
}
