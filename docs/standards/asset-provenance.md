# Asset Provenance

## Rules

Every third-party asset must be recorded before or with the commit that adds it.

Required fields:

- Asset name and version.
- Source URL.
- Author or publisher.
- License.
- Attribution requirements.
- Date added.
- Intended use.

## Allowed licenses

- CC0.
- Public domain.
- CC BY with attribution.
- CC BY-SA when the project can comply.
- Other licenses approved by an ADR.

Do not use assets with unclear, non-commercial, or incompatible licenses.

## First-party generated assets

JabaSurvivol uses a deterministic in-repo generator for its visual set.

- Generator: `tools/ArtGenerator`.
- Output root: `unity/JabaSurvivol/Assets/Resources/Art`.
- Manifest: `unity/JabaSurvivol/Assets/_Project/Art/art-manifest.json`.
- Author: the project identity configured for the repository.
- License: project-owned; no third-party asset license applies.
- Tool-only dependency: SkiaSharp, recorded in `THIRD_PARTY_NOTICES.md`.

Every generated asset must appear in the manifest with:

- `path`: path relative to the output root.
- `resourcePath`: Unity `Resources` path without extension.
- `width` and `height`: texture dimensions.
- `pivotX` and `pivotY`: normalized pivot in `[0, 1]`.
- `category`: character, weapon, projectile, effect, pickup, UI, environment,
  or app.
- `paletteVersion`: the palette revision used to draw the asset.
- `sha256`: hash of the committed PNG.

Do not hand-edit generated PNGs. Change the generator, regenerate, review the
contact sheet, and commit the output and manifest together.

## Generated assets from external tools

If a future asset is produced with an external generative tool, record the tool,
prompt source, date, and license terms in the provenance log. Do not claim human
authorship or manual creation, and do not attribute the tool in repository
content.

## Storage

- Keep assets small and compressed.
- Use Git LFS for large binaries.
- Do not commit source files that are not needed for builds unless they are part of the project workflow.
- Update `THIRD_PARTY_NOTICES.md` when attribution is required.

## Review

The reviewer and security agent reject assets without provenance or with incompatible licenses.
