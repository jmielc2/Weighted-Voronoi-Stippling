using UnityEngine;
using UnityEngine.Rendering;

namespace CentroidVisualizer {
    public class DataManager {
        readonly Vector2[] _colors;
        readonly Matrix4x4[] _coneMatrices;
        readonly VoronoiRegion[] _voronoiData;
        readonly int numPoints;

        static Mesh _coneMesh = null;

        public struct VoronoiRegion {
            Vector2 centerOfMass;
            float totalMass;
        }

        public DataManager(int numRegions, Camera cam) {
            if (_coneMesh == null) {
                CreateConeMesh(cam);
            }
            numPoints = numRegions;
            _colors = new Vector2[numPoints];
            _coneMatrices = new Matrix4x4[numPoints];
            _voronoiData = new VoronoiRegion[numPoints];
            AssignColors();
            GenerateRandomPoints(cam);
        }

        ~DataManager() {
            _coneMesh?.Clear();
        }

        public int NumPoints {
            get { return numPoints; }
        }

        public Vector2[] Colors {
            get { return _colors; }
        }

        public Matrix4x4[] ConeMatrices {
            get { return _coneMatrices; }
        }

        public VoronoiRegion[] VoronoiData {
            get { return _voronoiData; }
        }

        public Mesh ConeMesh {
            get { return _coneMesh; }
        }

        private void GenerateRandomPoints(Camera cam) {
            Vector3 point = Vector3.zero;
            for(int i = 0; i < numPoints; i++) {
                // Calculate Cone Matrix
                point.x = Random.Range(-1f, 1f) * cam.aspect;
                point.y = Random.Range(-1f, 1f);
                _coneMatrices[i] = Matrix4x4.TRS(point, Quaternion.identity, Vector3.one);
            }
        }

        private void AssignColors() {
            for(int i = 0; i < numPoints; i++) {
                _colors[i].x = Random.Range(0f, 1f);
                _colors[i].y = Random.Range(0f, 1f);
            }
        }

        private static void CreateConeMesh(Camera cam) {
            _coneMesh = new Mesh {
                subMeshCount = 1
            };
            _coneMesh.vertexBufferTarget |= GraphicsBuffer.Target.Structured;
            _coneMesh.indexBufferTarget |= GraphicsBuffer.Target.Structured;
            VertexAttributeDescriptor[] attributes = new[] {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3)
            } ;

            // Calculate Minimum Number of Cone Slices
            float radius = Mathf.Sqrt((cam.pixelWidth * cam.pixelWidth) + (cam.pixelHeight * cam.pixelHeight));
            float maxAngle = 2f * Mathf.Acos((radius - 1f) / radius);
            int numSlices = Mathf.CeilToInt((2f * Mathf.PI) / maxAngle);

            // Generate Mesh
            Vector3[] vertices = new Vector3[numSlices + 1];
            int[] triangles = new int[numSlices * 3];
            vertices[0] = Vector3.zero;
            float angle = 0f;
            float width = cam.aspect * 2f;
            radius = Mathf.Sqrt(width * width + 4);
            for (int i = 1; i < numSlices + 1; i++) {
                vertices[i] = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    1f
                );
                angle -= maxAngle;
            }
            for (int i = 0; i < numSlices; i++) {
                triangles[i * 3] = 0;
                triangles[(i * 3) + 1] = i;
                triangles[(i * 3) + 2] = i + 1;
            }
            triangles[((numSlices - 1) * 3) + 2] = 1;
            Debug.Log($"Cone Triangles: {triangles.Length}");
            Debug.Log($"Cone Vertices: {vertices.Length}");
            _coneMesh.vertices = vertices;
            _coneMesh.SetIndices(triangles, MeshTopology.Triangles, 0);
            _coneMesh.SetVertexBufferParams(vertices.Length, attributes);
            _coneMesh.SetIndexBufferParams(triangles.Length, IndexFormat.UInt32);
            
            _coneMesh.SetVertexBufferData(vertices, 0, 0, vertices.Length);
            _coneMesh.SetIndexBufferData(triangles, 0, 0, triangles.Length);

            _coneMesh.SetSubMesh(
                0, new SubMeshDescriptor(0, triangles.Length, MeshTopology.Triangles), MeshUpdateFlags.DontRecalculateBounds
            );
        }
    }
}

