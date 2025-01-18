using UnityEngine;
using UnityEngine.Rendering;

namespace FastStippler {
    public class FastStippler : MonoBehaviour {
        [SerializeField]
        Texture2D sourceImage;
        [SerializeField]
        Material material, pointMaterial;
        [SerializeField, Range(1024, 10000)]
        int numRegions = 100;
        [SerializeField]
        ComputeShader centroidCalculator;

        // Private Member Variables
        Camera cam;
        DataManager data;
        GraphicsBuffer voronoiArgsBuffer, stippleArgsBuffer;
        ComputeBuffer positionBuffer, colorBuffer, scaleBuffer, weightedBuffer, unweightedBuffer;
        Bounds renderBounds;
        RenderTexture voronoi = null;
        RenderTexture stipple = null;
        bool canPlay = true;
        RenderParams voronoiRp, stippleRp;
        int numGroupsPerDispatch;
        int condenseKernelId, reduceKernelId;

        readonly static int positionBufferId = Shader.PropertyToID("_PositionMatrixBuffer"),
                            colorBufferId = Shader.PropertyToID("_ColorBuffer"),
                            voronoiDiagramId = Shader.PropertyToID("_VoronoiDiagram"),
                            sourceImageId = Shader.PropertyToID("_SourceImage"),
                            sourceImageWidthId = Shader.PropertyToID("_SourceImageWidth"),
                            sourceImageHeightId = Shader.PropertyToID("_SourceImageHeight"),
                            numRegionsId = Shader.PropertyToID("_NumRegions"),
                            imageWidthId = Shader.PropertyToID("_ImageWidth"),
                            imageHeightId = Shader.PropertyToID("_ImageHeight"),
                            widthId = Shader.PropertyToID("_Width"),
                            heightId = Shader.PropertyToID("_Height"),
                            weightedBufferId = Shader.PropertyToID("_WeightedBuffer"),
                            unweightedBufferId = Shader.PropertyToID("_UnweightedBuffer"),
                            scaleBufferId = Shader.PropertyToID("_Scale"),
                            numGroupsPerDispatchId = Shader.PropertyToID("_NumGroupsPerDispatch"),
                            strideId = Shader.PropertyToID("_Stride"),
                            baseId = Shader.PropertyToID("_Base"),
                            remainingId = Shader.PropertyToID("_Remaining");

        public RenderTexture Stipple {
            get => stipple;
        }

        void OnValidate() {
            if (voronoiArgsBuffer != null) {
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
                data = new DataManager(numRegions, sourceImage, cam.aspect);
            }
            voronoi = CreateRenderTexture(sourceImage.width, sourceImage.height);
            RenderTextureDescriptor descriptor = new (cam.pixelWidth, cam.pixelHeight, RenderTextureFormat.Default);
            stipple = CreateRenderTexture(descriptor);
            numGroupsPerDispatch = Mathf.CeilToInt(voronoi.width * voronoi.height / 64f);
            CreateBuffers();
            LoadBuffers();
            ConfigureRenderPass();
        }

        void OnDisable() {
            voronoiArgsBuffer?.Release();
            voronoiArgsBuffer = null;
            stippleArgsBuffer?.Release();
            stippleArgsBuffer = null;
            positionBuffer?.Release();
            positionBuffer = null;
            colorBuffer?.Release();
            colorBuffer = null;
            scaleBuffer?.Release();
            scaleBuffer = null;
            weightedBuffer?.Release();
            weightedBuffer = null;
            unweightedBuffer?.Release();
            unweightedBuffer = null;
            voronoi.Release();
            voronoi = null;
            stipple.Release();
            stipple = null;
        }

        void Update() {
            if (!canPlay) {
                return;
            }
            RenderCentroid();
        }

        void RenderCentroid() {
            // Create Voronoi Diagram
            RenderTexture.active = cam.targetTexture = voronoi;
            Graphics.RenderMeshIndirect(voronoiRp, data.ConeMesh, voronoiArgsBuffer);
            cam.Render();
            CalculateCentroid();

            RenderTexture.active = cam.targetTexture = stipple;
            Graphics.RenderMeshIndirect(stippleRp, data.PointMesh, stippleArgsBuffer);
            cam.Render();
        }

