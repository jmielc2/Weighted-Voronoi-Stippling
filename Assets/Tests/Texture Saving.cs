using UnityEngine;

namespace Tests {
    [RequireComponent(typeof(Camera))]
    public class TestTextureSaving : MonoBehaviour {
        Camera cam;
        RenderTexture rt;
        Texture2D texture;
        Material material;
        [SerializeField] Color color;
        // Create And Link All Components
        void Awake() {
            Debug.Log("Awake");
            cam = GetComponent<Camera>();
            material = new Material(Shader.Find("Unlit/Test Shader"));
            material.SetVector(Shader.PropertyToID("_Color"), color);
            transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material = material;
        }

        // Create Components That Depend on Controls
        void OnEnable() {
            Debug.Log("Enable");
            CreateRenderTexture();
            PrerenderTexture();
            DestroyRenderTexture();
        }

        // Final changes before first frame is rendered
        void Start() {
            Debug.Log("Start");
            Destroy(transform.GetChild(0).gameObject);
        }

        void OnValidate() {
            Debug.Log("Validate");
        }

        void OnRenderImage(RenderTexture src, RenderTexture dest) {
            Graphics.Blit(texture, dest);
        }

        void PrerenderTexture() {
            cam.targetTexture = rt;
            RenderTexture.active = rt;
            cam.Render();
            texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false) {
                filterMode = FilterMode.Point,
            };
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture.Apply();
            Color[] colors = texture.GetPixels();
            Debug.Log($"Raw => ({colors[0].r}, {colors[0].g}, {colors[0].b})");
            // System.IO.File.WriteAllBytes("./Documents/prerendered.png", texture.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
        }

        void CreateRenderTexture() {
            var rtDescriptor = new RenderTextureDescriptor(cam.pixelWidth, cam.pixelHeight, RenderTextureFormat.ARGBFloat) {
                depthBufferBits = 32,
                useMipMap = false,
            };
            rt = new RenderTexture(rtDescriptor) {
                filterMode = FilterMode.Point
            };
            rt.Create();
        }

        void DestroyRenderTexture() {
            rt?.Release();
            rt = null;
        }
    }
}
