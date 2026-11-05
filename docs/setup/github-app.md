# Reviewer GitHub App

JabaSurvivol uses a GitHub App as the independent reviewer. This avoids creating a second GitHub user account while keeping builder and reviewer identities separate.

## Current app

- Name: `jaba-survivol-reviewer-jb`
- Slug: `jaba-survivol-reviewer-jb`
- App ID: `5629026`
- Installation ID: `178952745`
- Installed on: `jb-coder/JabaSurvivol`

The app is not a user account. It is installed on the repository and acts as `jaba-survivol-reviewer-jb[bot]`.

## Permissions

- Contents: read.
- Pull requests: read and write.
- Issues: read and write.
- Checks: read and write.
- Metadata: read.

No webhooks are required.

## Credentials

The App private key and configuration are stored outside the repository:

```text
%USERPROFILE%\.config\jaba-survivol\github-app\private-key.pem
%USERPROFILE%\.config\jaba-survivol\github-app\config.json
```

The config contains:

```json
{
  "appId": "5629026",
  "slug": "jaba-survivol-reviewer-jb",
  "privateKeyPath": "C:\\Users\\<user>\\.config\\jaba-survivol\\github-app\\private-key.pem",
  "installationId": "178952745"
}
```

Never commit the private key or config file.

## Token flow

`tools/agent/gh-reviewer.ps1` calls `tools/agent/gh-app-token.ps1`.

The token script:

1. Reads the App ID and private key.
2. Builds a short-lived RS256 JWT.
3. Finds the installation for `jb-coder` if needed.
4. Exchanges the JWT for a one-hour installation access token.
5. Passes the token to `gh` only for the duration of the command.

The token is never printed by the wrapper.

## Recreating the app

If the app is lost or the private key is rotated:

```powershell
pwsh tools/bootstrap/New-GitHubReviewerApp.ps1
```

The script opens GitHub, creates the app through the manifest flow, installs it only on `jb-coder/JabaSurvivol`, and writes the credentials to the secure local directory.

## Verification

```powershell
pwsh tools/agent/gh-reviewer.ps1 api repos/jb-coder/JabaSurvivol --jq ".full_name"
pwsh tools/agent/gh-reviewer.ps1 pr list --repo jb-coder/JabaSurvivol --limit 3
```

The output must resolve the repository and list pull requests without printing a token.

## Review behavior

The reviewer agent posts a review and manages labels:

- `agent:approved` when the review passes.
- `agent:changes-requested` when it fails.

The `pr-policy` check requires `agent:approved` and rejects `agent:changes-requested`. The repository rulesets require one approval and all required checks before merge.
