using PrintifyGenerator.Library.VectorStorage;

namespace PrintifyGenerator.Library.Tests.VectorStorage;

public class CosineSimilarityTests
{
    [Fact]
    public void Compute_IdenticalVectors_ReturnsOne()
    {
        var a = new float[] { 1, 2, 3 };
        var b = new float[] { 1, 2, 3 };
        Assert.Equal(1f, CosineSimilarity.Compute(a, b), 5);
    }

    [Fact]
    public void Compute_OppositeVectors_ReturnsMinusOne()
    {
        var a = new float[] { 1, 0 };
        var b = new float[] { -1, 0 };
        Assert.Equal(-1f, CosineSimilarity.Compute(a, b), 5);
    }

    [Fact]
    public void Compute_OrthogonalVectors_ReturnsZero()
    {
        var a = new float[] { 1, 0 };
        var b = new float[] { 0, 1 };
        Assert.Equal(0f, CosineSimilarity.Compute(a, b), 5);
    }

    [Fact]
    public void Compute_ZeroVector_ReturnsZero()
    {
        var a = new float[] { 0, 0, 0 };
        var b = new float[] { 1, 2, 3 };
        Assert.Equal(0f, CosineSimilarity.Compute(a, b));
    }

    [Fact]
    public void Compute_BothZeroVectors_ReturnsZero()
    {
        var a = new float[] { 0, 0 };
        var b = new float[] { 0, 0 };
        Assert.Equal(0f, CosineSimilarity.Compute(a, b));
    }

    [Fact]
    public void Compute_DifferentLengths_Throws()
    {
        var a = new float[] { 1, 2, 3 };
        var b = new float[] { 1, 2 };
        Assert.Throws<ArgumentException>(() => CosineSimilarity.Compute(a, b));
    }

    [Fact]
    public void Compute_KnownValue_ReturnsCorrect()
    {
        var a = new float[] { 1, 2, 3 };
        var b = new float[] { 4, 5, 6 };
        // dot = 4+10+18 = 32
        // normA = sqrt(1+4+9) = sqrt(14) ≈ 3.74166
        // normB = sqrt(16+25+36) = sqrt(77) ≈ 8.77496
        // similarity = 32 / (3.74166 * 8.77496) ≈ 32 / 32.8329 ≈ 0.97463
        float expected = 32f / (MathF.Sqrt(14) * MathF.Sqrt(77));
        Assert.Equal(expected, CosineSimilarity.Compute(a, b), 5);
    }

    [Fact]
    public void Compute_IsSymmetric()
    {
        var a = new float[] { 1.5f, 2.5f, 3.5f };
        var b = new float[] { 4.5f, 5.5f, 6.5f };
        Assert.Equal(
            CosineSimilarity.Compute(a, b),
            CosineSimilarity.Compute(b, a),
            7);
    }

    [Fact]
    public void Distance_IdenticalVectors_ReturnsZero()
    {
        var a = new float[] { 1, 2, 3 };
        Assert.Equal(0f, CosineSimilarity.Distance(a, a), 5);
    }

    [Fact]
    public void Distance_OrthogonalVectors_ReturnsOne()
    {
        var a = new float[] { 1, 0 };
        var b = new float[] { 0, 1 };
        Assert.Equal(1f, CosineSimilarity.Distance(a, b), 5);
    }
}
