using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Color-safe full-screen bloom and vignette for the 2D renderer.
    /// URP's integrated post-processing path is not used on the Android GLES
    /// emulator because its color-grading LUT path produces an unusable image.
    /// </summary>
    public sealed class JabaNeonPostFeature : FullScreenPassRendererFeature
    {
#pragma warning disable CS0618
        public override void AddRenderPasses(
            ScriptableRenderer renderer,
            ref RenderingData renderingData)
        {
            if (!VisualPolish.PostProcessingActive)
            {
                return;
            }

            base.AddRenderPasses(renderer, ref renderingData);
        }
#pragma warning restore CS0618
    }
}
