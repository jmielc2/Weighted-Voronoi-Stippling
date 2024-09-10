using UnityEngine;

public class PointManager {
    Vector3[] _colors;
    Vector3[] _points;
    Matrix4x4[] _pointMatrices;
    int _numPoints;
    
    static Mesh _pointMesh = GeneratePointMesh();
    static float pointScale = 0.02f;
    static Quaternion pointRotation = Quaternion.Euler(0f, 0f, 45f);

    public PointManager(int numPoints) {
        _numPoints = numPoints;
        _colors = new Vector3[numPoints];
        _points = new Vector3[numPoints];
        _pointMatrices = new Matrix4x4[numPoints];
        AssignColors();
    }

    public int numPoints {
        get { return _numPoints; }
    }

    public Vector3[] points {
        get { return _points; }
    }

    public Vector3[] colors {
        get { return colors; }
    }

    public Matrix4x4[] pointMatrices {
        get { return _pointMatrices; }
    }

    public Mesh pointMesh {
        get { return _pointMesh; }
    }

    public void GenerateRandomPoints() {
        for(int i = 0; i < _numPoints; i++) {
            // Calculate Cone Matrix
            _points[i].x = Random.Range(-1f, 1f) * (Screen.width / (float)Screen.height);
            _points[i].y = Random.Range(-1f, 1f);
            _points[i].z = 0f;
            _pointMatrices[i] = Matrix4x4.TRS(_points[i], pointRotation, Vector3.one * pointScale);
        }
    }

    void AssignColors() {
        for(int i = 0; i < _numPoints; i++) {
            colors[i].x = Random.Range(0f, 1f);
            colors[i].y = Random.Range(0f, 1f);
            colors[i].z = i / (float)_numPoints;
        }
    }

    public void UpdatePoints(Vector3[] newPoints) {
        _points = newPoints;
        Quaternion pointRotation = Quaternion.Euler(0f, 0f, 45f);
        for(int i = 0; i < _numPoints; i++) {
            _pointMatrices[i] = Matrix4x4.TRS(_points[i], pointRotation, Vector3.one * pointScale);
        }
    }

    

    static Mesh GeneratePointMesh() {
        Mesh mesh = new Mesh {
            subMeshCount = 1
        };

        Vector3[] vertices = {
            new Vector3(-1f, -1f, 0f),
            new Vector3(1f, -1f, 0f),
            new Vector3(-1f, 1f, 0f),
            new Vector3(1f, 1f, 0f)
        };
        int[] triangles = {
            0, 1, 2,
            2, 1, 3
        };

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0, true, 0);
        return mesh;
    }
}
