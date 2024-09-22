using UnityEngine;

// TODO: Update data manager to handle centroids as well.
public class DataManager {
    readonly Vector3[] _colors;
    Vector3[] _points;
    Vector3[] _centroids;
    readonly Matrix4x4[] _pointMatrices;
    readonly Matrix4x4[] _centroidMatrices;
    readonly Matrix4x4[] _coneMatrices;
    readonly int numPoints;
    
    static Mesh _pointMesh;
    static Mesh _coneMesh;
    const float pointScale = 0.02f;
    Quaternion pointRotation = Quaternion.Euler(0f, 0f, 45f);

    public DataManager(int numRegions, Camera cam) {
        Debug.Log("Generating data");
        numPoints = numRegions;
        _colors = new Vector3[numPoints];
        _points = new Vector3[numPoints];
        _centroids = (Vector3[])_points.Clone();
        _pointMatrices = new Matrix4x4[numPoints];
        _coneMatrices = new Matrix4x4[numPoints];
        AssignColors();
        GenerateRandomPoints(cam);
    }

    public Vector3[] Points {
        get { return _points; }
        private set {
            _points = value;
            // Update Matrices
        }
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

    public void UpdatePoints(Texture2D voronoi) {

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
