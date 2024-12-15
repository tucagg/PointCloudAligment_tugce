using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

public class RANSAC
{
    // 3 noktadan rigid dönüşüm hesaplama
    public static (Matrix4x4, Vector3) AlignThreePoints(List<Vector3> P, List<Vector3> Q)
    {
        Vector3 p1 = P[0], p2 = P[1], p3 = P[2];
        Vector3 q1 = Q[0], q2 = Q[1], q3 = Q[2];

        Matrix4x4 rotation = CalculateRotation(p1, p2, p3, q1, q2, q3);
        Vector3 translation = q1 - rotation.MultiplyPoint3x4(p1);

        return (rotation, translation);
    }

    private static Matrix4x4 CalculateRotation(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 q1, Vector3 q2, Vector3 q3)
    {
        Vector3 p12 = p2 - p1;
        Vector3 p13 = p3 - p1;

        Vector3 q12 = q2 - q1;
        Vector3 q13 = q3 - q1;

        Vector3 axis = Vector3.Cross(p12, p13).normalized;
        Vector3 targetAxis = Vector3.Cross(q12, q13).normalized;

        float angle = Vector3.Angle(p12, q12);
        return Matrix4x4.Rotate(Quaternion.AngleAxis(angle, axis));
    }

    public static (Matrix4x4, Vector3) ComputeRigidTransformation(List<Vector3> P, List<Vector3> Q)
    {
        if (P.Count != Q.Count)
        {
            Debug.LogError("Point sets must have the same number of points.");
            return (Matrix4x4.identity, Vector3.zero);
        }

        Vector3 centroidP = ComputeCentroid(P);
        Vector3 centroidQ = ComputeCentroid(Q);

        List<Vector3> centeredP = SubtractCentroid(P, centroidP);
        List<Vector3> centeredQ = SubtractCentroid(Q, centroidQ);

        Matrix4x4 covariance = ComputeCovarianceMatrix(centeredP, centeredQ);
        Quaternion rotation = PerformSVD(covariance);

        Vector3 translation = centroidQ - rotation * centroidP;

        return (Matrix4x4.Rotate(rotation), translation);
    }

    private static Vector3 ComputeCentroid(List<Vector3> points)
    {
        Vector3 centroid = Vector3.zero;
        foreach (var point in points)
        {
            centroid += point;
        }
        return centroid / points.Count;
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

    private static Matrix4x4 ComputeCovarianceMatrix(List<Vector3> P, List<Vector3> Q)
    {
        Matrix4x4 covariance = Matrix4x4.zero;

        for (int i = 0; i < P.Count; i++)
        {
            Vector3 p = P[i];
            Vector3 q = Q[i];

            covariance.m00 += p.x * q.x;
            covariance.m01 += p.x * q.y;
            covariance.m02 += p.x * q.z;

            covariance.m10 += p.y * q.x;
            covariance.m11 += p.y * q.y;
            covariance.m12 += p.y * q.z;

            covariance.m20 += p.z * q.x;
            covariance.m21 += p.z * q.y;
            covariance.m22 += p.z * q.z;
        }

        return covariance;
    }

    private static Quaternion PerformSVD(Matrix4x4 covariance)
    {
        // Unity Matrix4x4'ü Math.NET matrisine dönüştür
        Matrix<float> matrix = DenseMatrix.OfArray(new float[,]
        {
            { covariance.m00, covariance.m01, covariance.m02 },
            { covariance.m10, covariance.m11, covariance.m12 },
            { covariance.m20, covariance.m21, covariance.m22 }
        });

        // SVD işlemini uygula
        var svd = matrix.Svd();

        // U ve V matrislerinden rotasyonu oluştur
        Matrix<float> u = svd.U;
        Matrix<float> vt = svd.VT;

        // Rotasyon matrisi: R = U * VT
        Matrix<float> rotationMatrix = u.Multiply(vt);

        // Math.NET rotasyonunu Quaternion'a dönüştür
        Vector3 forward = new Vector3(rotationMatrix[0, 2], rotationMatrix[1, 2], rotationMatrix[2, 2]).normalized;
        Vector3 up = new Vector3(rotationMatrix[0, 1], rotationMatrix[1, 1], rotationMatrix[2, 1]).normalized;

        return Quaternion.LookRotation(forward, up);
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

    public static (Matrix4x4, Vector3) PerformRANSAC(List<Vector3> P, List<Vector3> Q, int iterations = 100, float threshold = 0.1f)
    {
        Matrix4x4 bestRotation = Matrix4x4.identity;
        Vector3 bestTranslation = Vector3.zero;
        int bestInliers = 0;
        float bestError = float.MaxValue;

        for (int i = 0; i < iterations; i++)
        {
            List<Vector3> sampleP = GetRandomPoints(P, 3);
            List<Vector3> sampleQ = GetRandomPoints(Q, 3);

            var (rotation, translation) = AlignThreePoints(sampleP, sampleQ);

            int inliers = CountInliers(P, Q, rotation, translation, threshold);
            float error = CalculateError(P, Q, rotation, translation);

            if (inliers > bestInliers || (inliers == bestInliers && error < bestError))
            {
                bestInliers = inliers;
                bestRotation = rotation;
                bestTranslation = translation;
                bestError = error;
            }

            Debug.Log($"Iteration {i + 1}/{iterations}: Inliers = {inliers}, Error = {error:F4}");
        }

        Debug.Log($"Best Inliers: {bestInliers}\nTotal Iterations: {iterations}\nBest Error: {bestError:F4}");

        return (bestRotation, bestTranslation);
    }
}