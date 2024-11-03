using UnityEngine;

namespace VoronoiVisualizer {
    [RequireComponent(typeof(Camera))]
    public class DisplayCamera : MonoBehaviour {
        private VoronoiGenerator generator;

        private void Start() {
            generator = transform.parent.GetComponent<VoronoiGenerator>();
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination) {
            Graphics.Blit(generator.renderTexture, destination);
        }
    }
}
