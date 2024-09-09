using UnityEngine;
using Unity.Collections;

[RequireComponent(typeof(Camera))]
public class VoronoiVisualizer : MonoBehaviour {
    // Control Parameters
    [SerializeField]
    Material material;
    [SerializeField]
    Color pointColor;
    [SerializeField, Range(1, 2000)]
    int numPoints;
    [SerializeField]
    bool showPoints = true;

    // Private Member Variables
    Mesh coneMesh;
    RenderParams renderParams;
    ComputeBuffer colorBuffer;
    ComputeBuffer positionsMatrixBuffer;
    Camera cam;
    bool saveImage = false;
    PointManager pointManager;
    Rect screenRect;

    // Constant and Statice Member Variables
    readonly static int colorBufferId = Shader.PropertyToID("_ColorBuffer"),
                        positionsMatrixBufferId = Shader.PropertyToID("_PositionsMatricBuffer");

    // Runs when parameter is changed in editor during play
    void OnValidate() {
        if (colorBuffer != null) {
            OnDisable();
            OnEnable();
        }
    }

    // Runs when element is enabled
    void OnEnable() {
        cam = GetComponent<Camera>();
        pointManager = new PointManager(numPoints, pointColor);
        pointManager.GeneratePoints();
        GenerateConeMesh();
        renderParams = new RenderParams(material) {
            worldBounds = new Bounds(Vector3.zero, Vector3.one * 7f)
        };
        screenRect = new Rect(0, 0, Screen.width, Screen.height);

        colorBuffer = new ComputeBuffer(numPoints, sizeof(float) * 3);
        colorBuffer.SetData(pointManager.GetColors());
        positionsMatrixBuffer = new ComputeBuffer(numPoints, sizeof(float) * 16);
        positionsMatrixBuffer.SetData(pointManager.GetMatrices());
        material.SetBuffer(colorBufferId, colorBuffer);
        material.SetBuffer(positionsMatrixBufferId, positionsMatrixBuffer);
    }

    // Runs when element is disabled
    void OnDisable() {
        colorBuffer.Release();
        colorBuffer = null;
        positionsMatrixBuffer.Release();
        positionsMatrixBuffer = null;
        pointManager.Release();
        pointManager = null;
    }

    void GenerateConeMesh() {
        coneMesh = new Mesh();
        coneMesh.subMeshCount = 1;

        // Calculate Minimum Number of Cone Slices
        float radius = Mathf.Sqrt(cam.pixelWidth * cam.pixelWidth + cam.pixelHeight * cam.pixelHeight);
        float maxAngle = 2f * Mathf.Acos((radius - 1f) / radius);
        int numSlices = Mathf.CeilToInt((2f * Mathf.PI) / maxAngle);

        // Generate Mesh
        Vector3[] vertices = new Vector3[numSlices + 1];
        int[] triangles = new int[numSlices * 3];
        vertices[0] = Vector3.zero;
        float angle = 0f;
        float width = cam.aspect * 2f;
        radius = Mathf.Sqrt(width * width + 4);
        for (int i = 1; i < numSlices + 1; i++) {
            vertices[i] = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                1f
            );
            angle -= maxAngle;
        }
        for (int i = 0; i < numSlices; i++) {
            triangles[i * 3] = 0;
            triangles[(i * 3) + 1] = i;
            triangles[(i * 3) + 2] = i + 1;
        }
        triangles[((numSlices - 1) * 3) + 2] = 1;
        coneMesh.SetVertices(vertices);
        coneMesh.SetTriangles(triangles, 0, true, 0);
    }

    void GenerateVoronoi() {
        
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.S)) {
            saveImage = true;
        }
        GenerateVoronoi();

        if (showPoints) {
            pointManager.DrawPoints();
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (saveImage) {
            // voronoiTexture.ReadPixels(regionToReadFrom, 0, 0, false);
            // voronoiTexture.Apply();
            // System.IO.File.WriteAllBytes("./Documents/voronoi.png", voronoiTexture.EncodeToPNG());
            saveImage = false;
        }
        Graphics.Blit(source, destination);
    }
}
