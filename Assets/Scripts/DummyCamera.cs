using UnityEngine;

[RequireComponent (typeof(Camera))]
public class DummyCamera : MonoBehaviour {
    [SerializeField]
    VoronoiVisualizer visualizer;

    Camera cam;
    private void Awake() {
        cam = GetComponent<Camera>();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Graphics.Blit(visualizer.renderTexture, destination);
    }
}
