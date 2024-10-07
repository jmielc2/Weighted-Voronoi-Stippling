using UnityEngine;
using UnityEngine.Rendering;

public class CentroidVoronoiGenerator : VoronoiVisualizer {
    [SerializeField]
    protected bool showCentroids = false;
    [SerializeField]
    protected Color centroidColor = new(0.8f, 0.1f, 0.1f, 1f);

    Texture2D voronoi;
    Rect screenReadRegion;
    ComputeBuffer centroidPositionBuffer;
    MaterialPropertyBlock pointPb, centroidPb;
    bool movePoints = false;

    protected override void OnEnable() {
        Debug.Log("Enabling");
        if (data == null || data.NumPoints != numRegions) {
            data = new DataManager(numRegions, cam);
        }
        CreateBuffers();
        CreateTextures();
        LoadBuffers();
        ConfigureRenderPass();
    }

    protected override void OnDisable() {
        base.OnDisable();
        centroidPositionBuffer?.Release();
        centroidPositionBuffer = null;
        voronoi = null;
    }
    
    // TODO: Camera isn't rendering so onPre and onPost render methds don't get called
    protected void OnPreRender() {
        Render();
    }

    protected void OnPostRender() {
        if (movePoints) {
            Debug.Log("Moving points");
            data.MovePoints();
            positionBuffer.SetData(data.ConeMatrices);
            pointPositionBuffer.SetData(data.PointMatrices);
            // Voronoi Material
            material.SetBuffer(positionBufferId, positionBuffer);

            // Point & Centroid Material
            pointPb.SetBuffer(positionBufferId, pointPositionBuffer);
            centroidPb.SetBuffer(positionBufferId, centroidPositionBuffer);
            movePoints = false;
        }
    }

    protected override void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            movePoints = true;
        }

        if (Input.GetKeyDown(KeyCode.S)) {
            Debug.Log("Writing texture to file.");
            Texture2D texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false) {
                filterMode = FilterMode.Point,
            };
            texture.ReadPixels(screenReadRegion, 0, 0, false);
            System.IO.File.WriteAllBytes("./Documents/centroid-voronoi-diagram.png", texture.EncodeToPNG());
        }
    }

    protected void Render() {
        PrerenderTexture();
        data.UpdateCentroids(voronoi, cam);
        centroidPositionBuffer.SetData(data.CentroidMatrices);
        RenderToTexture();
    }

    protected void PrerenderTexture() {
        Debug.Log("Prerendering texture");
        rp.material = material;
        rp.matProps = null;
        Graphics.RenderMeshIndirect(rp, data.ConeMesh, argsBuffer);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.Render();
        voronoi.ReadPixels(screenReadRegion, 0, 0, false);
    }

    protected override void RenderToTexture() {
        if (!showPoints && !showCentroids) {
            return;
        }
        Debug.Log("Rendering to texture");
        rp.material = pointMaterial;
        if (showPoints) {
            rp.matProps = pointPb;
            Graphics.RenderMeshIndirect(rp, data.PointMesh, pointArgsBuffer);
        }
        if (showCentroids) {
            rp.matProps = centroidPb;
            Graphics.RenderMeshIndirect(rp, data.PointMesh, pointArgsBuffer);
        }
        cam.clearFlags = CameraClearFlags.Nothing;
        cam.Render();
    }

    protected override void ConfigureRenderPass() {
        Debug.Log("Configure render pass.");
        screenReadRegion = new Rect(0, 0, rt.width, rt.height);
        centroidPb = new MaterialPropertyBlock();
        pointPb = new MaterialPropertyBlock();

        // Voronoi Material
        material.SetBuffer(positionBufferId, positionBuffer);
        material.SetBuffer(colorBufferId, colorBuffer);

        // Point & Centroid Material
        pointPb.SetBuffer(positionBufferId, pointPositionBuffer);
        pointPb.SetVector(colorId, pointColor);
        centroidPb.SetBuffer(positionBufferId, centroidPositionBuffer);
        centroidPb.SetVector(colorId, centroidColor);
        
        rp = new RenderParams() {
            camera = cam,
            receiveShadows = false,
            worldBounds = renderBounds,
            shadowCastingMode = ShadowCastingMode.Off
        };
        
    }

    protected void CreateTextures() {
        base.CreateRenderTexture();
        RenderTexture.active = rt;
        cam.targetTexture = rt;
        voronoi = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false) {
            filterMode = FilterMode.Point,
        };
    }

    protected override void CreateBuffers() {
        base.CreateBuffers();
        centroidPositionBuffer = new ComputeBuffer(numRegions, sizeof(float) * 16);
    }
}
