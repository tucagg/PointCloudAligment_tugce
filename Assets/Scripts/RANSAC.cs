using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;

public class RANSAC
{
    // RANSAC ile rigid dönüşüm hesaplama
    public static (Matrix4x4, Vector3) AlignThreePointsWithSVD(List<Vector3> P, List<Vector3> Q)
    {
        // P ve Q'nun ortalama noktalarını hesapla
        Vector3 centroidP = CalculateCentroid(P);
        Vector3 centroidQ = CalculateCentroid(Q);

        // Ortalama noktaları çıkart
        List<Vector3> centeredP = SubtractCentroid(P, centroidP);
        List<Vector3> centeredQ = SubtractCentroid(Q, centroidQ);

        // Matrisleri oluştur
        var matrixP = CreateMatrix(centeredP);
        var matrixQ = CreateMatrix(centeredQ);

        // Korelasyon matrisi hesapla
        var correlationMatrix = matrixP.Transpose() * matrixQ;

        // SVD işlemini uygula
        var svd = correlationMatrix.Svd();

        // Rotasyon matrisi: R = V * U^T
        var rotationMatrix = svd.VT.Transpose() * svd.U;

        // Rotasyonu Unity formatına dönüştür
        Matrix4x4 rotation = ConvertToUnityMatrix(rotationMatrix);

        // Çeviri hesapla
        Vector3 translation = centroidQ - rotation.MultiplyPoint3x4(centroidP);

        return (rotation, translation);
    }

    public static int CountInliers(List<Vector3> P, List<Vector3> Q, Matrix4x4 rotation, Vector3 translation, float threshold)
    {
        int inliers = 0;
        for (int i = 0; i < P.Count; i++)
        {
            Vector3 transformedPoint = rotation.MultiplyPoint3x4(P[i]) + translation;
            float distance = Vector3.Distance(transformedPoint, Q[i % Q.Count]);
            if (distance < threshold)
                inliers++;
        }
        return inliers;
    }

    public static float CalculateError(List<Vector3> P, List<Vector3> Q, Matrix4x4 rotation, Vector3 translation)
    {
        float totalError = 0.0f;
        int count = Mathf.Min(P.Count, Q.Count);

        for (int i = 0; i < count; i++)
        {
            Vector3 transformedPoint = rotation.MultiplyPoint3x4(P[i]) + translation;
            float distance = Vector3.Distance(transformedPoint, Q[i]);
            totalError += distance * distance;
        }

        return totalError / count;
    }

    public static (Matrix4x4, Vector3) PerformRANSAC(List<Vector3> P, List<Vector3> Q, int iterations = 1000, float threshold = 0.5f)
{
    Matrix4x4 bestRotation = Matrix4x4.identity;
    Vector3 bestTranslation = Vector3.zero;
    int bestInliers = 0;
    float bestError = float.MaxValue;
    int bestIteration = 0; // En iyi iterasyonu tutmak için

    for (int i = 0; i < iterations; i++)
    {
        // 3 rastgele nokta seç
        List<Vector3> sampleP = GetRandomPoints(P, 3);
        List<Vector3> sampleQ = GetRandomPoints(Q, 3);

        // SVD ile dönüşümü hesapla
        var (rotation, translation) = AlignThreePointsWithSVD(sampleP, sampleQ);

        // Inliers sayısını hesapla
        int inliers = CountInliers(P, Q, rotation, translation, threshold);

        // Hata hesapla
        float error = CalculateError(P, Q, rotation, translation);

        // En iyi sonuçları seç
        if (inliers > bestInliers || (inliers == bestInliers && error < bestError))
        {
            bestInliers = inliers;
            bestRotation = rotation;
            bestTranslation = translation;
            bestError = error;
            bestIteration = i + 1; // En iyi iterasyonu kaydet
        }

        // Iteration logu
        Debug.Log($"Iteration {i + 1}/{iterations}: Inliers = {inliers}, Error = {error:F4}");
    }

    // En iyi iterasyonu göster
    Debug.Log($"Best Iteration: {bestIteration}\nBest Inliers: {bestInliers}\nBest Error: {bestError:F4}\nTotal Iterations: {iterations}");
    return (bestRotation, bestTranslation);
}

    private static Vector3 CalculateCentroid(List<Vector3> points)
    {
        Vector3 sum = Vector3.zero;
        foreach (var point in points)
        {
            sum += point;
        }
        return sum / points.Count;
    }

    private static List<Vector3> SubtractCentroid(List<Vector3> points, Vector3 centroid)
    {
        List<Vector3> centeredPoints = new List<Vector3>();
        foreach (var point in points)
        {
            centeredPoints.Add(point - centroid);
        }
        return centeredPoints;
    }

    private static Matrix<double> CreateMatrix(List<Vector3> points)
    {
        var matrix = Matrix<double>.Build.Dense(points.Count, 3);
        for (int i = 0; i < points.Count; i++)
        {
            matrix[i, 0] = points[i].x;
            matrix[i, 1] = points[i].y;
            matrix[i, 2] = points[i].z;
        }
        return matrix;
    }

    private static Matrix4x4 ConvertToUnityMatrix(Matrix<double> mathNetMatrix)
    {
        Matrix4x4 unityMatrix = Matrix4x4.identity;
        unityMatrix.m00 = (float)mathNetMatrix[0, 0];
        unityMatrix.m01 = (float)mathNetMatrix[0, 1];
        unityMatrix.m02 = (float)mathNetMatrix[0, 2];

        unityMatrix.m10 = (float)mathNetMatrix[1, 0];
        unityMatrix.m11 = (float)mathNetMatrix[1, 1];
        unityMatrix.m12 = (float)mathNetMatrix[1, 2];

        unityMatrix.m20 = (float)mathNetMatrix[2, 0];
        unityMatrix.m21 = (float)mathNetMatrix[2, 1];
        unityMatrix.m22 = (float)mathNetMatrix[2, 2];

        return unityMatrix;
    }

    private static List<Vector3> GetRandomPoints(List<Vector3> points, int count)
    {
        List<Vector3> selected = new List<Vector3>();
        System.Random rand = new System.Random();
        while (selected.Count < count)
        {
            Vector3 point = points[rand.Next(points.Count)];
            if (!selected.Contains(point))
                selected.Add(point);
        }
        return selected;
    }
}