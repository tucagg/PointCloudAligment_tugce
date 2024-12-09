using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PointCloudManager : MonoBehaviour
{
    public Button applyRANSACButton;
    public Button resetButton;
    public Button switchRegistrationButton; // Button to switch registration method
    public Button showLineButton; // Button to toggle lines
    public TMP_Text infoText;

    private PointCloudLoader loader;
    private PointCloudVisualizer visualizer;

    private List<Vector3> P;  // First set of points
    private List<Vector3> Q;  // Second set of points
    private List<GameObject> visualizedPoints = new List<GameObject>();
    private List<Vector3> transformedQ = new List<Vector3>();  // Transformed points (after registration)
    private List<LineRenderer> movementLines = new List<LineRenderer>();  // To visualize movement as lines

    private bool registerSecondToFirst = true; // Default: Q -> P registration
    private bool showMovementLines = false; // Controls whether to show movement lines

    void Start()
    {
        loader = gameObject.AddComponent<PointCloudLoader>();
        visualizer = gameObject.AddComponent<PointCloudVisualizer>();

        // Load point clouds and visualize them
        P = loader.LoadPointCloud("fileP.txt");
        Q = loader.LoadPointCloud("fileQ.txt");

        VisualizeOriginalPoints();  // Visualize original points

        // Assign button functions
        applyRANSACButton.onClick.AddListener(ApplyRANSAC);
        resetButton.onClick.AddListener(ResetVisualization);
        switchRegistrationButton.onClick.AddListener(SwitchRegistrationMethod);
        showLineButton.onClick.AddListener(ToggleMovementLines);
    }

    void VisualizeOriginalPoints()
    {
        ClearVisualizedPoints();
        visualizedPoints.AddRange(visualizer.VisualizePoints(P, Color.red, 0.1f));  // Original P points in red
        visualizedPoints.AddRange(visualizer.VisualizePoints(Q, Color.blue, 0.1f)); // Original Q points in blue

        // If there are transformed points, visualize them
        if (transformedQ.Count > 0)
        {
            visualizedPoints.AddRange(visualizer.VisualizePoints(transformedQ, Color.green, 0.1f)); // Transformed points in green
        }

        // If the movement lines are enabled, draw them
        if (showMovementLines)
        {
            DrawMovementLines();
        }

        infoText.text = "Original Points Loaded.";
    }

    void ApplyRANSAC()
{
    // Ensure we have enough points
    if (P.Count == 0 || Q.Count == 0)
    {
        infoText.text = "Point clouds are empty. Cannot apply RANSAC.";
        return;
    }

    // Apply RANSAC for point registration
    var (rotation, translation) = RANSAC.PerformRANSAC(P, Q);

    // Clear previous transformed points and calculate the new ones
    transformedQ.Clear();

    if (registerSecondToFirst) // Register Q to P
    {
        foreach (var point in Q)
        {
            Vector3 transformedPoint = rotation.MultiplyPoint3x4(point) + translation;
            transformedQ.Add(transformedPoint);
        }
    }
    else // Register P to Q
    {
        foreach (var point in P)
        {
            Vector3 transformedPoint = rotation.MultiplyPoint3x4(point) + translation;
            transformedQ.Add(transformedPoint);
        }
    }

    // Re-visualize with the newly transformed points
    VisualizeOriginalPoints();

    // Format the transformation parameters
    string rotationMatrix = $"[{rotation.m00:F2}, {rotation.m01:F2}, {rotation.m02:F2}]\n" +
                            $"[{rotation.m10:F2}, {rotation.m11:F2}, {rotation.m12:F2}]\n" +
                            $"[{rotation.m20:F2}, {rotation.m21:F2}, {rotation.m22:F2}]";

    string translationVector = $"[{translation.x:F2}, {translation.y:F2}, {translation.z:F2}]";

    float scale = CalculateScale(rotation);

    // Display transformation parameters
    infoText.text = "RANSAC Applied.\n" +
                    "Rotation Matrix:\n" + rotationMatrix + "\n" +
                    "Translation Vector:\n" + translationVector + "\n" +
                    $"Scale: {scale:F2}";
}

    void ResetVisualization()
    {
        // Clear only green (transformed) points
        foreach (var obj in visualizedPoints)
        {
            if (obj != null && obj.GetComponent<Renderer>().material.color == Color.green)
            {
                Destroy(obj);
            }
        }
        visualizedPoints.RemoveAll(obj => obj == null);

        // Clear all movement lines
        foreach (var line in movementLines)
        {
            Destroy(line.gameObject);
        }
        movementLines.Clear();
        showMovementLines = false; // Reset toggle state

        // Clear transformed points
        transformedQ.Clear();

        // Visualize original points
        VisualizeOriginalPoints();
        infoText.text = "Visualization Reset.";
    }

    void SwitchRegistrationMethod()
    {
        // Toggle registration method (Q -> P or P -> Q)
        registerSecondToFirst = !registerSecondToFirst;
        infoText.text = registerSecondToFirst ? "Registering second to first" : "Registering first to second";
    }

    void ToggleMovementLines()
    {
        if (showMovementLines)
        {
            // Hide the movement lines
            foreach (var line in movementLines)
            {
                Destroy(line.gameObject); // Remove all line objects
            }
            movementLines.Clear();
            showMovementLines = false; // Update the toggle state
            infoText.text = "Movement lines hidden.";
        }
        else
        {
            // Show the movement lines
            DrawMovementLines();
            showMovementLines = true; // Update the toggle state
            infoText.text = "Movement lines displayed.";
        }
    }

    void DrawMovementLines()
    {
        // Clear previous movement lines if any
        foreach (var line in movementLines)
        {
            Destroy(line.gameObject);
        }
        movementLines.Clear();

        // Draw new lines between corresponding points
        for (int i = 0; i < P.Count; i++)
        {
            if (i >= transformedQ.Count) break; // Prevent out-of-range errors

            GameObject lineObject = new GameObject("MovementLine");
            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.02f;
            lineRenderer.endWidth = 0.02f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default")) { color = Color.yellow }; // Yellow lines

            // Set line positions (from P[i] to transformedQ[i])
            lineRenderer.SetPosition(0, P[i]);
            lineRenderer.SetPosition(1, transformedQ[i]);

            movementLines.Add(lineRenderer); // Add to list for toggling
        }
    }

    float CalculateScale(Matrix4x4 rotation)
    {
        if (P.Count < 2 || transformedQ.Count < 2)
        {
            return 1.0f; // Default scale when not enough points are available
        }

        Vector3 originalSize = P[1] - P[0];
        Vector3 transformedSize = transformedQ[1] - transformedQ[0];
        return transformedSize.magnitude / originalSize.magnitude;
    }

    void ClearVisualizedPoints()
    {
        // Clear previously visualized points
        foreach (var obj in visualizedPoints)
        {
            Destroy(obj);
        }
        visualizedPoints.Clear();
    }
}