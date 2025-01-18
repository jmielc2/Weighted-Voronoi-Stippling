using UnityEngine;

namespace FastStippler {
    public class DataManager {
        readonly float[] _colors;
        readonly Matrix4x4[] _coneMatrices;
        readonly int numPoints;

        static Mesh _coneMesh = null;
        static Mesh _pointMesh = null;

        public DataManager(int numRegions, Texture2D image, float camAspect) {
            if (_coneMesh == null) {
                CreateConeMesh(image);
                CreatePointMesh();
            }
            numPoints = numRegions;
            _colors = new float[numPoints];
            _coneMatrices = new Matrix4x4[numPoints];
            AssignColors();
            GenerateRandomPoints(image, camAspect);
        }

        public int NumPoints {
            get { return numPoints; }
        }

        public float[] Colors {
            get { return _colors; }
        }

        public Matrix4x4[] ConeMatrices {
            get { return _coneMatrices; }
        }

        public Mesh ConeMesh {
            get { return _coneMesh; }
        }

        public Mesh PointMesh {
            get {  return _pointMesh; }
        }

        private void GenerateRandomPoints(Texture2D image, float camAspect) {
            Vector3 point = Vector3.zero;
            float aspect = image.width / image.height;
            float height, width;
            if (aspect < camAspect) {
                height = 1f;
                width = aspect;
            } else {
                height = (1f / aspect) * camAspect;
                width = camAspect;
            }
            for(int i = 0; i < numPoints; i++) {
                // Calculate Cone Matrix
                point.x = Random.Range(-1f, 1f) * width;
                point.y = Random.Range(-1f, 1f) * height;
                _coneMatrices[i] = Matrix4x4.TRS(point, Quaternion.identity, Vector3.one);
            }
        }

        private void AssignColors() {
            for(int i = 0; i < numPoints; i++) {
                _colors[i] = i / (float)numPoints;
            }
        }

        private static void CreateConeMesh(Texture2D image) {
            _coneMesh = new Mesh {
                subMeshCount = 1
            };
            // Calculate Minimum Number of Cone Slices
            float radius = Mathf.Sqrt(image.width * image.width + image.height * image.height);
            float maxAngle = 2f * Mathf.Acos((radius - 1f) / radius) * 2f;
            int numSlices = Mathf.CeilToInt((2f * Mathf.PI) / maxAngle);

            // Generate Mesh
            Vector3[] vertices = new Vector3[numSlices + 1];
            int[] triangles = new int[numSlices * 3];
            vertices[0] = Vector3.zero;
            float angle = 0f;
            float aspect = image.width / image.height;
            radius = Mathf.Sqrt(aspect * aspect + 1) * 2f * 0.1f;
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
            _coneMesh.SetVertices(vertices);
            _coneMesh.SetTriangles(triangles, 0, false, 0);
        }

        private static void CreatePointMesh() {
            _pointMesh = new Mesh {
                subMeshCount = 3
            };
            Vector3[] vertices = {
                new (0.1f, 0.1f, 0f),
                new (-0.1f, 0.1f, 0f),
                new (-0.1f, -0.1f, 0f),
                new (0.1f, -0.1f, 0f)
            };
            int[] triangles = {
                0, 1, 2,
                2, 3, 0
             };
            _pointMesh.SetVertices(vertices);
            _pointMesh.SetTriangles(triangles, 0, false, 0);
        }
    }
}

