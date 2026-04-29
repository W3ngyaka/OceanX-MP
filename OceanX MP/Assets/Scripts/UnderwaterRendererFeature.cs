using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

public class UnderwaterRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class UnderwaterSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        [Header("Water Tint")]
        public Color shallowColor = new Color(0.02f, 0.55f, 0.52f, 1f);
        public Color deepColor    = new Color(0.00f, 0.10f, 0.14f, 1f);
        [Range(0f,1f)] public float depthBlend        = 0.55f;
        [Range(0f,1f)] public float colorTintStrength = 0.50f;

        [Header("Caustics")]
        public Texture2D causticTexture                = null;
        public float     causticScale                  = 2.5f;
        public float     causticSpeed                  = 0.10f;
        [Range(0f,2f)] public float causticIntensity   = 0.35f;

        [Header("Atmospheric Fog")]
        public Color fogColor = new Color(0.00f, 0.18f, 0.20f, 1f);
        [Range(0f,1f)] public float fogDensity = 0.55f;

        [Header("God Rays")]
        public Color godRayColor                       = new Color(0.55f, 0.95f, 0.75f, 1f);
        [Range(0f,3f)]  public float godRayIntensity   = 1.6f;
        public float godRaySpeed                       = 0.05f;
        [Range(1f,20f)] public float godRayWidth       = 11f;
        [Range(2f,12f)] public float godRayCount       = 8f;

        [Header("Surface Glow")]
        [Range(0f,3f)]  public float topGlow           = 1.8f;
        public Color topGlowColor                      = new Color(0.60f, 1.00f, 0.80f, 1f);
        [Range(1f,20f)] public float topGlowFalloff    = 7f;

        [Header("Warp / Refraction")]
        [Range(0f,0.1f)] public float warpStrength     = 0.012f;
        public float warpSpeed                         = 0.3f;

        [Header("Lens")]
        [Range(0f,2f)]    public float vignetteStrength   = 1.1f;
        [Range(0f,0.02f)] public float aberrationStrength = 0.002f;
    }

    public UnderwaterSettings settings = new UnderwaterSettings();

    private UnderwaterPass _pass;
    private Material       _material;

    public override void Create()
    {
        var shader = Shader.Find("Custom/UnderwaterEffect");
        if (shader == null)
        {
            Debug.LogWarning("[Underwater] Shader 'Custom/UnderwaterEffect' not found.");
            return;
        }
        _material = CoreUtils.CreateEngineMaterial(shader);
        _pass = new UnderwaterPass(_material) { renderPassEvent = settings.renderPassEvent };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_material == null || _pass == null) return;
        if (renderingData.cameraData.cameraType != CameraType.Game) return;
        _pass.Setup(settings, _material);
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing) => CoreUtils.Destroy(_material);

    // ── Pass ──────────────────────────────────────────────────────────────
    class UnderwaterPass : ScriptableRenderPass
    {
        private Material _mat;

        static readonly int P_Shallow    = Shader.PropertyToID("_ShallowColor");
        static readonly int P_Deep       = Shader.PropertyToID("_DeepColor");
        static readonly int P_DepthBlend = Shader.PropertyToID("_DepthBlend");
        static readonly int P_TintStr    = Shader.PropertyToID("_ColorTintStrength");
        static readonly int P_CausticTex = Shader.PropertyToID("_CausticTex");
        static readonly int P_CausticScl = Shader.PropertyToID("_CausticScale");
        static readonly int P_CausticSpd = Shader.PropertyToID("_CausticSpeed");
        static readonly int P_CausticInt = Shader.PropertyToID("_CausticIntensity");
        static readonly int P_FogColor   = Shader.PropertyToID("_FogColor");
        static readonly int P_FogDensity = Shader.PropertyToID("_FogDensity");
        static readonly int P_RayColor   = Shader.PropertyToID("_GodRayColor");
        static readonly int P_RayInt     = Shader.PropertyToID("_GodRayIntensity");
        static readonly int P_RaySpd     = Shader.PropertyToID("_GodRaySpeed");
        static readonly int P_RayWidth   = Shader.PropertyToID("_GodRayWidth");
        static readonly int P_RayCount   = Shader.PropertyToID("_GodRayCount");
        static readonly int P_TopGlow    = Shader.PropertyToID("_TopGlow");
        static readonly int P_TopColor   = Shader.PropertyToID("_TopGlowColor");
        static readonly int P_TopFalloff = Shader.PropertyToID("_TopGlowFalloff");
        static readonly int P_WarpStr    = Shader.PropertyToID("_WarpStrength");
        static readonly int P_WarpSpd    = Shader.PropertyToID("_WarpSpeed");
        static readonly int P_Vig        = Shader.PropertyToID("_VignetteStrength");
        static readonly int P_Aber       = Shader.PropertyToID("_AberrationStrength");

        public UnderwaterPass(Material mat)
        {
            _mat = mat;
            requiresIntermediateTexture = true;
        }

        public void Setup(UnderwaterSettings s, Material mat)
        {
            _mat = mat;
            mat.SetColor(P_Shallow,    s.shallowColor);
            mat.SetColor(P_Deep,       s.deepColor);
            mat.SetFloat(P_DepthBlend, s.depthBlend);
            mat.SetFloat(P_TintStr,    s.colorTintStrength);
            if (s.causticTexture != null) mat.SetTexture(P_CausticTex, s.causticTexture);
            mat.SetFloat(P_CausticScl, s.causticScale);
            mat.SetFloat(P_CausticSpd, s.causticSpeed);
            mat.SetFloat(P_CausticInt, s.causticIntensity);
            mat.SetColor(P_FogColor,   s.fogColor);
            mat.SetFloat(P_FogDensity, s.fogDensity);
            mat.SetColor(P_RayColor,   s.godRayColor);
            mat.SetFloat(P_RayInt,     s.godRayIntensity);
            mat.SetFloat(P_RaySpd,     s.godRaySpeed);
            mat.SetFloat(P_RayWidth,   s.godRayWidth);
            mat.SetFloat(P_RayCount,   s.godRayCount);
            mat.SetFloat(P_TopGlow,    s.topGlow);
            mat.SetColor(P_TopColor,   s.topGlowColor);
            mat.SetFloat(P_TopFalloff, s.topGlowFalloff);
            mat.SetFloat(P_WarpStr,    s.warpStrength);
            mat.SetFloat(P_WarpSpd,    s.warpSpeed);
            mat.SetFloat(P_Vig,        s.vignetteStrength);
            mat.SetFloat(P_Aber,       s.aberrationStrength);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer) return;

            var source   = resourceData.activeColorTexture;
            var destDesc = renderGraph.GetTextureDesc(source);
            destDesc.name        = "_UnderwaterDest";
            destDesc.clearBuffer = false;
            var dest = renderGraph.CreateTexture(destDesc);

            RenderGraphUtils.BlitMaterialParameters para = new(source, dest, _mat, 0);
            renderGraph.AddBlitPass(para, passName: "UnderwaterEffect");

            resourceData.cameraColor = dest;
        }
    }
}
