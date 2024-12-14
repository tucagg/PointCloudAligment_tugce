using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RANSAC
{
    // 3 noktadan rigid dönüşüm hesaplama
    public static (Matrix4x4, Vector3) AlignThreePoints(List<Vector3> P, List<Vector3> Q)
    {
        // 3 nokta seç
        Vector3 p1 = P[0], p2 = P[1], p3 = P[2];
        Vector3 q1 = Q[0], q2 = Q[1], q3 = Q[2];

        // 3 noktadan dönüşüm matrisini hesapla
        Matrix4x4 rotation = CalculateRotation(p1, p2, p3, q1, q2, q3);
        Vector3 translation = q1 - rotation.MultiplyPoint3x4(p1);

        return (rotation, translation);
    }

    // Rotasyonu 3 nokta üzerinden hesapla
    private static Matrix4x4 CalculateRotation(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 q1, Vector3 q2, Vector3 q3)
    {
        // Vektörleri oluştur
        Vector3 p12 = p2 - p1;
        Vector3 p13 = p3 - p1;

        Vector3 q12 = q2 - q1;
        Vector3 q13 = q3 - q1;

        // Normalizasyon ve çapraz çarpma
        Vector3 axis = Vector3.Cross(p12, p13).normalized;
        Vector3 targetAxis = Vector3.Cross(q12, q13).normalized;

        // Rotasyon matrisi için dönüşümü yap
        float angle = Vector3.Angle(p12, q12);
        return Matrix4x4.Rotate(Quaternion.AngleAxis(angle, axis));
    }
    
    // RANSAC için inliers sayısını hesapla
    public static int CountInliers(List<Vector3> P, List<Vector3> Q, Matrix4x4 rotation, Vector3 translation, float threshold)
    {
        int inliers = 0;
        for (int i = 0; i < P.Count; i++)
        {
            Vector3 transformedPoint = rotation.MultiplyPoint3x4(P[i]) + translation; // Rigid Transform
            float distance = Vector3.Distance(transformedPoint, Q[i % Q.Count]);
            if (distance < threshold)
                inliers++;
        }
        return inliers;
    }

    public static (Matrix4x4, Vector3) PerformRANSAC(List<Vector3> P, List<Vector3> Q, int iterations = 100, float threshold = 0.1f)
{
    Matrix4x4 bestRotation = Matrix4x4.identity;
    Vector3 bestTranslation = Vector3.zero;
    int bestInliers = 0;
    float bestError = float.MaxValue;

    for (int i = 0; i < iterations; i++)
    {
        // 3 rastgele nokta seç
        List<Vector3> sampleP = GetRandomPoints(P, 3);
        List<Vector3> sampleQ = GetRandomPoints(Q, 3);

        // 3 nokta üzerinden dönüşümü hesapla
        var (rotation, translation) = AlignThreePoints(sampleP, sampleQ);

        // Inliers sayısını kontrol et
        int inliers = CountInliers(P, Q, rotation, translation, threshold);

        // Hata hesapla
        float error = CalculateError(P, Q, rotation, translation);

        // En fazla inlier bulunan dönüşümü seç
        if (inliers > bestInliers || (inliers == bestInliers && error < bestError))
        {
            bestInliers = inliers;
            bestRotation = rotation;
            bestTranslation = translation;
            bestError = error;
        }

        // Debug ekranı için loglama
        Debug.Log($"Iteration {i + 1}/{iterations}: Inliers = {inliers}, Error = {error:F4}");
    }

    // En iyi sonuçları loglama
    Debug.Log($"Best Inliers: {bestInliers}\nTotal Iterations: {iterations}\nBest Error: {bestError:F4}");

    return (bestRotation, bestTranslation);
}

public static float CalculateError(List<Vector3> P, List<Vector3> Q, Matrix4x4 rotation, Vector3 translation)
{
    float totalError = 0.0f;
    int count = Mathf.Min(P.Count, Q.Count);

    for (int i = 0; i < count; i++)
    {
        Vector3 transformedPoint = rotation.MultiplyPoint3x4(P[i]) + translation;
        float distance = Vector3.Distance(transformedPoint, Q[i]);
        totalError += distance * distance; // Squared error
    }

    return totalError / count; // Mean squared error
}

    // Listeden rastgele 3 nokta seç
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