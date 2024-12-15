using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FileTransformer : MonoBehaviour
{
    // Ayarlar: Rotasyon ve Translasyon
    public Vector3 rotationAngles = new Vector3(30f, 45f, 0f); // X, Y, Z eksenleri etrafında dönüş açısı (derece)
    public Vector3 translationVector = new Vector3(1.5f, 1.5f, 1.5f); // Translasyon (taşıma vektörü)

    // Dosya isimleri
    public string inputFileName = "fileP.txt";
    public string outputFileName = "fileQ.txt";

    [ContextMenu("Generate File Q")]
    public void GenerateFileQ()
    {
        Debug.Log("Starting file transformation process...");

        // fileP'yi yükle
        List<Vector3> pointsP = LoadPointCloud(inputFileName);
        if (pointsP.Count == 0)
        {
            Debug.LogError($"Failed to load points from {inputFileName}");
            return;
        }

        // fileQ'yu oluştur
        List<Vector3> pointsQ = TransformPoints(pointsP, rotationAngles, translationVector);

        // fileQ'yu kaydet
        SavePointCloud(pointsQ, outputFileName);
        Debug.Log($"Transformed points saved to {outputFileName}");
    }

    // fileP'yi yükler
    List<Vector3> LoadPointCloud(string fileName)
    {
        List<Vector3> points = new List<Vector3>();
        string filePath = Path.Combine(Application.dataPath, fileName);

        if (!File.Exists(filePath))
        {
            Debug.LogError($"File not found: {filePath}");
            return points;
        }

        string[] lines = File.ReadAllLines(filePath);
        int numPoints = int.Parse(lines[0].Trim());

        for (int i = 1; i <= numPoints; i++)
        {
            string[] coords = lines[i].Trim().Split(' ');
            if (coords.Length == 3)
            {
                float x = float.Parse(coords[0]);
                float y = float.Parse(coords[1]);
                float z = float.Parse(coords[2]);
                points.Add(new Vector3(x, y, z));
            }
        }

        return points;
    }

    // Noktaları rotasyon ve translasyon uygular
    List<Vector3> TransformPoints(List<Vector3> points, Vector3 rotation, Vector3 translation)
    {
        List<Vector3> transformedPoints = new List<Vector3>();

        // Rotasyon matrisini oluştur
        Quaternion rotationQuat = Quaternion.Euler(rotation);

        foreach (var point in points)
        {
            // Rotasyon ve translasyon uygula
            Vector3 transformedPoint = rotationQuat * point + translation;
            transformedPoints.Add(transformedPoint);
        }

        return transformedPoints;
    }

    // fileQ'yu kaydeder
    void SavePointCloud(List<Vector3> points, string fileName)
    {
        string filePath = Path.Combine(Application.dataPath, fileName);
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine(points.Count); // İlk satırda nokta sayısı
            foreach (var point in points)
            {
                writer.WriteLine($"{point.x:F4} {point.y:F4} {point.z:F4}");
            }
        }
    }
}