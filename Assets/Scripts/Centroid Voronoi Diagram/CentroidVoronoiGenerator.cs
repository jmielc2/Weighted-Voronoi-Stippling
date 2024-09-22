using UnityEngine;

public class CentroidVoronoiGenerator : VoronoiVisualizer {
    [SerializeField]
    protected bool showCentroids = false;
    [SerializeField]
    protected Color centroidColor = new(0.8f, 0.1f, 0.1f, 1f);

    bool rendered = false;
    Texture2D voronoi;
    Rect screenReadRegion;
    ComputeBuffer centroidPositionBuffer;
    MaterialPropertyBlock pointPb, centroidPb;

    protected override void OnEnable() {
        Debug.Log("Enabling");
        if (data == null || data.NumPoints != numRegions) {
            data = new DataManager(numRegions, cam);
        }
        CreateBuffers();
        LoadBuffers();
        ConfigureRenderPass();
        CreateTextures();
        if (!validating) {
            Render();
        }
    }

    protected override void OnDisable() {
        base.OnDisable();
        voronoi = null;
        rendered = false;
    }

    protected override void Update() {
        if (!rendered) {
            Render();
        }

        if (Input.GetKeyDown(KeyCode.S)) {
            Debug.Log("Writing texture to file.");
            Texture2D texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false) {
                filterMode = FilterMode.Point,
            };
            RenderTexture.active = rt;
            texture.ReadPixels(screenReadRegion, 0, 0);
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes("./Documents/centroid-voronoi-diagram.png", texture.EncodeToPNG());
        }
    }

    protected void Render() {
        PrerenderTexture();
        data.UpdatePoints(voronoi);
        centroidPositionBuffer.SetData(data.CentroidMatrices);
        RenderToTexture();
        rendered = true;
    }

    protected void PrerenderTexture() {
        Debug.Log("Prerendering texture");
        cam.targetTexture = rt;
        RenderTexture.active = rt;
        rp.material = material;
        Graphics.RenderMeshIndirect(rp, data.ConeMesh, argsBuffer);
        cam.Render();
        voronoi.ReadPixels(screenReadRegion, 0, 0);
        RenderTexture.active = null;
        cam.targetTexture = null;
    }

    protected override void RenderToTexture() {
        if (!showPoints && !showCentroids) {
            return;
        }
        Debug.Log("Rendering to texture");
        cam.targetTexture = rt;
        RenderTexture.active = rt;
        rp.material = pointMaterial;
        if (showPoints) {
            Graphics.RenderMeshIndirect(rp, data.PointMesh, pointArgsBuffer);
        }
        if (showCentroids) {
            Graphics.RenderMeshIndirect(rp, data.PointMesh, pointArgsBuffer);
        }
        cam.Render();
        RenderTexture.active = null;
        cam.targetTexture = null;
    }

    // TODO: Update ConfigureRenderPass for property blocks.
    protected override void ConfigureRenderPass() {
        base.ConfigureRenderPass();
        screenReadRegion = new Rect(0, 0, rt.width, rt.height);
    }

    protected void CreateTextures() {
        base.CreateRenderTexture();
        voronoi = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false) {
            filterMode = FilterMode.Point,
        };
    }

    // TODO: Update CreateBuffers for centroidPositionBuffer
    protected override void CreateBuffers() {
        base.CreateBuffers();
    }

    // Calculates the centroid positions for each voronoi section
    //void CalculateCentroids() {
    //    int[] counts = new int[numRegions];
    //    for (int y = 0; y < texture.height; y++) {
    //        for (int x = 0; x < texture.width; x++) {
    //            float colorB = texture.GetPixel(x, y).b + (0.5f / numRegions);
    //            int index = Mathf.FloorToInt(colorB * numRegions);
    //            centroids[index] += new Vector3(
    //                (x + 0.5f) / (float)texture.width,
    //                (y + 0.5f) / (float)texture.height
    //            );
    //            counts[index]++;
    //        }
    //    }
    //    for (int i = 0; i < numRegions; i++) {
    //        Vector3 centroid = Vector3.zero;
    //        if (counts[i] == 0) {
    //            Debug.Log($"Index {i} has count of 0.");
    //        } else {
    //            float scalar = 2f / counts[i];
    //            centroid.x = (centroids[i].x * scalar - 1f) * cam.aspect;
    //            centroid.y = centroids[i].y * scalar - 1f;
    //            centroidPositions[i] = centroid;
    //            centroid.z = centroidScale * 0.5f;
    //            centroidMatrices[i] = Matrix4x4.TRS(centroid, Quaternion.identity, Vector3.one * centroidScale);
    //        }
    //    }
    //    Debug.Log("Centroids generated.");
    //    UpdatePoints(centroidPositions);
    //}
}
