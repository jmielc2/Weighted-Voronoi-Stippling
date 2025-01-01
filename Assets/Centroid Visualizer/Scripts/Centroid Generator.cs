using UnityEngine;
using UnityEngine.Rendering;

namespace CentroidVisualizer {
    public class CentroidGenerator : MonoBehaviour {
        [SerializeField]
        Material material;
        [SerializeField, Range(1, 20000)]
        int numRegions = 100;
        [SerializeField]
        ComputeShader centroidCalculator;

        // Private Member Variables
        Camera cam;
        DataManager data;
        GraphicsBuffer argsBuffer;
        ComputeBuffer positionBuffer, colorBuffer, waveBuffer;
        Bounds renderBounds;
        RenderTexture rt = null;
        bool canPlay = true;
        RenderParams rp;
        int numWavesPerDispatch;

        readonly static int positionBufferId = Shader.PropertyToID("_PositionMatrixBuffer"),
                            colorBufferId = Shader.PropertyToID("_ColorBuffer"),
                            voronoiDiagramId = Shader.PropertyToID("_VoronoiDiagram"),
                            numRegionsId = Shader.PropertyToID("_NumRegions"),
                            imageWidthId = Shader.PropertyToID("_ImageWidth"),
                            imageHeightId = Shader.PropertyToID("_ImageHeight"),
                            widthId = Shader.PropertyToID("_Width"),
                            heightId = Shader.PropertyToID("_Height"),
                            waveBufferId = Shader.PropertyToID("_WaveBuffer"),
                            numWavesPerDispatchId = Shader.PropertyToID("_NumWavesPerDispatch"),
                            offsetId = Shader.PropertyToID("_Offset");

        int condenseKernelId, reduceKernelId;

        public RenderTexture renderTexture {
            get => rt;
        }

        void OnValidate() {
            if (argsBuffer != null) {
                OnDisable();
                OnEnable();
            }
        }

        void Awake() {
            canPlay = RequirementCheck();
            if (!canPlay) {
                Debug.Log("Requirements not met.");
            }
            cam = GetComponent<Camera>();
            condenseKernelId = centroidCalculator.FindKernel("Condense");
            reduceKernelId = centroidCalculator.FindKernel("Reduce");
            renderBounds = new Bounds(Vector3.zero, Vector3.one * 3f);
        }

        void OnEnable() {
            if (data == null || data.NumPoints != numRegions) {
                data = new DataManager(numRegions, cam);
            }
            rt = CreateRenderTexture();
            numWavesPerDispatch = Mathf.CeilToInt(rt.width * rt.height / 32f);
            CreateBuffers();
            LoadBuffers();
            ConfigureRenderPass();
        }

        void OnDisable() {
            argsBuffer?.Release();
            argsBuffer = null;
            positionBuffer?.Release();
            positionBuffer = null;
            colorBuffer?.Release();
            colorBuffer = null;
            waveBuffer?.Release();
            waveBuffer = null;
            rt?.Release();
            rt = null;
        }

        void Update() {
            if (!canPlay) {
                return;
            }
            RenderCentroid();
        }

        void RenderCentroid() {
            // Create Voronoi Diagram
            Graphics.RenderMeshIndirect(rp, data.ConeMesh, argsBuffer);
            cam.Render();
        }

        public void CalculateCentroid() {
            // Condense
            centroidCalculator.Dispatch(
                condenseKernelId, numRegions, numWavesPerDispatch, 1
            );

            // Reduce
            int offset = 1;
            int remaining = numWavesPerDispatch;
            while (offset < numWavesPerDispatch) {
                int numBatches = Mathf.CeilToInt(remaining / 64f);
                centroidCalculator.SetInt(offsetId, offset);
                centroidCalculator.Dispatch(
                    reduceKernelId, numRegions, numBatches, 1
                );
                remaining = Mathf.CeilToInt(remaining / 2f);
                offset *= 2;
            }
        }

        void ConfigureRenderPass() {
            // Voronoi Material
            material.SetBuffer(positionBufferId, positionBuffer);
            material.SetBuffer(colorBufferId, colorBuffer);
            cam.targetTexture = rt;
            rp = new RenderParams() {
                camera = cam,
                worldBounds = renderBounds,
                material = material,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false
            };
        } 
        
        void CreateBuffers() {
            argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            positionBuffer = new ComputeBuffer(numRegions, sizeof(float) * 16);
            colorBuffer = new ComputeBuffer(numRegions, sizeof(float) * 3);
            waveBuffer = new ComputeBuffer(numRegions * numWavesPerDispatch, sizeof(float) * 3);
        }

        void LoadBuffers() {
            // Load Command Buffer
            LoadArgBuffer(argsBuffer, data.ConeMesh);
            
            // Load Positions & Colors
            positionBuffer.SetData(data.ConeMatrices);
            colorBuffer.SetData(data.Colors);

            // Set shared compute shader data
            centroidCalculator.SetInt(numRegionsId, numRegions);
            centroidCalculator.SetInt(imageWidthId, rt.width);
            centroidCalculator.SetInt(imageHeightId, rt.height);
            centroidCalculator.SetInt(numWavesPerDispatchId, numWavesPerDispatch);
            centroidCalculator.SetFloat(widthId, cam.aspect * 2f);
            centroidCalculator.SetFloat(heightId, 2f);

            // Set compute shader data needed to gather voronoi data
            centroidCalculator.SetTexture(condenseKernelId, voronoiDiagramId, rt);
            centroidCalculator.SetBuffer(condenseKernelId, waveBufferId, waveBuffer);

            // Set compute shader data needed to calculate centroids
            centroidCalculator.SetBuffer(reduceKernelId, waveBufferId, waveBuffer);
            centroidCalculator.SetBuffer(reduceKernelId, colorBufferId, colorBuffer);
            centroidCalculator.SetBuffer(reduceKernelId, positionBufferId, positionBuffer);
        }

        void LoadArgBuffer(GraphicsBuffer buffer, Mesh mesh) {
            GraphicsBuffer.IndirectDrawIndexedArgs[] args = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
            args[0].baseVertexIndex = mesh.GetBaseVertex(0);
            args[0].indexCountPerInstance = mesh.GetIndexCount(0);
            args[0].instanceCount = (uint)numRegions;
            args[0].startIndex = mesh.GetIndexStart(0);
            args[0].startInstance = 0;
            buffer.SetData(args);
        }

        RenderTexture CreateRenderTexture(RenderTextureDescriptor descriptor) {
            RenderTexture texture = new RenderTexture(descriptor) {
                filterMode = FilterMode.Point
            };
            texture.Create();
            return texture;
        }

        RenderTexture CreateRenderTexture() {
            var descriptor = new RenderTextureDescriptor(cam.pixelWidth, cam.pixelHeight, RenderTextureFormat.ARGBFloat) {
                depthBufferBits = 32,
                useMipMap = false,
                enableRandomWrite = true
            };
            return CreateRenderTexture(descriptor);
        }

        bool RequirementCheck() {
            return SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat) && SystemInfo.supportsComputeShaders;
        }
    }
}

