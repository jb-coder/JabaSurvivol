# Secrets

This directory is for SOPS-encrypted secrets only.

Rules:

- Never commit plaintext secrets.
- Use SOPS with age.
- Keep the age private key in GitHub Actions secrets.
- Rotate credentials regularly.
- Do not store keystores, service account JSON files, or tokens in plaintext.
