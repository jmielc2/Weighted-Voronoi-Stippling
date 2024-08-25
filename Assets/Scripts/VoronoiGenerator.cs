using UnityEngine;
using Unity.Collections;

[RequireComponent(typeof(Camera))]
public class VoronoiGenerator : MonoBehaviour {
    // Control Parameters
    [SerializeField]
    Material material;
    [SerializeField]
    Material pointMaterial;
    [SerializeField]
    Mesh pointMesh;
    [SerializeField, Range(1, 1023)]
    int numPoints;
    [SerializeField]
    bool showPoints = true;

    // Private Member Variables
    Mesh coneMesh;
    RenderParams renderParams;
    ComputeBuffer colorBuffer;
    NativeArray<Vector3> colors;
    Matrix4x4[] coneMatrices;
    Matrix4x4[] pointMatrices;
    Camera cam;
    Texture2D voronoiTexture;
    bool saveImage = false;

    // Constant and Statice Member Variables
    const float pointScale = 0.025f;
    readonly static int colorBufferId = Shader.PropertyToID("_ColorBuffer");

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
        GeneratePoints();
        GenerateConeMesh();
        material.SetBuffer(colorBufferId, colorBuffer);
        renderParams = new RenderParams();
        renderParams.worldBounds = new Bounds(Vector3.zero, Vector3.one * 7f);
        voronoiTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        Camera.onPostRender += OnPostRenderCallback;
    }

    // Runs when element is disabled
    void OnDisable() {
        colors.Dispose();
        colorBuffer.Release();
        colorBuffer = null;
    }

    // Generates point used to create Voronoi Diagram
    void GeneratePoints() {
        colorBuffer = new ComputeBuffer(numPoints, sizeof(float) * 3);
        colors = new NativeArray<Vector3>(numPoints, Allocator.Persistent);
        coneMatrices = new Matrix4x4[numPoints];
        pointMatrices = new Matrix4x4[numPoints];
        Vector3 position = Vector3.zero;
        Quaternion pointRotation = Quaternion.Euler(0f, 0f, 45f);
        for (int i = 0; i < numPoints; i++) {
            // Calculate Cone Matrix
            position.x = Random.Range(-1f, 1f) * cam.aspect;
            position.y = Random.Range(-1f, 1f);
            position.z = 0f;
            coneMatrices[i] = Matrix4x4.Translate(position);
            // Calculate Point Matrix
            position.z = pointScale * 0.5f;
            pointMatrices[i] = Matrix4x4.TRS(position, pointRotation, Vector3.one * pointScale);
            // Assign Unique Color
            colors[i] = new Vector3(
                Random.Range(0f, 1f),
                Random.Range(0f, 1f),
                (float)i / (float)numPoints
            );
        }
        colorBuffer.SetData(colors);
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
        renderParams.material = material;
        Graphics.RenderMeshInstanced(renderParams, coneMesh, 0, coneMatrices);
        
        if (showPoints) {
            renderParams.material = pointMaterial;
            Graphics.RenderMeshInstanced(renderParams, pointMesh, 0, pointMatrices);
        }
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            saveImage = true;
        }
        GenerateVoronoi();
    }

    void OnPostRenderCallback(Camera renderCam) {
        if (saveImage) {
            Rect regionToReadFrom = new Rect(0, 0, Screen.width, Screen.height);
            voronoiTexture.ReadPixels(regionToReadFrom, 0, 0, false);
            voronoiTexture.Apply();
            System.IO.File.WriteAllBytes("./Documents/voronoi.png", voronoiTexture.EncodeToPNG());
            saveImage = false;
        }
    }
}
