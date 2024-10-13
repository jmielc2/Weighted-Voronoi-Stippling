using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(Camera))]
public class DelauneyTest : MonoBehaviour {
    Vector3[] points;
    List<Vector3> voronois;
    Vector3[] centroids;
    Camera cam;

    [SerializeField, Range(3, 20000)]
    int numGenerators = 100;
    [SerializeField]
    bool showDelauney = true;
    [SerializeField]
    bool showVoronoi = true, showCentroids = false;

    void Awake() {
        cam = GetComponent<Camera>();
    }

    void OnEnable() {
        
    }

    void CalcDelauney() {

    }

    void Update() {

    }

    void OnDrawGizmosSelected() {
    
    }
}
