using UnityEngine;
using Unity.Collections;

[RequireComponent(typeof(Camera))]
public class VoronoiVisualizer : MonoBehaviour {
    [SerializeField]
    Color pointColor;
    [SerializeField, Range(1, 2000)]
    int numRegions;
    [SerializeField]
    bool showPoints = true;
    [SerializeField]
    VoronoiGenerator voronoiGenerator;

    // Private Member Variables
    Camera cam;
    bool saveImage = false;
    Rect screenRect;


    // Runs when element is enabled
    void OnEnable() {
        cam = GetComponent<Camera>();
        screenRect = new Rect(0, 0, Screen.width, Screen.height);
        voronoiGenerator.Initialize(numRegions, pointColor);
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.S)) {
            saveImage = true;
        }
        // Texture2D voronoi = voronoiGenerator.CreateVoronoiDiagram(Screen.width / 2, Screen.height / 2);

        if (showPoints) {
            voronoiGenerator.DrawPoints();
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (saveImage) {
            // voronoiTexture.ReadPixels(regionToReadFrom, 0, 0, false);
            // voronoiTexture.Apply();
            // System.IO.File.WriteAllBytes("./Documents/voronoi.png", voronoiTexture.EncodeToPNG());
            saveImage = false;
        }
        Graphics.Blit(source, destination);
    }
}
