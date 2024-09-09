using UnityEngine;

public class PointManager {
    Vector3[] colors;
    Vector3[] points;
    Matrix4x4[] pointMatrices;
    Mesh pointMesh;
    int numPoints;

    Material pointMaterial;
    RenderParams renderParams;
    GraphicsBuffer argsBuffer;
    
    readonly static int colorBufferId = Shader.PropertyToID("_ColorBuffer"),
                    positionsMatrixBufferId = Shader.PropertyToID("_PositionsMatricBuffer");
    const float pointScale = 0.025f;

    public PointManager(int numPoints, Color pointColor) {
        this.numPoints = numPoints;
        colors = new Vector3[numPoints];
        points = new Vector3[numPoints];
        pointMatrices = new Matrix4x4[numPoints];
        Shader materialShader = Shader.Find("Unlit/Point Shader");
        pointMaterial = new Material(materialShader);
        renderParams = new RenderParams(pointMaterial);
        pointMaterial.color = pointColor;
        // TODO: Send points position data to GPU for point instance rendering.
        InitializeBuffer();
    }

    ~PointManager() {
        Release();
    }

    public void Release() {
        if (argsBuffer != null ) {
            argsBuffer.Release();
            argsBuffer = null;
        }
    }

    public Vector3[] GetColors() {
        return colors;
    }

    public Matrix4x4[] GetMatrices() {
        return pointMatrices;
    }

    public void GeneratePoints() {
        Quaternion pointRotation = Quaternion.Euler(0f, 0f, 45f);
        for (int i = 0; i < numPoints; i++) {
            // Calculate Cone Matrix
            points[i].x = Random.Range(-1f, 1f) * (Screen.width / (float)Screen.height);
            points[i].y = Random.Range(-1f, 1f);
            points[i].z = 0f;
            pointMatrices[i] = Matrix4x4.TRS(points[i], pointRotation, Vector3.one * pointScale);
            // Assign Unique Color
            colors[i] = new Vector3(
                Random.Range(0f, 1f),
                Random.Range(0f, 1f),
                (float)i / (float)numPoints
            );
        }
    }

    public void UpdatePoints(Vector3[] newPoints) {
        points = newPoints;
        Quaternion pointRotation = Quaternion.Euler(0f, 0f, 45f);
        for (int i = 0; i < numPoints; i++) {
            pointMatrices[i] = Matrix4x4.TRS(points[i], pointRotation, Vector3.one * pointScale);
        }
    }
    
    public void DrawPoints() {
        Graphics.RenderMeshIndirect(renderParams, pointMesh, argsBuffer);
    }

    private void InitializeBuffer() {
        CreatePointMesh();
        GraphicsBuffer.IndirectDrawIndexedArgs[] args = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        args[0].indexCountPerInstance = pointMesh.GetIndexCount(0);
        args[0].instanceCount = (uint)numPoints;
        args[0].startIndex = pointMesh.GetIndexStart(0);
        args[0].baseVertexIndex = pointMesh.GetBaseVertex(0);
        args[0].startInstance = 0;
        argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        argsBuffer.SetData(args);

        
    }

    private void CreatePointMesh() {
        pointMesh = new Mesh {
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

        pointMesh.SetVertices(vertices);
        pointMesh.SetTriangles(triangles, 0, false, 0);
    }
}
