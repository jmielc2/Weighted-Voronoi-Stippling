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
        protected RenderTexture rt = null;
        protected bool validating = false;
        protected bool canPlay = true;
        protected Mesh voronoiMesh;

        protected readonly static int positionBufferId = Shader.PropertyToID("_PositionMatrixBuffer"),
                            colorBufferId = Shader.PropertyToID("_ColorBuffer"),
                            voronoiDiagramId = Shader.PropertyToID("_VoronoiDiagram"),
                            voronoiDataId = Shader.PropertyToID("_VoronoiData"),
                            numRegionsId = Shader.PropertyToID("_NumRegions"),
                            imageWidthId = Shader.PropertyToID("_ImageWidth"),
                            imageHeightId = Shader.PropertyToID("_ImageHeight"),
                            widthId = Shader.PropertyToID("_Width"),
                            heightId = Shader.PropertyToID("_Height"),

                            coneMeshVertexBufferId = Shader.PropertyToID("_ConeMeshVertexBuffer"),
                            coneMeshIndexBufferId = Shader.PropertyToID("_ConeMeshIndexBuffer"),
                            voronoiMeshVertexBufferId = Shader.PropertyToID("_VoronoiMeshVertexBuffer"),
                            voronoiMeshIndexBufferId = Shader.PropertyToID("_VoronoiMeshIndexBuffer"),
                            numConeVerticesId = Shader.PropertyToID("_NumConeVertices"),
                            numConeIndicesId = Shader.PropertyToID("_NumConeIndices");

        public RenderTexture RenderTexture {
            get => rt;
        }

        protected void OnValidate() {
            Debug.Log("Validating");
            validating = true;
            if (positionBuffer != null) {
                OnDisable();
                OnEnable();
            }
            validating = false;
        }

        protected void Awake() {
            Debug.Log("Awake");
            canPlay = RequirementCheck();
            if (!canPlay) {
                Debug.Log("Requirements not met.");
            }
            cam = GetComponent<Camera>();
            renderBounds = new Bounds(Vector3.zero, Vector3.one * 5f);
        }

        protected void OnEnable() {
            Debug.Log("Enabling");
            if (data == null || data.NumPoints != numRegions) {
                data = new DataManager(numRegions, cam);
            }
            rt = CreateRenderTexture();
            CreateBuffers();
            LoadBuffers();
            ConfigureRenderPass();
        }

        protected void OnDisable() {
            Debug.Log("Disabling");
            data = null;
            positionBuffer?.Release();
            positionBuffer = null;
            colorBuffer?.Release();
            colorBuffer = null;
            voronoiData?.Release();
            voronoiData = null;
            voronoiMesh.Clear();
            DestroyRenderTexture();
        }

        protected void Update() {
            if (!canPlay) {
                return;
            }
            RenderCentroid();
        }

        protected void RenderCentroid() {
            if (validating) {
                return;
            }
            // Create Voronoi Mesh
            centroidCalculator.Dispatch(
                centroidCalculator.FindKernel("PopulateVoronoiMeshVertexData"), numRegions, data.ConeMesh.vertices.Length, 1
            );

            centroidCalculator.Dispatch(
                centroidCalculator.FindKernel("PopulateVoronoiMeshIndexData"), numRegions, data.ConeMesh.triangles.Length, 1
            );
            
            // Graphics.RenderMeshIndirect(rp, data.ConeMesh, argsBuffer);
            Graphics.RenderMesh(rp, voronoiMesh, 0, Matrix4x4.identity);
            cam.Render();

            // Gather Voronoi Data
            int numGroupsX = Mathf.CeilToInt(rt.width / 8f);
            int numGroupsY = Mathf.CeilToInt(rt.height / 8f);
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
            material.SetInt(numRegionsId, numRegions);

            rp = new RenderParams() {
                camera = cam,
                receiveShadows = false,
                worldBounds = renderBounds,
                shadowCastingMode = ShadowCastingMode.Off
            };
            cam.targetTexture = rt;
            RenderTexture.active = rt;
            rp.material = material;
        }

        protected void CreateBuffers() {
            Debug.Log("Creating buffers.");
            positionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, numRegions, sizeof(float) * 16);
            colorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured,numRegions, sizeof(float) * 2);
            voronoiData = new GraphicsBuffer(GraphicsBuffer.Target.Structured, numRegions, sizeof(int) * 3);
            voronoiMesh = CreateVoronoiMesh();
        }

        protected void LoadBuffers() {
            Debug.Log("Loading buffers.");
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
            centroidCalculator.SetInt(numConeVerticesId, data.ConeMesh.vertices.Length);
            centroidCalculator.SetInt(numConeIndicesId, data.ConeMesh.triangles.Length);

            // Set compute shader data needed to gather voronoi data
            int kernelId = centroidCalculator.FindKernel("GatherData");
            centroidCalculator.SetTexture(kernelId, voronoiDiagramId, rt);
            centroidCalculator.SetBuffer(kernelId, voronoiDataId, voronoiData);

            // Set compute shader data needed to calculate centroids
            kernelId = centroidCalculator.FindKernel("CalculateCentroid");
            centroidCalculator.SetBuffer(kernelId, voronoiDataId, voronoiData);
            centroidCalculator.SetBuffer(kernelId, positionBufferId, positionBuffer);

            // Set compute shader data needed to calculate voronoi mesh
            kernelId = centroidCalculator.FindKernel("PopulateVoronoiMeshVertexData");
            centroidCalculator.SetBuffer(kernelId, coneMeshVertexBufferId, data.ConeMesh.GetVertexBuffer(0));
            centroidCalculator.SetBuffer(kernelId, voronoiMeshVertexBufferId, voronoiMesh.GetVertexBuffer(0));
            centroidCalculator.SetBuffer(kernelId, positionBufferId, positionBuffer);

            kernelId = centroidCalculator.FindKernel("PopulateVoronoiMeshIndexData");
            centroidCalculator.SetBuffer(kernelId, coneMeshIndexBufferId, data.ConeMesh.GetIndexBuffer());
            centroidCalculator.SetBuffer(kernelId, coneMeshVertexBufferId, data.ConeMesh.GetVertexBuffer(0));
            centroidCalculator.SetBuffer(kernelId, voronoiMeshIndexBufferId, voronoiMesh.GetIndexBuffer());
           
        }

        protected RenderTexture CreateRenderTexture(RenderTextureDescriptor? descriptor = null) {
            Debug.Log("Creating render texture.");
            RenderTextureDescriptor desc = (descriptor != null) ? (RenderTextureDescriptor) descriptor! : new RenderTextureDescriptor(cam.pixelWidth, cam.pixelHeight, RenderTextureFormat.ARGBFloat) {
                depthBufferBits = 32,
                useMipMap = false
            };
            RenderTexture texture = new(desc) {
                filterMode = FilterMode.Point
            };
            texture.Create();
            return texture;
        }

        protected void DestroyRenderTexture() {
            rt?.Release();
            rt = null;
        }

        protected bool RequirementCheck() {
            return SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat) && SystemInfo.supportsComputeShaders;
        }

        protected Mesh CreateVoronoiMesh() {
            // Creates and configures a mesh to be updated via Compute Shader then rendered in a single draw call.
            Mesh mesh = new() {
                subMeshCount = 1
            };
            VertexAttributeDescriptor[] attributes = new[] {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 4),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 1)
            };
            Debug.Log(data.ConeMesh.vertices.Length * numRegions);
            mesh.vertexBufferTarget |= GraphicsBuffer.Target.Structured;
            mesh.indexBufferTarget |= GraphicsBuffer.Target.Structured;
            int numIndices = data.ConeMesh.triangles.Length * numRegions;
            mesh.SetVertexBufferParams(data.ConeMesh.vertices.Length * numRegions, attributes);
            mesh.SetIndexBufferParams(numIndices, IndexFormat.UInt32);
            mesh.SetSubMesh(
                0, new SubMeshDescriptor(0, numIndices, MeshTopology.Triangles), MeshUpdateFlags.DontRecalculateBounds
            );

            return mesh;
        }
    }
}