        void CalculateCentroid() {
            for (int baseOffset = 0; baseOffset < numRegions; baseOffset += 2048) {
                int regionCount = (baseOffset + 2048 > numRegions)? numRegions - baseOffset : 2048;
                
                // Condense
                centroidCalculator.SetInt(baseId, baseOffset);
                centroidCalculator.Dispatch(condenseKernelId, regionCount, voronoi.width / 8, voronoi.height / 8);

                // Reduce
                int remaining = numGroupsPerDispatch;
                for (int stride = 1; stride < numGroupsPerDispatch; stride *= 256) {
                    int numBatches = Mathf.CeilToInt(remaining / 256f);
                    centroidCalculator.SetInt(strideId, stride);
                    centroidCalculator.SetInt(remainingId, remaining);
                    centroidCalculator.Dispatch(reduceKernelId, regionCount, numBatches, 1);
                    remaining = Mathf.CeilToInt(remaining / 256f);
                }
            }
        }

        void ConfigureRenderPass() {
            // Voronoi Material
            material.SetBuffer(positionBufferId, positionBuffer);
            material.SetBuffer(colorBufferId, colorBuffer);

            voronoiRp = new RenderParams() {
                camera = cam,
                worldBounds = renderBounds,
                material = material,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false
            };

            // Point Material
            pointMaterial.SetBuffer(positionBufferId, positionBuffer);
            pointMaterial.SetBuffer(scaleBufferId, scaleBuffer);

            stippleRp = new RenderParams() {
                camera = cam,
                worldBounds = renderBounds,
                material = pointMaterial,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false
            };
        } 
        
        void CreateBuffers() {
            voronoiArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            stippleArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            positionBuffer = new ComputeBuffer(numRegions, sizeof(float) * 16);
            colorBuffer = new ComputeBuffer(numRegions, sizeof(float));
            scaleBuffer = new ComputeBuffer(numRegions, sizeof(float));
            weightedBuffer = new ComputeBuffer(2048 * numGroupsPerDispatch, sizeof(float) * 3);
            unweightedBuffer = new ComputeBuffer(2048 * numGroupsPerDispatch, sizeof(float) * 3);
        }

        void LoadBuffers() {
            // Load Command Buffer
            LoadArgBuffer(voronoiArgsBuffer, data.ConeMesh);
            LoadArgBuffer(stippleArgsBuffer, data.PointMesh);
            
            // Load Positions & Colors
            positionBuffer.SetData(data.ConeMatrices);
            colorBuffer.SetData(data.Colors);

            // Set shared compute shader data
            float aspect = voronoi.width / voronoi.height;
            centroidCalculator.SetInt(numRegionsId, numRegions);
            centroidCalculator.SetInt(imageWidthId, voronoi.width);
            centroidCalculator.SetInt(imageHeightId, voronoi.height);
            centroidCalculator.SetInt(sourceImageWidthId, sourceImage.width);
            centroidCalculator.SetInt(sourceImageHeightId, sourceImage.height);
            centroidCalculator.SetInt(numGroupsPerDispatchId, numGroupsPerDispatch);
            centroidCalculator.SetFloat(widthId, aspect * 2f);
            centroidCalculator.SetFloat(heightId, 2f);

            // Set compute shader data needed to gather voronoi data
            centroidCalculator.SetTexture(condenseKernelId, voronoiDiagramId, voronoi);
            centroidCalculator.SetTexture(condenseKernelId, sourceImageId, sourceImage);
            centroidCalculator.SetBuffer(condenseKernelId, weightedBufferId, weightedBuffer);
            centroidCalculator.SetBuffer(condenseKernelId, unweightedBufferId, unweightedBuffer);

            // Set compute shader data needed to calculate centroids
            centroidCalculator.SetBuffer(reduceKernelId, weightedBufferId, weightedBuffer);
            centroidCalculator.SetBuffer(reduceKernelId, unweightedBufferId, unweightedBuffer);
            centroidCalculator.SetBuffer(reduceKernelId, colorBufferId, colorBuffer);
            centroidCalculator.SetBuffer(reduceKernelId, scaleBufferId, scaleBuffer);
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
            RenderTexture texture = new (descriptor) {
                filterMode = FilterMode.Point
            };
            texture.Create();
            return texture;
        }

        RenderTexture CreateRenderTexture(int width, int height) {
            var descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.RFloat) {
                depthBufferBits = 32,
                useMipMap = false,
                enableRandomWrite = true
            };
            return CreateRenderTexture(descriptor);
        }

        bool RequirementCheck() {
            return SystemInfo.SupportsTextureFormat(TextureFormat.RFloat) && SystemInfo.supportsComputeShaders;
        }
    }
}

