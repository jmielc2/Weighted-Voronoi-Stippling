using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class TestIndirectInstancing : MonoBehaviour {
    Camera cam;

    [SerializeField] Mesh mesh;
    [SerializeField] Material material;
    [SerializeField, Range(10, 500)] int resolution;

    RenderParams rp;
    GraphicsBuffer argsBuffer;
    ComputeBuffer positionBuffer;
    ComputeBuffer colorBuffer;
    int numItems;
    float scale;

    void OnValidate() {
        Debug.Log("Validate");
        if (argsBuffer != null) {
            OnDisable();
            OnEnable();
        }
    }

    void Awake() {
        Debug.Log("Awake");
        cam = GetComponent<Camera>();
    }

    void OnEnable() {
        Debug.Log("Enable");
        numItems = resolution * resolution;
        scale = 1f / resolution;
        CreateBuffers();
        LoadBuffers();
        ConfigureRenderPass();
    }

    void OnDisable() {
        Debug.Log("Disable");
        argsBuffer?.Release();
        argsBuffer = null;
        positionBuffer?.Release();
        positionBuffer = null;
        colorBuffer?.Release();
        colorBuffer = null;
    }

    void Update() {
        // Debug.Log("Update");
        Graphics.RenderMeshIndirect(rp, mesh, argsBuffer);
    }



    void ConfigureRenderPass() {
        Debug.Log("Configure Renderer");
        material.SetBuffer(Shader.PropertyToID("_PositionMatrixBuffer"), positionBuffer);
        material.SetBuffer(Shader.PropertyToID("_ColorBuffer"), colorBuffer);
        rp = new RenderParams(material) {
            camera = cam,
            receiveShadows = false,
            worldBounds = new Bounds(new Vector3(0, 0, 0.5f), Vector3.one * 3f),
            shadowCastingMode = ShadowCastingMode.Off,
        };
    }

    void LoadBuffers() {
        Debug.Log("Load Buffers");
        // Load Command Buffer
        {
            GraphicsBuffer.IndirectDrawIndexedArgs[] args = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
            args[0].baseVertexIndex = mesh.GetBaseVertex(0);
            args[0].indexCountPerInstance = mesh.GetIndexCount(0);
            args[0].instanceCount = (uint)numItems;
            args[0].startIndex = mesh.GetIndexStart(0);
            args[0].startInstance = 0;
            argsBuffer.SetData(args);
        }

        // Load Positions & Colors
        {
            Matrix4x4[] positions = new Matrix4x4[numItems];
            Vector3[] colors = new Vector3[numItems];
            Vector3 pos = new Vector3(0f, 0f, 0.5f);
            for (int i = 0; i < numItems; i++) {
                pos.y = (((i / resolution) + 0.5f) / resolution) * 2f - 1f;
                pos.x = (((i % resolution) + 0.5f) / resolution) * 2f - 1f;
                // Debug.Log($"({pos.x}, {pos.y})");
                positions[i] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * scale);
                colors[i].x = colors[i].y = colors[i].z = i / (float)numItems;
            }
            positionBuffer.SetData(positions);
            colorBuffer.SetData(colors);
        }
    }

    void CreateBuffers() {
        Debug.Log("Create Buffers");
        argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        positionBuffer = new ComputeBuffer(numItems, sizeof(float) * 16);
        colorBuffer = new ComputeBuffer(numItems, sizeof(float) * 3);
    }
}
