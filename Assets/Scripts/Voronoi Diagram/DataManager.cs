using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Random = UnityEngine.Random;

public class DataManager {
    readonly Vector3[] _colors;
    Vector3[] _points;
    Vector3[] _centroids;
    readonly Matrix4x4[] _pointMatrices;
    readonly Matrix4x4[] _centroidMatrices;
    readonly Matrix4x4[] _coneMatrices;
    readonly int numPoints;

    NativeArray<Color> colors;
    NativeArray<Vector2> centroids;
    NativeArray<int> counts;
    bool printed = false;
 
    static Mesh _pointMesh;
    static Mesh _coneMesh;
    const float pointScale = 0.018f;
    Quaternion pointRotation = Quaternion.Euler(0f, 0f, 45f);

    [BurstCompile(CompileSynchronously = true, FloatPrecision = FloatPrecision.Standard, FloatMode = FloatMode.Fast)]
    protected struct CalcCentroidsJob : IJobFor {
        [ReadOnly]
        public NativeArray<Color> colors;
        [ReadOnly]
        public int numPoints, width, height;
        [ReadOnly]
        public float aspect;

        public NativeArray<int> counts;
        public NativeArray<Vector2> centroids;

        public void Execute(int row) {
            if (row >= height) {
                return;
            }
            int bIndex = row * width;
            float id = colors[bIndex].b;
            int start = 0;
            for (int i = 1; i < width; i++) {
                if (colors[bIndex + i].b == id) {
                    continue;
                }
                float middle = (start + i) * 0.5f;
                int index = Mathf.FloorToInt(id * numPoints);
                counts[index] += 1;
                centroids[index] += new Vector2(
                    (float)middle,
                    (float)row
                    // (2f * aspect / width) * middle - aspect,
                    // (2f / height) * row - 1f
                );
                start = i;
                id = colors[bIndex + i].b;
            }
        }
    };

    public DataManager(int numRegions, Camera cam) {
        Debug.Log("Generating data");
        numPoints = numRegions;
        _colors = new Vector3[numPoints];
        colors = new NativeArray<Color>(cam.pixelWidth * cam.pixelHeight, Allocator.Persistent);
        centroids = new NativeArray<Vector2>(numPoints, Allocator.Persistent);
        counts = new NativeArray<int>(numPoints, Allocator.Persistent);
        _points = new Vector3[numPoints];
        _centroids = new Vector3[numPoints];
        _pointMatrices = new Matrix4x4[numPoints];
        _coneMatrices = new Matrix4x4[numPoints];
        AssignColors();
        GenerateRandomPoints(cam);
        _centroidMatrices = new Matrix4x4[numPoints];
    }

    ~DataManager() {
        colors.Dispose();
        centroids.Dispose();
        counts.Dispose();
    }

    public Vector3[] Points {
        get { return _points; }
    }

    public int NumPoints {
        get { return numPoints; }
    }

    public Vector3[] Colors {
        get { return _colors; }
    }

    public Matrix4x4[] PointMatrices {
        get { return _pointMatrices; }
    }

    public Matrix4x4[] CentroidMatrices {
        get { return _centroidMatrices; }
    }

    public Matrix4x4[] ConeMatrices {
        get { return _coneMatrices; }
    }

    public Mesh PointMesh {
        get { return _pointMesh; }
    }

    public Mesh ConeMesh {
        get { return _coneMesh; }
    }

    private void GenerateRandomPoints(Camera cam) {
        for(int i = 0; i < numPoints; i++) {
            // Calculate Cone Matrix
            _points[i].x = Random.Range(-1f, 1f) * cam.aspect;
            _points[i].y = Random.Range(-1f, 1f);
            _points[i].z = 0f;
            _coneMatrices[i] = Matrix4x4.TRS(_points[i], pointRotation, Vector3.one);
            _pointMatrices[i] = Matrix4x4.TRS(_points[i], pointRotation, Vector3.one * pointScale);
        }
    }

    private void AssignColors() {
        for(int i = 0; i < numPoints; i++) {
            _colors[i].x = Random.Range(0f, 1f);
            _colors[i].y = Random.Range(0f, 1f);
            _colors[i].z = i / (float)numPoints;
        }
    }

    public void UpdateCentroids(Texture2D voronoi, Camera cam) {
        colors.CopyFrom(voronoi.GetPixels());
        for (int i = 0; i < numPoints; i++) {
            counts[i] = 0;
            centroids[i] = Vector2.zero;
        }

        new CalcCentroidsJob{
            colors = colors,
            counts = counts,
            centroids = centroids,
            numPoints = numPoints,
            width = voronoi.width,
            height = voronoi.height,
            aspect = cam.aspect,
        }.Schedule(voronoi.height, default).Complete();

        for (int i = 0; i < numPoints; i++) {
            if (counts[i] != 0) {
                centroids[i] /= counts[i];
            } else {
                centroids[i] = new Vector2(_points[i].x, _points[i].y);
            }
            if (!printed) {
                Debug.Log(centroids[i]);
            }
            _centroids[i].x = centroids[i].x;
            _centroids[i].y = centroids[i].y;
            _centroidMatrices[i] = Matrix4x4.TRS(_centroids[i], Quaternion.identity, Vector3.one * pointScale);
        }
        printed = true;
    }

    public void MovePoints(float amount = 1f) {
        for (int i = 0; i< numPoints; i++) {
            _points[i] = Vector3.MoveTowards(_points[i], _centroids[i], amount);
            _coneMatrices[i] = Matrix4x4.TRS(_points[i], pointRotation, Vector3.one);
            _pointMatrices[i] = Matrix4x4.TRS(_points[i], pointRotation, Vector3.one * pointScale);
        }
    }

    public static void CreatePointMesh() {
        _pointMesh = new Mesh {
            subMeshCount = 1,
            vertices = new Vector3[] {
                new (-0.5f, -0.5f, 0f),
                new (-0.5f, 0.5f, 0f),
                new (0.5f, -0.5f, 0f),
                new (0.5f, 0.5f, 0f)
            }
        };
        _pointMesh.SetTriangles(new int[] {
            0, 1, 2,
            2, 1, 3
        }, 0, true);
    }

    public static void CreateConeMesh(Camera cam) {
        _coneMesh = new Mesh {
            subMeshCount = 1
        };
        // Calculate Minimum Number of Cone Slices
        float radius = Mathf.Sqrt(cam.pixelWidth * cam.pixelWidth + cam.pixelHeight * cam.pixelHeight);
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
        _coneMesh.SetVertices(vertices);
        _coneMesh.SetTriangles(triangles, 0, true, 0);
    }
}
