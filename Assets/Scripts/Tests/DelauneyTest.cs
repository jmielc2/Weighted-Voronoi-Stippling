using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using DelaunatorSharp;

[RequireComponent(typeof(Camera))]
public class DelauneyTest : MonoBehaviour {
    Vector3[] points;
    Camera cam;
    Delaunator delaunator;
    int numPoints = 3;

    void Start() {
        cam = GetComponent<Camera>();
        points = new Vector3[numPoints];
        for (int i = 0; i < numPoints; i++) {
            points[i] = new Vector3(
                Random.Range(-cam.aspect, cam.aspect),
                Random.Range(-1f, 1f),
                1f
            );
        }
        delaunator = new Delaunator(ToPoints(points).ToArray());
        points = new Vector3[delaunator.Triangles.Length];
        for (int i = 0; i <= delaunator.Triangles.Length; i++) {
            IPoint point = delaunator.Points[delaunator.Triangles[i]];
            points[i] = new Vector3((float)point.X, (float)point.Y, 1f);
        }
    }

    private IEnumerable<IPoint> ToPoints(Vector3[] points) {
        foreach(Vector3 point in points) {
            yield return new Point(point.x, point.y);
        }
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawLineList(points);
    }
}
