using UnityEngine;

namespace CentroidVisualizer {
    [RequireComponent(typeof(Camera))]
    public class DisplayCamera : MonoBehaviour {
        private CentroidGenerator generator;

        private void Start() {
            generator = transform.parent.GetComponent<CentroidGenerator>();
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination) {
            Graphics.Blit(generator.renderTexture, destination);
        }
    }
}
