using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointCloudVisualizer : MonoBehaviour
{
    public List<GameObject> VisualizePoints(List<Vector3> points, Color color, float size = 0.1f)
    {
        List<GameObject> visualizedPoints = new List<GameObject>();

        foreach (var point in points)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = point;
            sphere.transform.localScale = new Vector3(size, size, size);
            sphere.GetComponent<Renderer>().material.color = color;
            visualizedPoints.Add(sphere);
        }

        return visualizedPoints;
    }
}