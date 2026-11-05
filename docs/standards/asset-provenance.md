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

## AI-generated assets

If an asset is generated, record the tool, prompt source, date, and license terms in the provenance log. Do not claim human authorship or manual creation.

## Storage

- Keep assets small and compressed.
- Use Git LFS for large binaries.
- Do not commit source files that are not needed for builds unless they are part of the project workflow.
- Update `THIRD_PARTY_NOTICES.md` when attribution is required.

## Review

The reviewer and security agent reject assets without provenance or with incompatible licenses.
