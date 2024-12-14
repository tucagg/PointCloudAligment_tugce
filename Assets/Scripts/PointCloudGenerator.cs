using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PointCloudGenerator : MonoBehaviour
{
    public string filePPath = "fileP.txt";  // fileP.txt dosyasının yolu
    public string fileQPath = "fileQ.txt"; // fileQ.txt dosyasının yolu

    [ContextMenu("Generate File Q")]
    public void GenerateFileQ()
    {
        // fileP.txt'yi oku
        if (!File.Exists(filePPath))
        {
            Debug.LogError($"File not found: {filePPath}");
            return;
        }

        string[] lines = File.ReadAllLines(filePPath);
        int numPointsP = int.Parse(lines[0].Trim());
        List<Vector3> pointsP = new List<Vector3>();

        for (int i = 1; i <= numPointsP; i++)
        {
            string[] splitLine = lines[i].Trim().Split(' ');
            float x = float.Parse(splitLine[0]);
            float y = float.Parse(splitLine[1]);
            float z = float.Parse(splitLine[2]);
            pointsP.Add(new Vector3(x, y, z));
        }

        int totalPointsQ = Random.Range(numPointsP, numPointsP + 21);
        int exactMatchMin = Mathf.CeilToInt(totalPointsQ / 2f);
        int exactMatchCount = Random.Range(exactMatchMin, totalPointsQ + 1);
        int randomPointCount = totalPointsQ - exactMatchCount;

        List<Vector3> exactMatchPoints = new List<Vector3>();
        for (int i = 0; i < exactMatchCount; i++)
        {
            exactMatchPoints.Add(pointsP[Random.Range(0, pointsP.Count)]);
        }

        List<Vector3> randomPoints = new List<Vector3>();
        for (int i = 0; i < randomPointCount; i++)
        {
            float x = Random.Range(-5f, 5f);
            float y = Random.Range(-5f, 5f);
            float z = Random.Range(-5f, 5f);
            randomPoints.Add(new Vector3(x, y, z));
        }

        List<Vector3> pointsQ = new List<Vector3>();
        pointsQ.AddRange(exactMatchPoints);
        pointsQ.AddRange(randomPoints);

        for (int i = pointsQ.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            Vector3 temp = pointsQ[i];
            pointsQ[i] = pointsQ[randomIndex];
            pointsQ[randomIndex] = temp;
        }

        using (StreamWriter writer = new StreamWriter(fileQPath))
        {
            writer.WriteLine(pointsQ.Count);
            foreach (Vector3 point in pointsQ)
            {
                writer.WriteLine($"{point.x:F2} {point.y:F2} {point.z:F2}");
            }
        }

        Debug.Log($"Generated {fileQPath} with {pointsQ.Count} points. Exact Matches: {exactMatchCount}, Random Points: {randomPointCount}");
    }
}