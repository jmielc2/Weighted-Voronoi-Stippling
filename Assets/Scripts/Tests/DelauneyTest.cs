using System.Collections.Generic;
using UnityEngine;
using VoronatorSharp;

[RequireComponent(typeof(Camera))]
public class DelauneyTest : MonoBehaviour {
    Vector2[] generators;
    Vector3[] voronois;
    Vector3[] centroids;
    Color[] pixels;
    Camera cam;
    float width, height;
    Voronator voronator;

    [SerializeField]
    Texture2D stippleImage;
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
        pixels = stippleImage.GetPixels();
        float imageAspect = stippleImage.width / (float)stippleImage.height;
        if (imageAspect > cam.aspect) {
            width = cam.aspect;
            height = cam.aspect * (1f / imageAspect);
        } else {
            width = imageAspect;
            height = 1f;
        }
        width += 0.01f;
        height += 0.01f;
        if (generators == null || generators.Length != numGenerators) {
            generators = new Vector2[numGenerators];
            centroids = new Vector3[numGenerators];
            for (int i = 0; i < numGenerators; i++) {
                generators[i] = new (
                    Random.Range(-width, width),
                    Random.Range(-height, height)
                );
            }
            
            CalcDelauney(generators, width, height);
        }
    }

    void CalcDelauney(Vector2[] generators, float width, float height) {
        // Create Voronoi
        voronator = new Voronator(
            generators,
            new Vector2(-width - 0.02f, -height - 0.02f),
            new Vector2(width + 0.02f, height + 0.02f)
        );

        // Get Voronoi Regions
        List<Vector3> voronoisList = new List<Vector3>();
        for (int i = 0; i < generators.Length; i++) {
            List<Vector2> region = voronator.GetClippedPolygon(i);
            if (region == null) {
                continue;
            }
            centroids[i] = CalcCentroid(region).ToVector3();
            // centroids[i] = CalcWeightedCentroid(region).ToVector3();
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
        Vector2 origin = region[0];
        float totalArea = 0f;
        float centroidX = 0f;
        float centroidY = 0f;
        Vector2 a = Vector2.zero;
        Vector2 b = Vector2.zero;
        // Calculate weighted average of centroids of subregions
        for (int i = 1; i < region.Count - 1; i++) {
            a.x = region[i].x - origin.x;
            a.y = region[i].y - origin.y;
            b.x = region[i + 1].x - origin.x;
            b.y = region[i + 1].y - origin.y;
            float area = Mathf.Abs(a.x * b.y - b.x * a.y);
            float x = a.x + b.x;
            float y = a.y + b.y;
            totalArea += area;
            centroidX += area * x;
            centroidY += area * y;
        }
        return new Vector2(
            (centroidX / (3f * totalArea)) + origin.x,
            (centroidY / (3f * totalArea)) + origin.y
        );
    }

    private Vector2 CalcWeightedCentroid(List<Vector2> region) {
        Vector2 origin = region[0];
        float totalArea = 0f;
        float centroidX = 0f;
        float centroidY = 0f;
        Vector2 a = Vector2.zero;
        Vector2 b = Vector2.zero;
        // Debug.Log("Reading:")
        // Calculate weighted average of centroids of subregions
        for (int i = 1; i < region.Count - 1; i++) {
            a.x = region[i].x - origin.x;
            a.y = region[i].y - origin.y;
            b.x = region[i + 1].x - origin.x;
            b.y = region[i + 1].y - origin.y;
            float area = Mathf.Abs(a.x * b.y - b.x * a.y) * 0.5f;
            float x = (a.x + b.x) / 3f;
            float y = (a.y + b.y) / 3f;
            int posX = (int)Mathf.Floor((origin.x + x + width) * 0.5f * (1f / width) * (stippleImage.width - 1));
            int posY = (int)Mathf.Floor((origin.y + y + height) * 0.5f * (1f / height) * (stippleImage.height - 1));
            // Debug.Log($"({posX}, {posY})");
            Color color = pixels[posY * stippleImage.width + posX];
            float weight = color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
            totalArea += area * (1f - weight);
            centroidX += area * x * (1f - weight);
            centroidY += area * y * (1f - weight);
        }
        return new Vector2(
            (centroidX / totalArea) + origin.x,
            (centroidY / totalArea) + origin.y
        );
    }

    void Update() {
        for (int i = 0; i < generators.Length; i++) {
            generators[i] = centroids[i].ToVector2();
        }
        CalcDelauney(generators, width, height);
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
            Gizmos.color = Color.black;
            foreach(Vector2 centroid in centroids) {
                Gizmos.DrawSphere(centroid, 0.05f);
            }
        }
    }
}
