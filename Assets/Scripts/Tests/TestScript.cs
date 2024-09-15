using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class TestScript : MonoBehaviour {
    Camera cam;
    [SerializeField] RenderTexture rt;
    [SerializeField] Texture2D texture;

    void Awake() {
        Debug.Log("Awake");
    }

    void OnEnable() {
        Debug.Log("Enable");
        cam = GetComponent<Camera>();
        CreateRenderTexture();
    }

    // Start is called before the first frame update
    void Start() {
        Debug.Log("Start");
        PrerenderTexture();
    }

    // Update is called once per frame
    void Update() {
        
    }

    void OnDisable() {
        rt.Release();    
    }

    void PrerenderTexture() {
        cam.targetTexture = rt;
        RenderTexture.active = rt;
        cam.Render();
        texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false, true) {
            filterMode = FilterMode.Point,
        };
        texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture.Apply();
        Color[] colors = texture.GetPixels();
        Debug.Log($"(0, 0) = {colors[0]}");
        Debug.Log($"sRGB = {texture.isDataSRGB}");
        // System.IO.File.WriteAllBytes("./Documents/prerendered.png", texture.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = null;
    }

    void CreateRenderTexture() {
        var rtDescriptor = new RenderTextureDescriptor(cam.pixelWidth, cam.pixelHeight, RenderTextureFormat.ARGBFloat) {
            // graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
            sRGB = false,
            depthBufferBits = 32,
            useMipMap = false,
        };
        rt = new RenderTexture(rtDescriptor) {
            filterMode = FilterMode.Point
        };
        rt.Create();
    }
}
