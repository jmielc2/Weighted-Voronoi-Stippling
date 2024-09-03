using UnityEngine;
using Unity.Collections;

[RequireComponent(typeof(Camera))]
public class CentroidalVoronoiGenerator : MonoBehaviour {
    // Control Parameters
    [SerializeField]
    Material material;
    [SerializeField]
    Material pointMaterial;
    [SerializeField, Range(1, 1024)]
    int numPoints;
    [SerializeField]
    Mesh pointMesh;
    [SerializeField]
    bool showPoints = true;
    [SerializeField]
    Material centroidMaterial;
    [SerializeField]
    bool showCentroids = true;
    [SerializeField]
    Camera voronoiCam;

    // Private Member Variables
    Mesh coneMesh;
    RenderParams renderParams;
    ComputeBuffer colorBuffer;
    NativeArray<Vector3> colors;
    Matrix4x4[] coneMatrices;
    Matrix4x4[] pointMatrices;
    Matrix4x4[] centroidMatrices;
    Camera cam;
    RenderTexture voronoiTextureTarget;
    Texture2D voronoiTexture;
    bool saveImage = false;
    bool calcCentroid = true;
    Rect screenRegionRect;

    // Constant and Static Member Variables
    const float pointScale = 0.025f;
    const float centroidScale = 0.02f;
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
        calcCentroid = true;
        cam = GetComponent<Camera>();
        GeneratePoints();
        GenerateConeMesh();
        material.SetBuffer(colorBufferId, colorBuffer);
        renderParams = new RenderParams() {
            worldBounds = new Bounds(Vector3.zero, Vector3.one * 7f)
        };
        int diagramWidth = Screen.width / 4;
        int diagramHeight = Screen.height / 4;
        screenRegionRect = new Rect(0, 0, Screen.width, Screen.height);
        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(diagramWidth, diagramHeight, RenderTextureFormat.ARGBFloat, 32) {
            sRGB = false
        };
        voronoiTexture = new Texture2D(diagramWidth, diagramHeight, TextureFormat.RGBAFloat, false);
        // voronoiTextureTarget = new RenderTexture(diagramWidth, diagramHeight, 32 * 4, RenderTextureFormat.ARGBFloat);
        voronoiTextureTarget = new RenderTexture(descriptor);
        voronoiTextureTarget.Create();
        voronoiCam.CopyFrom(cam);
        voronoiCam.targetTexture = voronoiTextureTarget;
    }

    // Runs when element is disabled
    void OnDisable() {
        colors.Dispose();
        colorBuffer.Release();
        colorBuffer = null;
        voronoiTextureTarget.Release();
        voronoiTextureTarget = null;
    }

    // Calculates the centroid positions for each voronoi section
    void CalculateCentroids() {
        // TODO: Centroid calculation works, but could be improved in performance.
        Vector2[] centroidPositions = new Vector2[numPoints];
        int[] counts = new int[numPoints];
        for (int y = 0; y < voronoiTexture.height; y++) {
            for (int x = 0; x < voronoiTexture.width; x++) {
                float colorB = voronoiTexture.GetPixel(x, y).b + (0.5f / numPoints);
                // float colorB = voronoiTexture.GetPixel(x, y).b;
                // Debug.Log(voronoiTexture.GetPixel(x, y).b);
                int index = Mathf.FloorToInt(colorB * numPoints);
                centroidPositions[index] += new Vector2(
                    (x + 0.5f) / (float)voronoiTexture.width,
                    (y + 0.5f) / (float)voronoiTexture.height
                );
                counts[index]++;
            }
        }
        for (int i = 0; i < numPoints; i++) {
            Vector3 centroid = Vector3.zero;
            if (counts[i] == 0) {
                centroidMatrices[i] = pointMatrices[i];
                Debug.Log($"Index {i} has count of 0.");
            } else {
                float scalar = 2f / counts[i];
                centroid.x = (centroidPositions[i].x * scalar - 1f) * cam.aspect;
                centroid.y = centroidPositions[i].y * scalar - 1f;
                centroidPositions[i] = centroid;
                centroid.z = centroidScale * 0.5f;
                centroidMatrices[i] = Matrix4x4.TRS(centroid, Quaternion.identity, Vector3.one * centroidScale);
            }
        }
        Debug.Log("Centroids generated.");
        UpdatePoints(centroidPositions);
    }

    void UpdatePoints(Vector2[] centroidPositions) {
        // TODO: Update points to move towards the centroid positions.
        for(int i = 0; i < centroidPositions.Length; i++) {
            Vector3 centroid = centroidPositions[i];
            Vector3 point = coneMatrices[i].GetColumn(3);
        }
    }

    void GeneratePoints() {
        colorBuffer = new ComputeBuffer(numPoints, sizeof(float) * 3);
        colors = new NativeArray<Vector3>(numPoints, Allocator.Persistent);
        coneMatrices = new Matrix4x4[numPoints];
        pointMatrices = new Matrix4x4[numPoints];
        centroidMatrices = new Matrix4x4[numPoints];
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
            // Assign Unique Color (will be used when calculating centroid)
            colors[i] = new Vector3(
                Random.Range(0f, 1f),
                Random.Range(0f, 1f),
                i / (float)numPoints
            );
        }

        colorBuffer.SetData(colors);
    }

    void GenerateConeMesh() {
        coneMesh = new Mesh() {
            subMeshCount = 1
        };

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

        // Render To Texture
        var currentRT = RenderTexture.active;
        RenderTexture.active = voronoiTextureTarget;
        Graphics.RenderMeshInstanced(renderParams, coneMesh, 0, coneMatrices);
        voronoiCam.Render();
        voronoiTexture.ReadPixels(screenRegionRect, 0, 0);
        voronoiTexture.Apply();
        RenderTexture.active = currentRT;

        // Render To Screen
        Graphics.DrawTexture(screenRegionRect, voronoiTexture);
    }

    void Update() {
        // Get User Input
        if (Input.GetKeyDown(KeyCode.S)) {
            saveImage = true;
        }
        if (Input.GetKeyDown(KeyCode.C)) {
            calcCentroid = true;
        }

        // Generate Voronoi Texture
        GenerateVoronoi();

        // Update Centroids
        if (calcCentroid) {
            CalculateCentroids();
            calcCentroid = false;
        }

        // Render Points
        if (showPoints) {
            renderParams.material = pointMaterial;
            Graphics.RenderMeshInstanced(renderParams, pointMesh, 0, pointMatrices);
        }
        if (showCentroids) {
            renderParams.material = centroidMaterial;
            Graphics.RenderMeshInstanced(renderParams, pointMesh, 0, centroidMatrices);
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (saveImage) {
            var currentRT = RenderTexture.active;
            RenderTexture.active = source;
            Texture2D screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBAFloat, false);
            screenshot.ReadPixels(screenRegionRect, 0, 0);
            screenshot.Apply();
            System.IO.File.WriteAllBytes("./Documents/centroidal-voronoi.png", screenshot.EncodeToPNG());
            saveImage = false;
            currentRT = RenderTexture.active;
        }
        Graphics.Blit(source, destination);
    }
}
