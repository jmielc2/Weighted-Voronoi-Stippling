using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using DelaunatorSharp;
using System;

using Random = UnityEngine.Random;
using DelaunatorSharp.Unity.Extensions;

[RequireComponent(typeof(Camera))]
public class DelauneyTest : MonoBehaviour {
    Vector3[] points;
    List<Vector3> voronois;
    Vector3[] centroids;
    Camera cam;
    Delaunator delaunator;

    [SerializeField, Range(4, 20000)]
    int numGenerators = 100;
    [SerializeField]
    bool showDelauney = true;
    [SerializeField]
    bool showVoronoi = true, showCentroids = false;

    private bool IsWithinBounds(IPoint point) => Math.Abs(point.X) <= cam.aspect  && Math.Abs(point.Y) <= 1;

    private IPoint FindBoundaryPoint(IPoint start, IPoint end) {
        IPoint point = new Point();
        if (Math.Abs(end.X) > cam.aspect) {
            double x = (end.X > 0)? cam.aspect : -cam.aspect;
            double m = (end.Y - start.Y) / (end.X - start.X);
            point.Y = start.Y + m * (x - start.X);
            point.X = x;
        }
        if (Math.Abs(end.Y) > 1) {
            double y = (end.Y > 0)? 1 : -1;
            double m = (end.Y - start.Y) / (end.X - start.X);
            point.X = (y - start.Y) / m + start.X;
            point.Y = y;
        }
        if (point.X == point.Y && point.X == 0) {
            Debug.Log($"({start}), ({end}) => ({point})");
        }
        return point;
    }

    private List<Vector3> CorrectPoints(IPoint[] points) {
        List<Vector3> result = new ();
        IPoint prev = points.Last();
        bool calcBoundary = IsWithinBounds(prev);
        if (!calcBoundary) {
            prev = null;
        }
        for (int i = 0; i < points.Length; i++) {
            IPoint point = points[i];
            if (IsWithinBounds(point)) {
                if (prev != null) {
                    result.Add(prev.ToVector3(0.5f));
                    result.Add(point.ToVector3(0.5f));
                }
                prev = point;
                calcBoundary = true;
            } else {
                if (calcBoundary) {
                    result.Add(prev.ToVector3(0.5f));
                    result.Add(FindBoundaryPoint(prev, point).ToVector3(0.5f));
                }
                IPoint next = points[(i + 1 == points.Length)? 0 : i + 1];
                if (IsWithinBounds(next) && result.Count > 0) {
                    result.Add(result.Last());
                    result.Add(FindBoundaryPoint(next, point).ToVector3(0.5f));
                }
                calcBoundary = false;
            }
        }
        return result;
    }

    void Awake() {
        cam = GetComponent<Camera>();
    }

    void OnEnable() {
        // Generator Points
        Point[] generators = new Point[numGenerators];
        for (int i = 0; i < numGenerators; i++) {
            generators[i] = new Point(
                Random.Range(-cam.aspect, cam.aspect),
                Random.Range(-1f, 1f)
            );
        }
        CalcDelauney(generators);
    }

    void CalcDelauney(Point[] generators) {
        // Create Delaunay Triangulation
        delaunator = new Delaunator(generators.OfType<IPoint>().ToArray());
        int numTriangles = delaunator.Triangles.Length / 3;
        
        // Fill Points Array
        points = new Vector3[numTriangles * 6];
        for (int t = 0, i = 0; t < numTriangles; t++, i += 6) {
            IPoint point1 = delaunator.Points[delaunator.Triangles[t * 3]];
            IPoint point2 = delaunator.Points[delaunator.Triangles[t * 3 + 1]];
            IPoint point3 = delaunator.Points[delaunator.Triangles[(t == numTriangles)? 0 : t * 3 + 2]];
            points[i] = point1.ToVector3(0.5f);
            points[i + 1] = point2.ToVector3(0.5f);

            points[i + 2] = points[i + 1];
            points[i + 3] = point3.ToVector3(0.5f);
            
            points[i + 4] = points[i + 3];
            points[i + 5] = points[i];
        }

        // Fill Voronoi Array
        List<IVoronoiCell> vCells = delaunator.GetVoronoiCellsBasedOnCircumcenters().ToList();
        voronois = new List<Vector3>(vCells.Count);
        foreach (IVoronoiCell cell in vCells) {
            voronois.AddRange(CorrectPoints(cell.Points).AsEnumerable());
        }

        // Fill Centroids Array
        centroids = new Vector3[vCells.Count];
        for (int i = 0; i < vCells.Count; i++) {
            centroids[i] = Delaunator.GetCentroid(vCells[i].Points).ToVector3(0.5f);
        }
    }

    void Update() {
        Point[] generators = centroids.ToPointEnumeration().ToArray();
        CalcDelauney(generators);
    }

    void OnDrawGizmosSelected() {
        if (showDelauney && points != null) {
            Gizmos.color = Color.black;
            Gizmos.DrawLineList(points);
        }
        if (showVoronoi && voronois != null) {
            Gizmos.color = Color.blue;
            Gizmos.DrawLineList(voronois.ToArray());
        }
        if (showCentroids && centroids != null) {
            Gizmos.color = Color.green;
            foreach (Vector3 centroid in centroids) {
                Gizmos.DrawSphere(centroid, 0.005f);
            }
        }
    }
}
