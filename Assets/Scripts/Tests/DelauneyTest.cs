using System.Collections.Generic;
using UnityEngine;
using VoronatorSharp;

[RequireComponent(typeof(Camera))]
public class DelauneyTest : MonoBehaviour {
    Vector2[] generators;
    List<VoronoiRegion> voronoiRegions;
    Vector3[] centroids;
    Vector2 minBounds;
    Vector2 maxBounds;
    Color[] pixels;
    Camera cam;
    float width, height;
    Voronator voronator;

    [SerializeField]
    Texture2D stippleImage;
    [SerializeField, Range(1, 20000)]
    int numGenerators = 100;

    private struct VoronoiRegion {
        public Vector2 centerOfMass;
        public float totalMass;
    }

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
        minBounds = new Vector2(-width - 0.02f, -height - 0.02f);
        maxBounds = new Vector2(width + 0.02f, height + 0.02f);
        if (generators == null || generators.Length != numGenerators) {
            generators = new Vector2[numGenerators];
            centroids = new Vector3[numGenerators];
            voronoiRegions = new List<VoronoiRegion>(numGenerators);
            for (int i = 0; i < numGenerators; i++) {
                voronoiRegions.Add(new VoronoiRegion());
                centroids[i] = Vector2.zero;
                // generators[i] = new Vector2(
                //     Random.Range(-width, width),
                //     Random.Range(-height, height)
                // );
                int x = 0, y = 0;
                for (int j = 0; j < 10; j++) {
                    x = Random.Range(0, stippleImage.width);
                    y = Random.Range(0, stippleImage.height);
                    if (CalcPixelWeight(x, y) >= 0.001f) {
                        break;
                    } 
                }
                Vector2 result = PixelCoordToWorldCoord(x, y);
                generators[i] = result;
            }
            
            CalcDelauney();
        }
    }

    float CalcPixelWeight(int x, int y) {
        Color pixel = pixels[y * stippleImage.width + x];
        return 1f - ((pixel.r * 0.299f) + (pixel.g * 0.587f) + (pixel.b * 0.114f));
    }

    void CalcDelauney() {
        // Create Voronoi
        voronator = new Voronator(generators, minBounds, maxBounds);

        // Prepare Weighted Centroid Data
        for (int i = 0; i < numGenerators; i++) {
            voronoiRegions[i] = new VoronoiRegion();
        }
        int currVoronoi = 0;
        for (int x = 0; x < stippleImage.width; x++) {
            for (int y = 0; y < stippleImage.height; y++) {
                Vector2 worldCoords = PixelCoordToWorldCoord(x, y);
                currVoronoi = voronator.Find(worldCoords, currVoronoi);
                float weight = CalcPixelWeight(x, y);
                VoronoiRegion region = voronoiRegions[currVoronoi];
                region.centerOfMass.x += weight * worldCoords.x;
                region.centerOfMass.y += weight * worldCoords.y;
                region.totalMass += weight;
                voronoiRegions[currVoronoi] = region;
            }
        }

        // Calc Voronoi Weighted Centroids
        for (int i = 0; i < numGenerators; i++) {
            // Calculate Voronoi Weighted Centroid
            List<Vector2> regionPoints = voronator.GetClippedPolygon(i);
            if (regionPoints == null) {
                continue;
            }
            VoronoiRegion regionData = voronoiRegions[i];
            if (regionData.totalMass - 0.005f > 0f) {
                centroids[i].x = regionData.centerOfMass.x / regionData.totalMass;
                centroids[i].y = regionData.centerOfMass.y / regionData.totalMass;
            } else {
                centroids[i] = CalcCentroid(regionPoints).ToVector3();
            }
        }
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

    Vector2 PixelCoordToWorldCoord(int x, int y) {
        Vector2 worldCoord = Vector2.zero;
        worldCoord.x = (((x + 0.5f) / (stippleImage.width * 0.5f)) - 1f) * width;
        worldCoord.y = (((y + 0.5f) / (stippleImage.height * 0.5f)) - 1f) * height;
        return worldCoord;
    }

    void Update() {
        for (int i = 0; i < generators.Length; i++) {
            generators[i] = centroids[i].ToVector2();
        }
        CalcDelauney();
    }

    void OnDrawGizmosSelected() {
        if (centroids != null) {
            Gizmos.color = Color.black;
            foreach(Vector2 centroid in centroids) {
                Gizmos.DrawSphere(centroid, 0.008f);
            }
        }
    }
}
