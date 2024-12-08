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
        protected int numGroups;
        protected bool captureScreen = false;
        protected bool canPlay = true;

        protected const int numInstancesPerGroup = 1024;
        protected readonly static int positionBufferId = Shader.PropertyToID("_PositionMatrixBuffer"),
                            colorBufferId = Shader.PropertyToID("_ColorBuffer");
                            
        protected void OnValidate() {
            if (argsBuffer != null) {
                OnDisable();
                OnEnable();
            }
        }

        protected void Awake() {
            canPlay = RequirementCheck();
            if (!canPlay) {
                Debug.Log("Requirements not met.");
            }
            cam = GetComponent<Camera>();
            renderBounds = new Bounds(Vector3.zero, Vector3.one * 3f);
        }

        protected void OnEnable() {
            if (data == null || data.NumPoints != numRegions) {
                data = new DataManager(numRegions, cam);
            }
            numGroups = Mathf.CeilToInt(numRegions / (float)numInstancesPerGroup);
            CreateBuffers();
            LoadBuffers();
            ConfigureRenderPass();
        }

        protected void OnDisable() {
            argsBuffer?.Release();
            argsBuffer = null;
            positionBuffer?.Release();
            positionBuffer = null;
            colorBuffer?.Release();
            colorBuffer = null;
        }

        protected void Update() {
            if (!canPlay) {
                return;
            }
            Graphics.DrawMeshInstancedIndirect(data.ConeMesh, 0, material, renderBounds, argsBuffer);
        }

        protected void ConfigureRenderPass() {
            // Voronoi Material
            material.SetBuffer(positionBufferId, positionBuffer);
            material.SetBuffer(colorBufferId, colorBuffer);

            rp = new RenderParams(material) {
                camera = cam,
                receiveShadows = false,
                worldBounds = renderBounds,
                shadowCastingMode = ShadowCastingMode.Off
            };
        }

        protected void CreateBuffers() {
            argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            positionBuffer = new ComputeBuffer(numRegions, sizeof(float) * 16);
            colorBuffer = new ComputeBuffer(numRegions, sizeof(float) * 3);
        }

        protected void LoadBuffers() {
            // Load Command Buffer
            LoadArgBuffer(ref argsBuffer, data.ConeMesh);
            
            // Load Positions & Colors
            positionBuffer.SetData(data.ConeMatrices);
            colorBuffer.SetData(data.Colors);
        }

        protected void LoadArgBuffer(ref GraphicsBuffer buffer, Mesh mesh) {
            GraphicsBuffer.IndirectDrawIndexedArgs[] args = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
            args[0].baseVertexIndex = mesh.GetBaseVertex(0);
            args[0].indexCountPerInstance = mesh.GetIndexCount(0);
            args[0].instanceCount = (uint)numRegions;
            args[0].startIndex = mesh.GetIndexStart(0);
            args[0].startInstance = 0;
            buffer.SetData(args);
        }

        protected bool RequirementCheck() {
            return SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat);
        }
    }
}
