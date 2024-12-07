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
        protected GraphicsBuffer positionBuffer, colorBuffer, voronoiData;
        protected Bounds renderBounds;
        protected RenderTexture voronoiTexture = null;
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

        public RenderTexture RenderTexture {
            get => voronoiTexture;
        }

        protected void Awake() {
            Debug.Log("Awake");
            cam = GetComponent<Camera>();
            data = new DataManager(numRegions, cam);
            renderBounds = new Bounds(Vector3.zero, Vector3.one * 5f);
            voronoiTexture = CreateRenderTexture();
            CreateBuffers();
            LoadBuffers();
            ConfigureRenderPass();
            canPlay = RequirementCheck();
            if (!canPlay) {
                Debug.Log("Requirements not met.");
            }
        }

        protected void OnDestroy() {
            Debug.Log("Destroying");
            positionBuffer?.Release();
            positionBuffer = null;
            colorBuffer?.Release();
            colorBuffer = null;
            voronoiData?.Release();
            voronoiData = null;
            voronoiTexture.Release();
        }

        protected void Update() {
            if (!canPlay) {
                return;
            }
            RenderCentroid();
        }

        protected void RenderCentroid() {
            Graphics.RenderMeshPrimitives(rp, data.ConeMesh, 0, numRegions);
            cam.Render();

            // // Gather Voronoi Data
            int numGroupsX = Mathf.CeilToInt(voronoiTexture.width / 8f);
            int numGroupsY = Mathf.CeilToInt(voronoiTexture.height / 8f);
            centroidCalculator.Dispatch(
                centroidCalculator.FindKernel("GatherData"), numGroupsX, numGroupsY, 1
            );

            // Calculate Centroid
            int numGroups = Mathf.CeilToInt(numRegions / 64f);
            centroidCalculator.Dispatch(
                centroidCalculator.FindKernel("CalculateCentroid"), numGroups, 1, 1
            );
        }

        protected void ConfigureRenderPass() {
            Debug.Log("Configuring renderer.");
            // Voronoi Material
            material.SetBuffer(colorBufferId, colorBuffer);
            material.SetBuffer(positionBufferId, positionBuffer);

            cam.targetTexture = voronoiTexture;
            RenderTexture.active = voronoiTexture;
            rp = new RenderParams() {
                camera = cam,
                receiveShadows = false,
                worldBounds = renderBounds,
                shadowCastingMode = ShadowCastingMode.Off
            };
            rp.material = material;
        }

        protected void CreateBuffers() {
            Debug.Log("Creating buffers.");
            positionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, numRegions, sizeof(float) * 16);
            colorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured,numRegions, sizeof(float) * 3);
            voronoiData = new GraphicsBuffer(GraphicsBuffer.Target.Structured, numRegions, sizeof(int) * 3);
        }

        protected void LoadBuffers() {
            Debug.Log("Loading buffers.");
            // Load Positions & Colors
            positionBuffer.SetData(data.ConeMatrices);
            colorBuffer.SetData(data.Colors);
            voronoiData.SetData(data.VoronoiData);

            // Set shared compute shader data
            centroidCalculator.SetInt(numRegionsId, numRegions);
            centroidCalculator.SetInt(imageWidthId, cam.pixelWidth);
            centroidCalculator.SetInt(imageHeightId, cam.pixelHeight);
            centroidCalculator.SetFloat(widthId, cam.aspect * 2f);
            centroidCalculator.SetFloat(heightId, 2f);

            // Set compute shader data needed to gather voronoi data
            int kernelId = centroidCalculator.FindKernel("GatherData");
            centroidCalculator.SetTexture(kernelId, voronoiDiagramId, voronoiTexture);
            centroidCalculator.SetBuffer(kernelId, voronoiDataId, voronoiData);

            // Set compute shader data needed to calculate centroids
            kernelId = centroidCalculator.FindKernel("CalculateCentroid");
            centroidCalculator.SetBuffer(kernelId, voronoiDataId, voronoiData);
            centroidCalculator.SetBuffer(kernelId, positionBufferId, positionBuffer);
        }

        protected RenderTexture CreateRenderTexture() {
            RenderTextureDescriptor desc = new RenderTextureDescriptor(cam.pixelWidth, cam.pixelHeight, RenderTextureFormat.ARGBFloat) {
                depthBufferBits = 32,
                useMipMap = false
            };
            return CreateRenderTexture(desc);
        }

        protected RenderTexture CreateRenderTexture(RenderTextureDescriptor descriptor) {
            Debug.Log("Creating render texture.");
            
            RenderTexture texture = new(descriptor) {
                filterMode = FilterMode.Point
            };
            texture.Create();
            return texture;
        }

        protected bool RequirementCheck() {
            return SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat) && SystemInfo.supportsComputeShaders;
        }
    }
}

