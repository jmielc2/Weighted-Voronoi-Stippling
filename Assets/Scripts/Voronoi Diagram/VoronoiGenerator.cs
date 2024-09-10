using UnityEngine;
using UnityEngine.Experimental.Rendering;

[RequireComponent(typeof(Camera))]
public class VoronoiGenerator : MonoBehaviour {
    Material voronoiMaterial;
    RenderParams voronoiRP;
    GraphicsBuffer voronoiArgsBuffer;
    RenderTexture voronoiTarget;
    Texture2D voronoiTexture;
    bool voronoiGenerated;

    Material pointMaterial;
    RenderParams pointRP;
    GraphicsBuffer pointArgsBuffer;

    ComputeBuffer colorBuffer;
    ComputeBuffer positionsMatrixBuffer;
    PointManager pointManager;
    Bounds renderBounds;
    Camera cam;
    int numRegions;

    Mesh voronoiMesh;
    readonly static int colorBufferId = Shader.PropertyToID("_ColorBuffer"),
                    positionsMatrixBufferId = Shader.PropertyToID("_PositionsMatricBuffer");

    // TODO: Go through order of initialization to make sure things are ready when they need to be.
    // Lots of interwoven dependencies here (improve design?)
    public void Initialize(int numRegions, Color pointColor) {
        this.numRegions = numRegions;
        cam = GetComponent<Camera>();
        cam.enabled = false;
        voronoiGenerated = false;
        renderBounds = new Bounds(Vector3.zero, Vector3.one * 7f);
        voronoiMaterial = new Material(Shader.Find("Custom/Voronoi Shader"));
        voronoiRP = new RenderParams(voronoiMaterial) {
            camera = cam,
            worldBounds = renderBounds,
            receiveShadows = false,
            shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
        };
        pointManager = new PointManager(numRegions);
        CreateVoronoiTarget(Screen.width, Screen.height);
        voronoiTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBAFloat, false);

        pointMaterial = new Material(Shader.Find("Unlit/Point Shader")) {
            color = pointColor
        };
        pointRP = new RenderParams(pointMaterial) {
            camera = null,
            worldBounds = renderBounds,
            receiveShadows = false,
            shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off
        };
        pointManager = new PointManager(numRegions);
    }

    public void InitializeBuffers() {
        var args = new GraphicsBuffer.IndirectDrawIndexedArgs[1];

        // Load Buffers With Data
        { // Voronoi Buffer Setup
            args[0].instanceCount = (uint)numRegions;
            args[0].indexCountPerInstance = voronoiMesh.GetIndexCount(0);
            args[0].startIndex = 0;
            args[0].baseVertexIndex = voronoiMesh.GetBaseVertex(0);
            args[0].startInstance = 0;
            voronoiArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            voronoiArgsBuffer.SetData(args);
        }

        { // Point Buffer Setup
            Mesh pointMesh = pointManager.pointMesh;
            args[0].instanceCount = (uint)numRegions;
            args[0].indexCountPerInstance = pointMesh.GetIndexCount(0);
            args[0].startIndex = 0;
            args[0].baseVertexIndex = pointMesh.GetBaseVertex(0);
            args[0].startInstance = 0;
            pointArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            pointArgsBuffer.SetData(args);
        }
        colorBuffer = new ComputeBuffer(numRegions, sizeof(float) * 4);
        colorBuffer.SetData(pointManager.colors);
        positionsMatrixBuffer = new ComputeBuffer(numRegions, sizeof(float) * 16);
        positionsMatrixBuffer.SetData(pointManager.pointMatrices);

        // Assign Buffers to Materials
        voronoiMaterial.SetBuffer(colorBufferId, colorBuffer);
        voronoiMaterial.SetBuffer(positionsMatrixBufferId, positionsMatrixBuffer);
        pointMaterial.SetBuffer(positionsMatrixBufferId, positionsMatrixBuffer);
    }

    void OnValidate() {
        if (colorBuffer != null) {
            OnDisable();
        }
    }

    void OnDisable() {
        pointArgsBuffer.Release();
        voronoiArgsBuffer.Release();
        colorBuffer.Release();
        positionsMatrixBuffer.Release();
        voronoiTarget.Release();
        pointArgsBuffer = voronoiArgsBuffer = colorBuffer = positionsMatrixBuffer = voronoiTarget = null;
    }

    public Texture2D CreateVoronoiDiagram(int width, int height) {
        if (voronoiTexture.width != width || voronoiTexture.height != height) {
            voronoiTarget.Release();
            CreateVoronoiTarget(width, height);
            Texture2D voronoiTexture = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
            voronoiGenerated = false;
        }
        if (voronoiGenerated) {
            // TODO: Render to camera and retreive texture.
            cam.Render();
            voronoiGenerated = true;
        }
        return voronoiTexture;
    }

    public void DrawVoronoi() {
        if (!voronoiGenerated) {
            CreateVoronoiDiagram(voronoiTexture.width, voronoiTexture.height);
        }
        Graphics.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), voronoiTexture);
    }

    public void DrawPoints() {
        Graphics.RenderMeshIndirect(pointRP, pointManager.pointMesh, pointArgsBuffer);
    }

    public void Release() {
        voronoiArgsBuffer?.Release();
        voronoiArgsBuffer = null;
        pointArgsBuffer?.Release();
        pointArgsBuffer = null;
    }

    void CreateVoronoiTarget(int width, int height) {
        RenderTextureDescriptor rtDescriptor = new RenderTextureDescriptor(width, height) {
            // colorFormat = RenderTextureFormat.ARGB32,
            graphicsFormat = GraphicsFormat.R32G32B32A32_SFloat,
            depthBufferBits = 32,
            sRGB = false,
            useMipMap = false,
            autoGenerateMips = false,
        };
        voronoiTarget = new RenderTexture(rtDescriptor);
        voronoiTarget.Create();
        cam.targetTexture = voronoiTarget;
    }

    Mesh GenerateConeMesh() {
        Mesh mesh = new Mesh() {
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
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0, true, 0);
        return mesh;
    }
}
