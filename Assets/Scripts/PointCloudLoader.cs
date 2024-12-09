using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PointCloudLoader : MonoBehaviour
{
    public List<Vector3> LoadPointCloud(string fileName)
    {
        List<Vector3> points = new List<Vector3>();
        string path = Path.Combine(Application.dataPath, "Resources", fileName);

        if (!File.Exists(path))
        {
            Debug.LogError("File not found: " + path);
            return points;
        }

        string[] lines = File.ReadAllLines(path);
        int numPoints = int.Parse(lines[0]);

        for (int i = 1; i <= numPoints; i++)
        {
            string[] coords = lines[i].Split(' ');
            float x = float.Parse(coords[0]);
            float y = float.Parse(coords[1]);
            float z = float.Parse(coords[2]);
            points.Add(new Vector3(x, y, z));
        }

        return points;
    }
}