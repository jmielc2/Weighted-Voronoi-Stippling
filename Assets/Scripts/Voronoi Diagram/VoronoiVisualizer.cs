using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class VoronoiVisualizer : MonoBehaviour {
    [SerializeField, Range(1, 20000)]
    int numRegions;

    // Private Member Variables
    Camera cam;
    Material material, pointMaterial;
    DataManager data;
    RenderParams rp;
    GraphicsBuffer argsBuffer;
    ComputeBuffer positionBuffer;
    ComputeBuffer colorBuffer;
    Bounds renderBounds;
    RenderTexture rt;
    Texture2D texture;
    bool validating = false;

    void OnValidate() {
        Debug.Log("Validating");
        validating = true;
        if (argsBuffer != null) {
            OnDisable();
            OnEnable();
        }
        validating = false;
        Debug.Log("Done validating");
    }

    void Awake() {
        Debug.Log("Awake");
        cam = GetComponent<Camera>();
        renderBounds = new Bounds(Vector3.zero, Vector3.one * 3f);
        DataManager.CreatePointMesh();
        DataManager.CreateConeMesh(cam);
    }

    // Runs when element is enabled
    void OnEnable() {
        Debug.Log("Enabling");
        data = new DataManager(numRegions, cam);
        CreateBuffers();
        LoadBuffers();
        ConfigureRenderPass();
        RenderToTexture();
    }

    void OnDisable() {
        Debug.Log("Disabling");
        argsBuffer?.Release();
        argsBuffer = null;
        positionBuffer?.Release();
        positionBuffer = null;
        colorBuffer?.Release();
        colorBuffer = null;
        texture = null;
        DestroyRenderTexture();
    }

    void Update() {
        if (rt == null) {
            RenderToTexture();
        }

        if (Input.GetKeyDown(KeyCode.S)) {
            Debug.Log("Writing texture to file.");
            texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false, true) {
                filterMode = FilterMode.Point,
            };
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes("./Documents/voronoi-diagram.png", texture.EncodeToPNG());
            texture = null;
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Graphics.Blit(rt, destination);
    }

    void RenderToTexture() {
        if (validating) {
            return;
        }
        CreateRenderTexture();
        Debug.Log("Prerendering texture");
        cam.targetTexture = rt;
        RenderTexture.active = rt;
        Graphics.RenderMeshIndirect(rp, data.ConeMesh, argsBuffer);
        cam.Render();
        RenderTexture.active = null;
        cam.targetTexture = null;
    }

    void ConfigureRenderPass() {
        Debug.Log("Configuring renderer.");
        material = new Material(Shader.Find("Custom/Indirect Voronoi Shader"));
        material.SetBuffer(Shader.PropertyToID("_PositionMatrixBuffer"), positionBuffer);
        material.SetBuffer(Shader.PropertyToID("_ColorBuffer"), colorBuffer);
        rp = new RenderParams(material) {
            camera = cam,
            receiveShadows = false,
            worldBounds = renderBounds,
            shadowCastingMode = ShadowCastingMode.Off
        };
    }

    void LoadBuffers() {
        Debug.Log("Loading buffers.");
        // Load Command Buffer
        {
            Mesh mesh = data.ConeMesh;
            GraphicsBuffer.IndirectDrawIndexedArgs[] args = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
            args[0].baseVertexIndex = mesh.GetBaseVertex(0);
            args[0].indexCountPerInstance = mesh.GetIndexCount(0);
            args[0].instanceCount = (uint)numRegions;
            args[0].startIndex = mesh.GetIndexStart(0);
            args[0].startInstance = 0;
            argsBuffer.SetData(args);
        }
        
        // Load Positions & Colors
        {
            positionBuffer.SetData(data.ConeMatrices);
            colorBuffer.SetData(data.Colors);
        }
    }

    void CreateBuffers() {
        Debug.Log("Creating buffers.");
        argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        positionBuffer = new ComputeBuffer(numRegions, sizeof(float) * 16);
        colorBuffer = new ComputeBuffer(numRegions, sizeof(float) * 3);
    }

    void CreateRenderTexture() {
        Debug.Log("Creating render texture.");
        var rtDescriptor = new RenderTextureDescriptor(cam.pixelWidth, cam.pixelHeight, RenderTextureFormat.ARGBFloat) {
            depthBufferBits = 32,
            useMipMap = false,
        };
        rt = new RenderTexture(rtDescriptor) {
            filterMode = FilterMode.Point
        };
        rt.Create();
    }

    void DestroyRenderTexture() {
        rt?.Release();
        rt = null;
    }
}
