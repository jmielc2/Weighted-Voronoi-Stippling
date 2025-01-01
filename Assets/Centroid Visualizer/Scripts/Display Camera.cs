using UnityEngine;

namespace CentroidVisualizer {
    [RequireComponent(typeof(Camera))]
    public class DisplayCamera : MonoBehaviour {
        private CentroidGenerator generator;
        private bool captureFrame = false;

        private void Start() {
            generator = transform.parent.GetComponent<CentroidGenerator>();
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.S)) {
               captureFrame = true;
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination) {
            generator.CalculateCentroid();
            RenderTexture rt = generator.renderTexture;
            Graphics.Blit(rt, destination);
            
            if (captureFrame) {
                Debug.Log("Writing texture to file.");
                RenderTexture.active = rt;
                Texture2D texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false) {
                    filterMode = FilterMode.Point,
                };
                texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                System.IO.File.WriteAllBytes("./Documents/centroid-diagram.png", texture.EncodeToPNG());
                captureFrame = false;
            }
        }
    }
}
