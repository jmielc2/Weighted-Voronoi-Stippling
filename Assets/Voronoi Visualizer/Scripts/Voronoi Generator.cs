using UnityEngine;
using UnityEngine.Rendering;

namespace VoronoiVisualizer {
    [RequireComponent(typeof(Camera))]
    public class VoronoiGenerator : MonoBehaviour {
        [SerializeField]
        protected Material material;
        [SerializeField, Range(1, 20000)]
        protected int numRegions = 100;

        // Private Member Variables
        protected Camera cam;
        protected DataManager data;
        protected RenderParams rp;
        protected GraphicsBuffer argsBuffer;
        protected ComputeBuffer positionBuffer, colorBuffer;
        protected Bounds renderBounds;
        protected RenderTexture rt;
        protected bool validating = false;
        protected bool canPlay = true;

        protected readonly static int positionBufferId = Shader.PropertyToID("_PositionMatrixBuffer"),
                            colorBufferId = Shader.PropertyToID("_ColorBuffer");
                            
        public RenderTexture renderTexture {
            get => rt;
        }

        protected void OnValidate() {
            Debug.Log("Validating");
            validating = true;
            if (argsBuffer != null) {
                OnDisable();
                OnEnable();
            }
            validating = false;
        }

        protected virtual void Awake() {
            Debug.Log("Awake");
            canPlay = RequirementCheck();
            if (!canPlay) {
                Debug.Log("Requirements not met.");
            }
            cam = GetComponent<Camera>();
            renderBounds = new Bounds(Vector3.zero, Vector3.one * 3f);
        }

        protected virtual void OnEnable() {
            Debug.Log("Enabling");
            if (data == null || data.NumPoints != numRegions) {
                data = new DataManager(numRegions, cam);
            }
            CreateBuffers();
            LoadBuffers();
            ConfigureRenderPass();
            RenderToTexture();
        }

        protected virtual void OnDisable() {
            Debug.Log("Disabling");
            argsBuffer?.Release();
            argsBuffer = null;
            positionBuffer?.Release();
            positionBuffer = null;
            colorBuffer?.Release();
            colorBuffer = null;
            DestroyRenderTexture();
        }

        protected virtual void Update() {
            if (!canPlay) {
                return;
            }
            if (rt == null) {
                RenderToTexture();
            }
            
            if (Input.GetKeyDown(KeyCode.S)) {
                Debug.Log("Writing texture to file.");
                Texture2D texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false) {
                    filterMode = FilterMode.Point,
                };
                texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                System.IO.File.WriteAllBytes("./Documents/voronoi-diagram.png", texture.EncodeToPNG());
            }
        }

        protected virtual void RenderToTexture() {
            if (validating) {
                return;
            }
            CreateRenderTexture();
            Debug.Log("Rendering to texture");
            cam.targetTexture = rt;
            RenderTexture.active = rt;
            rp.material = material;
            Graphics.RenderMeshIndirect(rp, data.ConeMesh, argsBuffer);
            cam.Render();
        }

        protected virtual void ConfigureRenderPass() {
            Debug.Log("Configuring renderer.");
            // Voronoi Material
            material.SetBuffer(positionBufferId, positionBuffer);
            material.SetBuffer(colorBufferId, colorBuffer);

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
            positionBuffer = new ComputeBuffer(numRegions, sizeof(float) * 16);
            colorBuffer = new ComputeBuffer(numRegions, sizeof(float) * 3);
        }

        protected virtual void LoadBuffers() {
            Debug.Log("Loading buffers.");
            // Load Command Buffer
            LoadArgBuffer(argsBuffer, data.ConeMesh);
            
            // Load Positions & Colors
            positionBuffer.SetData(data.ConeMatrices);
            colorBuffer.SetData(data.Colors);
        }

        protected virtual void LoadArgBuffer(GraphicsBuffer buffer, Mesh mesh) {
            GraphicsBuffer.IndirectDrawIndexedArgs[] args = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
            args[0].baseVertexIndex = mesh.GetBaseVertex(0);
            args[0].indexCountPerInstance = mesh.GetIndexCount(0);
            args[0].instanceCount = (uint)numRegions;
            args[0].startIndex = mesh.GetIndexStart(0);
            args[0].startInstance = 0;
            buffer.SetData(args);
            buffer.SetData(args);
        }

        protected void CreateRenderTexture() {
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

        protected bool RequirementCheck() {
            return SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat);
        }
    }
}
