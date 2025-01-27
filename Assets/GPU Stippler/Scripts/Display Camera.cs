using UnityEngine;

namespace GPUStippler {
    [RequireComponent(typeof(Camera))]
    public class DisplayCamera : MonoBehaviour {
        GPUStippler stippler;
        bool captureFrame = false;

        private void Start() {
            stippler = transform.parent.GetComponent<GPUStippler>();
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.S)) {
               captureFrame = true;
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination) {
            Graphics.Blit(stippler.Stipple, destination);
            
            if (captureFrame) {
                Debug.Log("Writing texture to file.");
                RenderTexture.active = stippler.Stipple;
                var rt = stippler.Stipple;
                Texture2D texture = new (rt.width, rt.height, TextureFormat.RGBAFloat, false) {
                    filterMode = FilterMode.Point,
                };
                texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                System.IO.File.WriteAllBytes("./Documents/stippled-image.png", texture.EncodeToPNG());
                captureFrame = false;
            }
        }
    }
}
