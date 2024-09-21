using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class VoronoiVisualizer : MonoBehaviour {
    [SerializeField, Range(1, 20000)]
    protected int numRegions = 100;
    [SerializeField]
    protected bool showPoints = true;
    [SerializeField]
    protected Color pointColor = new(0.1f, 0.1f, 0.1f, 1f);
    [SerializeField]
    protected Material material, pointMaterial;

    // Private Member Variables
    protected Camera cam;
    protected DataManager data;
    protected RenderParams rp;
    protected GraphicsBuffer argsBuffer, pointArgsBuffer;
    protected ComputeBuffer positionBuffer, pointPositionBuffer, colorBuffer;
    protected Bounds renderBounds;
    protected RenderTexture rt;
    protected Texture2D texture;
    protected bool validating = false;

    protected readonly static int positionBufferId = Shader.PropertyToID("_PositionMatrixBuffer"),
                        colorBufferId = Shader.PropertyToID("_ColorBuffer"),
                        colorId = Shader.PropertyToID("_Color");
                        

    protected void OnValidate() {
        Debug.Log("Validating");
        validating = true;
        if (argsBuffer != null) {
            OnDisable();
            OnEnable();
        }
        validating = false;
    }

    protected void Awake() {
        Debug.Log("Awake");
        cam = GetComponent<Camera>();
        renderBounds = new Bounds(Vector3.zero, Vector3.one * 3f);
        DataManager.CreatePointMesh();
        DataManager.CreateConeMesh(cam);
    }

    protected void OnEnable() {
        Debug.Log("Enabling");
        if (data == null || data.NumPoints != numRegions) {
            data = new DataManager(numRegions, cam);
        }
        CreateBuffers();
        LoadBuffers();
        ConfigureRenderPass();
        RenderToTexture();
    }

    protected void OnDisable() {
        Debug.Log("Disabling");
        argsBuffer?.Release();
        argsBuffer = null;
        pointArgsBuffer?.Release();
        pointArgsBuffer = null;
        positionBuffer?.Release();
        positionBuffer = null;
        pointPositionBuffer?.Release();
        pointPositionBuffer = null;
        colorBuffer?.Release();
        colorBuffer = null;
        texture = null;
        DestroyRenderTexture();
    }

    private void Update() {
        if (rt == null) {
            RenderToTexture();
        }

        if (Input.GetKeyDown(KeyCode.S)) {
            Debug.Log("Writing texture to file.");
            texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false) {
                filterMode = FilterMode.Point,
            };
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes("./Documents/voronoi-diagram.png", texture.EncodeToPNG());
            texture = null;
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Graphics.Blit(rt, destination);
    }

    protected virtual void RenderToTexture() {
        if (validating) {
            return;
        }
        CreateRenderTexture();
        Debug.Log("Prerendering texture");
        cam.targetTexture = rt;
        RenderTexture.active = rt;
        rp.material = material;
        Graphics.RenderMeshIndirect(rp, data.ConeMesh, argsBuffer);
        if (showPoints) {
            rp.material = pointMaterial;
            Graphics.RenderMeshIndirect(rp, data.PointMesh, pointArgsBuffer);
        }
        cam.Render();
        RenderTexture.active = null;
        cam.targetTexture = null;
    }

    protected virtual void ConfigureRenderPass() {
        Debug.Log("Configuring renderer.");
        // Voronoi Material
        // material = new Material(Shader.Find("Custom/Indirect Voronoi Shader"));
        material.SetBuffer(positionBufferId, positionBuffer);
        material.SetBuffer(colorBufferId, colorBuffer);

        // Point Material
        // pointMaterial = new Material(Shader.Find("Unlit/Indirect Point Shader"));
        pointMaterial.SetBuffer(positionBufferId, pointPositionBuffer);
        pointMaterial.SetVector(colorId, pointColor);

        rp = new RenderParams() {
            camera = cam,
            receiveShadows = false,
            worldBounds = renderBounds,
            shadowCastingMode = ShadowCastingMode.Off
        };
    }

    protected virtual void CreateBuffers() {
        Debug.Log("Creating buffers.");
        argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        pointArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        positionBuffer = new ComputeBuffer(numRegions, sizeof(float) * 16);
        pointPositionBuffer = new ComputeBuffer(numRegions, sizeof(float) * 16);
        colorBuffer = new ComputeBuffer(numRegions, sizeof(float) * 3);
    }

    protected virtual void LoadBuffers() {
        Debug.Log("Loading buffers.");
        // Load Command Buffer
        {
            LoadArgBuffer(argsBuffer, data.ConeMesh);
            LoadArgBuffer(pointArgsBuffer, data.PointMesh);
        }
        
        // Load Positions & Colors
        {
            positionBuffer.SetData(data.ConeMatrices);
            pointPositionBuffer.SetData(data.PointMatrices);
            colorBuffer.SetData(data.Colors);
        }
    }

    protected virtual void LoadArgBuffer(GraphicsBuffer buffer, Mesh mesh) {
        GraphicsBuffer.IndirectDrawIndexedArgs[] args = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        args[0].baseVertexIndex = mesh.GetBaseVertex(0);
        args[0].indexCountPerInstance = mesh.GetIndexCount(0);
        args[0].instanceCount = (uint)numRegions;
        args[0].startIndex = mesh.GetIndexStart(0);
        args[0].startInstance = 0;
        buffer.SetData(args);
    }

    protected virtual void CreateRenderTexture() {
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

    protected virtual void DestroyRenderTexture() {
        rt?.Release();
        rt = null;
    }
}
