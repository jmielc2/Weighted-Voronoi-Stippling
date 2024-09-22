using UnityEngine;

public class CentroidVoronoiGenerator : VoronoiVisualizer {
    [SerializeField]
    protected bool showCentroids = false;
    [SerializeField]
    protected Color centroidColor = new(0.8f, 0.1f, 0.1f, 1f);

    protected override void Update() {
        if (rt == null) {
            RenderToTexture();
        }

        if (Input.GetKeyDown(KeyCode.S)) {
            Debug.Log("Writing texture to file.");
            texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false) {
                filterMode = FilterMode.Point,
            };
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes("./Documents/centroid-voronoi-diagram.png", texture.EncodeToPNG());
            texture = null;
        }
    }

    // Calculates the centroid positions for each voronoi section
    // void CalculateCentroids() {
        // Vector3[] centroidPositions = new Vector3[numRegions];
        // int[] counts = new int[numRegions];
        // for (int y = 0; y < texture.height; y++) {
        //     for (int x = 0; x < texture.width; x++) {
        //         float colorB = texture.GetPixel(x, y).b + (0.5f / numRegions);
        //         int index = Mathf.FloorToInt(colorB * numRegions);
        //         centroidPositions[index] += new Vector3(
        //             (x + 0.5f) / (float)texture.width,
        //             (y + 0.5f) / (float)texture.height
        //         );
        //         counts[index]++;
        //     }
        // }
        // for (int i = 0; i < numRegions; i++) {
        //     Vector3 centroid = Vector3.zero;
        //     if (counts[i] == 0) {
        //         Debug.Log($"Index {i} has count of 0.");
        //     } else {
        //         float scalar = 2f / counts[i];
        //         centroid.x = (centroidPositions[i].x * scalar - 1f) * cam.aspect;
        //         centroid.y = centroidPositions[i].y * scalar - 1f;
        //         centroidPositions[i] = centroid;
        //         centroid.z = centroidScale * 0.5f;
        //         centroidMatrices[i] = Matrix4x4.TRS(centroid, Quaternion.identity, Vector3.one * centroidScale);
        //     }
        // }
        // Debug.Log("Centroids generated.");
        // UpdatePoints(centroidPositions);
    // }
}
