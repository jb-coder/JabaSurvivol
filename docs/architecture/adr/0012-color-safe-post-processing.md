# ADR-0012: Color-Safe Post-Processing for the 2D Renderer

- Status: Accepted
- Date: 2026-11-16
- Owners: Architecture and rendering
- Related issues: #203

## Context

v0.8.0 ships with URP camera post-processing disabled because enabling it on
the Android emulator turns the scene into a beige, low-contrast image. The
project default-volume profile is neutral, and removing the project and package
default profiles does not fix the output, so the problem is in the URP 2D
post-processing path rather than in the profile values.

The failure is reproducible with SwiftShader and with the host GPU path. It is
not fixed by:

- A neutral runtime profile.
- An empty default volume profile.
- Low-dynamic-range or high-dynamic-range color grading.
- HDR rendering on or off.
- Gamma or linear color space.

The color-grading LUT path is the common element. The integrated path also
renders Screen Space Overlay UI through the camera target, which can lower HUD
contrast. URP 2D does not expose a public Render Graph color texture through
`UniversalResourceData`, so the stock full-screen feature samples a black
texture in Render Graph mode.

## Decision

1. Keep `renderPostProcessing` disabled on the main camera.
2. Ship a custom `JabaNeonPostFeature` derived from
   `FullScreenPassRendererFeature` at the
   `BeforeRenderingPostProcessing` injection point.
3. Use a first-party `JabaSurvivol/NeonPost` shader that samples the camera
   color, applies an eight-tap threshold bloom, and applies a subtle vignette.
   The shader does not use a color-grading LUT or tonemapping.
4. Enable URP compatibility mode with the `URP_COMPATIBILITY_MODE` scripting
   define so the feature can use the 2D renderer's camera color target. This is
   an explicit, temporary platform workaround.
5. Install the feature, material, and shader from
   `ProjectSetup.Configure` so clean builds and local runs stay in sync.
6. Keep post-processing behind a quality check: devices with two or fewer
   cores or less than 1536 MB of system memory use the unprocessed neon path.
7. Keep the HUD on the native overlay path so text and touch controls are not
   filtered by the camera effect.
8. Revisit the Render Graph path when URP exposes a public 2D color texture
   hook. The follow-up must remove compatibility mode and the workaround.

## Consequences

- Bloom and vignette are visible without changing the arena, actor, or HUD
  contrast.
- The custom pass adds one full-screen color copy and one full-screen draw.
  The emulator draw-call and frame budgets are measured in the release
  evidence.
- Compatibility mode is deprecated upstream. It is isolated to this one pass
  and can be removed without changing gameplay code or save data.
- The shader is first-party and is covered by the existing asset provenance
  rules.
- Low-end devices keep the v0.8.0 visual fallback.

## Alternatives considered

- Keep URP post-processing disabled: rejected because bloom and vignette are
  part of the intended neon look.
- Keep tuning the volume profile: rejected because an empty profile still
  corrupts the output.
- Implement a custom Render Graph pass against internal 2D resource data:
  rejected because it depends on internal APIs and is more fragile than the
  full-screen feature.
- Render the scene to a RenderTexture and display it through a UI RawImage:
  rejected because it adds a second camera path, extra memory, and more UI
  ordering risk.
- Require a physical device only: rejected because the emulator is the current
  automated evidence path and the washout must be fixed there.

## Reversibility

Revert this ADR by removing the renderer feature from `Renderer2D.asset`,
removing the shader and material, and restoring the v0.8.0 fallback. No save,
progression, or gameplay migration is involved.
