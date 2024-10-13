using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using VoronatorSharp;

[RequireComponent(typeof(Camera))]
public class DelauneyTest : MonoBehaviour {
    Vector2[] generators;
    Vector3[] voronois;
    Vector3[] centroids;
    Camera cam;
    Voronator voronator;

    [SerializeField, Range(1, 20000)]
    int numGenerators = 100;
    [SerializeField]
    bool showGenerators = false, showVoronoi = true, showCentroids = false;

    void Awake() {
        cam = GetComponent<Camera>();
    }

    void OnValidate() {
        OnEnable();
    }

    void OnEnable() {
        if (cam == null) {
            return;
        }
        generators = new Vector2[numGenerators];
        for (int i = 0; i < numGenerators; i++) {
            generators[i] = new (
                Random.Range(-cam.aspect, cam.aspect),
                Random.Range(-1f, 1f)
            );
        }
        CalcDelauney(generators);
    }

    void CalcDelauney(Vector2[] generators) {
        // Create Voronoi
        voronator = new Voronator(
            generators,
            new Vector2(-cam.aspect, -1f),
            new Vector2(cam.aspect, 1f)
        );

        // Get Voronoi Regions
        List<Vector3> voronoisList = new List<Vector3>();
        centroids = new Vector3[numGenerators];
        for (int i = 0; i < generators.Length; i++) {
            List<Vector2> region = voronator.GetClippedPolygon(i);
            if (region == null) {
                continue;
            }
            centroids[i] = CalcCentroid(region).ToVector3();
            for (int j = 0; j < region.Count; j++) {
                voronoisList.Add(region[j]);
                if (j == region.Count - 1) {
                    voronoisList.Add(region[0]);
                } else {
                    voronoisList.Add(region[j + 1]);
                }
            }
        }
        voronois = voronoisList.ToArray();
    }

    private Vector2 CalcCentroid(List<Vector2> region) {
        return Vector2.zero;
    }

    void Update() {

    }

    void OnDrawGizmosSelected() {
        if (showGenerators && generators != null) {
            Gizmos.color = Color.magenta;
            foreach(Vector2 generator in generators) {
                Gizmos.DrawSphere(generator.ToVector3(), 0.01f);
            }
        }

        if (showVoronoi && voronois != null) {
            Gizmos.color = Color.blue;
            Gizmos.DrawLineList(voronois);
        }

        if (showCentroids && centroids != null) {
            Gizmos.color = Color.green;
            foreach(Vector2 centroid in centroids) {
                Gizmos.DrawSphere(centroid, 0.01f);
            }
        }
    }
}
