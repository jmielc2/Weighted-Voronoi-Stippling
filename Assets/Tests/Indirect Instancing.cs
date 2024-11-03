using UnityEngine;
using UnityEngine.Rendering;

namespace Tests {
    [RequireComponent(typeof(Camera))]
    public class TestIndirectInstancing : MonoBehaviour {
        [SerializeField] Material material;
        [SerializeField, Range(10, 500)] int resolution;

        Mesh mesh;
        Camera cam;
        RenderParams rp;
        GraphicsBuffer argsBuffer;
        ComputeBuffer positionBuffer;
        ComputeBuffer colorBuffer;
        int numItems;
        float scale;
        Bounds renderBounds;
        RenderTexture rt;
        Texture2D texture;
        bool validating = false;

        void OnValidate() {
            Debug.Log("Validate");
            validating = true;
            if (argsBuffer != null) {
                OnDisable();
                OnEnable();
            }
            validating = false;
        }

        void Awake() {
            Debug.Log("Awake");
            cam = GetComponent<Camera>();
            renderBounds = new Bounds(Vector3.zero, Vector3.one * 2f);
            CreateMesh();
        }

        void OnEnable() {
            Debug.Log("Enable");
            numItems = resolution * resolution;
            scale = 1f / resolution;
            CreateBuffers();
            LoadBuffers();
            ConfigureRenderPass();
            if (!validating) {
                CreateRenderTexture();
                PrerenderTexture();
                DestroyRenderTexture();
            } else {
                texture = null;
            }
        }

        void OnDisable() {
            Debug.Log("Disable");
            argsBuffer?.Release();
            argsBuffer = null;
            positionBuffer?.Release();
            positionBuffer = null;
            colorBuffer?.Release();
            colorBuffer = null;
            DestroyRenderTexture();
        }

        void Update() {
            if (texture == null) {
                CreateRenderTexture();
                PrerenderTexture();
                DestroyRenderTexture();
            }

            // Debug.Log("Update");
            if (Input.GetKeyDown(KeyCode.S) && texture != null) {
                Debug.Log("Writing texture to file.");
                System.IO.File.WriteAllBytes("./Documents/prerendered.png", texture.EncodeToPNG());
            }
        }

        void OnRenderImage(RenderTexture src, RenderTexture dest) {
            Graphics.Blit(texture, dest);
        }

        void PrerenderTexture() {
            Debug.Log("Prerender Texture");
            cam.targetTexture = rt;
            RenderTexture.active = rt;
            Graphics.RenderMeshIndirect(rp, mesh, argsBuffer);
            cam.Render();
            texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false) {
                filterMode = FilterMode.Point,
            };
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
        }

        void CreateMesh() {
            Debug.Log("Create Mesh");
            mesh = new Mesh {
                subMeshCount = 1,
                vertices = new Vector3[] {
                    new(-0.5f, -0.5f, 0f),
                    new (-0.5f, 0.5f, 0f),
                    new (0.5f, -0.5f, 0f),
                    new (0.5f, 0.5f, 0f),
                },
                bounds = renderBounds,
            };
            mesh.SetTriangles(new int[] {
                    0, 1, 2,
                    2, 1, 3
            }, 0, false);
        }

        void ConfigureRenderPass() {
            Debug.Log("Configure Renderer");
            material.SetBuffer(Shader.PropertyToID("_PositionMatrixBuffer"), positionBuffer);
            material.SetBuffer(Shader.PropertyToID("_ColorBuffer"), colorBuffer);
            rp = new RenderParams(material) {
                camera = cam,
                receiveShadows = false,
                worldBounds = renderBounds,
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
                Vector3 pos = Vector3.zero;
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

        void CreateRenderTexture() {
            Debug.Log("Create Render Texture");
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
}
