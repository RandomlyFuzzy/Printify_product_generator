using System.Numerics;

namespace PrintifyGenerator.Library.VectorStorage;

public static class CosineSimilarity
{
    public static float Compute(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Vectors must have same length");

        int i = 0;
        float dot = 0, normA = 0, normB = 0;

        if (Vector.IsHardwareAccelerated && a.Length >= Vector<float>.Count)
        {
            var vdot = Vector<float>.Zero;
            var vnormA = Vector<float>.Zero;
            var vnormB = Vector<float>.Zero;

            for (; i <= a.Length - Vector<float>.Count; i += Vector<float>.Count)
            {
                var va = new Vector<float>(a, i);
                var vb = new Vector<float>(b, i);
                vdot += va * vb;
                vnormA += va * va;
                vnormB += vb * vb;
            }

            for (int j = 0; j < Vector<float>.Count; j++)
            {
                dot += vdot[j];
                normA += vnormA[j];
                normB += vnormB[j];
            }
        }

        for (; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        normA = MathF.Sqrt(normA);
        normB = MathF.Sqrt(normB);

        if (normA == 0 || normB == 0)
            return 0;

        return dot / (normA * normB);
    }

    public static float Distance(float[] a, float[] b)
    {
        return 1f - Compute(a, b);
    }
}
