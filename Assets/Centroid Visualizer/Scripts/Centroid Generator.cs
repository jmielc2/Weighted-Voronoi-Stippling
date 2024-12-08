using UnityEngine;
using UnityEngine.Rendering;

namespace CentroidVisualizer {
    public class CentroidGenerator : MonoBehaviour {
        [SerializeField]
        protected Material material;
        [SerializeField, Range(1, 20000)]
        protected int numRegions = 100;
        [SerializeField]
        protected ComputeShader centroidCalculator;

        // Private Member Variables
        protected Camera cam;
        protected DataManager data;
        protected RenderParams rp;
        protected GraphicsBuffer argsBuffer;
        protected ComputeBuffer positionBuffer, colorBuffer, voronoiData;
        protected Bounds renderBounds;
        protected RenderTexture rt = null;
        protected int numGroups;
        protected bool validating = false;
        protected bool canPlay = true;

        protected readonly static int positionBufferId = Shader.PropertyToID("_PositionMatrixBuffer"),
                            colorBufferId = Shader.PropertyToID("_ColorBuffer"),
                            voronoiDiagramId = Shader.PropertyToID("_VoronoiDiagram"),
                            voronoiDataId = Shader.PropertyToID("_VoronoiData"),
                            numRegionsId = Shader.PropertyToID("_NumRegions"),
                            imageWidthId = Shader.PropertyToID("_ImageWidth"),
                            imageHeightId = Shader.PropertyToID("_ImageHeight"),
                            widthId = Shader.PropertyToID("_Width"),
                            heightId = Shader.PropertyToID("_Height");

        public RenderTexture renderTexture {
            get => rt;
        }

        protected void OnValidate() {
            validating = true;
            if (argsBuffer != null) {
                OnDisable();
                OnEnable();
            }
            validating = false;
        }

        protected virtual void Awake() {
            canPlay = RequirementCheck();
            if (!canPlay) {
                Debug.Log("Requirements not met.");
            }
            cam = GetComponent<Camera>();
            renderBounds = new Bounds(Vector3.zero, Vector3.one * 3f);
        }

        protected virtual void OnEnable() {
            if (data == null || data.NumPoints != numRegions) {
                data = new DataManager(numRegions, cam);
            }
            rt = CreateRenderTexture();
            CreateBuffers();
            LoadBuffers();
            ConfigureRenderPass();
        }

        protected virtual void OnDisable() {
            argsBuffer?.Release();
            argsBuffer = null;
            positionBuffer?.Release();
            positionBuffer = null;
            colorBuffer?.Release();
            colorBuffer = null;
            voronoiData?.Release();
            voronoiData = null;
            DestroyRenderTexture();
        }

        protected virtual void Update() {
            if (!canPlay) {
                return;
            }
            RenderCentroid();
        }

        protected virtual void RenderCentroid() {
            // Create Voronoi Diagram
            // Graphics.RenderMeshIndirect(rp, data.ConeMesh, argsBuffer);
            Graphics.DrawMeshInstancedIndirect(data.ConeMesh, 0, material, renderBounds, argsBuffer);
            cam.Render();

            // Gather Voronoi Data
            int numBatchesX = Mathf.CeilToInt(rt.width / 8f);
            int numBatchesY = Mathf.CeilToInt(rt.height / 8f);
            centroidCalculator.Dispatch(
                centroidCalculator.FindKernel("GatherData"), numBatchesX, numBatchesY, 1
            );

            // Calculate Centroid
            int numBatches = Mathf.CeilToInt(numRegions / 64f);
            centroidCalculator.Dispatch(
                centroidCalculator.FindKernel("CalculateCentroid"), numBatches, 1, 1
            );
        }

        protected virtual void ConfigureRenderPass() {
            // Voronoi Material
            material.SetBuffer(positionBufferId, positionBuffer);
            material.SetBuffer(colorBufferId, colorBuffer);

            cam.targetTexture = rt;
            RenderTexture.active = rt;
            rp = new RenderParams(material) {
                camera = cam,
                receiveShadows = false,
                worldBounds = renderBounds,
                shadowCastingMode = ShadowCastingMode.Off
            };
        }

        protected virtual void CreateBuffers() {
            argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            positionBuffer = new ComputeBuffer(numRegions, sizeof(float) * 16);
            colorBuffer = new ComputeBuffer(numRegions, sizeof(float) * 3);
            voronoiData = new ComputeBuffer(numRegions, sizeof(float) * 3);
        }

        protected virtual void LoadBuffers() {
            // Load Command Buffer
            LoadArgBuffer(argsBuffer, data.ConeMesh);
            
            // Load Positions & Colors
            positionBuffer.SetData(data.ConeMatrices);
            colorBuffer.SetData(data.Colors);
            voronoiData.SetData(data.VoronoiData);

            // Set shared compute shader data
            centroidCalculator.SetInt(numRegionsId, numRegions);
            centroidCalculator.SetInt(imageWidthId, rt.width);
            centroidCalculator.SetInt(imageHeightId, rt.height);
            centroidCalculator.SetFloat(widthId, cam.aspect * 2f);
            centroidCalculator.SetFloat(heightId, 2f);

            // Set compute shader data needed to gather voronoi data
            int kernelId = centroidCalculator.FindKernel("GatherData");
            centroidCalculator.SetTexture(kernelId, voronoiDiagramId, rt);
            centroidCalculator.SetBuffer(kernelId, voronoiDataId, voronoiData);

            // Set compute shader data needed to calculate centroids
            kernelId = centroidCalculator.FindKernel("CalculateCentroid");
            centroidCalculator.SetBuffer(kernelId, voronoiDataId, voronoiData);
            centroidCalculator.SetBuffer(kernelId, positionBufferId, positionBuffer);
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

        protected RenderTexture CreateRenderTexture(RenderTextureDescriptor descriptor) {
            RenderTexture texture = new RenderTexture(descriptor) {
                filterMode = FilterMode.Point
            };
            texture.Create();
            return texture;
        }

        protected RenderTexture CreateRenderTexture() {
            var descriptor = new RenderTextureDescriptor(cam.pixelWidth, cam.pixelHeight, RenderTextureFormat.ARGBFloat) {
                depthBufferBits = 32,
                useMipMap = false
            };
            return CreateRenderTexture(descriptor);
        }

        protected virtual void DestroyRenderTexture() {
            rt?.Release();
            rt = null;
        }

        protected bool RequirementCheck() {
            return SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat) && SystemInfo.supportsComputeShaders;
        }
    }
}

