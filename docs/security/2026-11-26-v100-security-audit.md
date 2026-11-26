# v1.0.0 Security and Dependency Audit

- Date: 2026-11-26
- Issue: #240
- Branch: `chore/240-v100-security-audit`
- Baseline commit: `0e3f807` (`develop`)
- Scope: dependencies, secrets, authentication and refresh, authorization,
  leaderboard authority, cloud saves, BFF rate limits, Android release
  configuration, debug surface, and ADR-0008 donations.
- Out of scope: penetration testing of third-party services, payments (not
  implemented), and physical-device profiling.

## Verdict

No P0 or P1 security finding was identified. The v1.0.0 backend, client, and
Android release configuration meet the audit acceptance criteria with the
accepted risks and follow-ups recorded below. No code fix was required inside
this issue.

## 1. Dependency audit

Command:

```text
dotnet list JabaSurvivol.slnx package --vulnerable --include-transitive
```

Result: exit code 0. All 17 projects report no vulnerable packages. Full output
is in `evidence/2026-11-26-dependency-audit.txt`.

GitHub advisory state:

- Open Dependabot alerts: 0.
- Open CodeQL alerts: 0.
- Open secret scanning alerts: 0.

Supply-chain controls already present:

- `container-ci` builds all four images for `linux/arm64` and scans them with
  Trivy `v0.36.0` pinned to
  `ed142fd0673e97e23eac54620cfb913e5ce36c25`, failing on `CRITICAL` or `HIGH`
  fixes.
- Dependabot watches NuGet and GitHub Actions weekly on `develop`.
- `security.yml` runs CodeQL and a non-blocking NuGet audit on pull requests,
  pushes, and a weekly schedule.

Gap: the release workflow does not publish an SBOM or artifact attestation.
This is P2 follow-up #249.

## 2. Secret scanning

The scan used `git ls-files` and `git grep -l`, so no match value was printed.
Detailed commands and file-only results are in
`evidence/2026-11-26-secret-scan.txt`.

Results:

- Tracked secret-like files: `.env.example` and `deploy/secrets/README.md` only.
- Private key markers: none.
- JWT-like literals: none.
- Known token prefixes: none.
- Hardcoded signing keys: none.
- Connection strings with credentials: `.env.example` only, as a documented
  local PostgreSQL default.
- Non-example `.env` files: none.
- Open GitHub secret scanning alerts: 0.

Runtime secret handling:

- `Jwt__SigningKey` is generated per AppHost run or supplied from the
  `jabasurvivol-secrets` Kubernetes Secret.
- `ConnectionStrings__Database` is supplied from the Kubernetes Secret or the
  local environment.
- The Unity client stores only the refresh token, encrypted with Android
  Keystore AES/GCM or Windows DPAPI. Access tokens stay in memory.
- Logs and evidence use fixed or redacted messages.

## 3. Authentication, refresh, and authorization

### Access tokens

- HMAC SHA-256, configurable issuer and audience, lifetime validation, and a
  minimum 32-character signing key. Configuration validation rejects missing or
  weak values and access lifetimes outside 1-60 minutes.
- Default access lifetime is 15 minutes with 30 seconds of clock skew.
- `MapInboundClaims` is disabled, and the account ID comes from the `sub` claim.

### Refresh tokens and rotation

- 32 random bytes, base64url encoded, hashed with SHA-256 before storage, valid
  for 30 days by default.
- The token hash has a unique index. Sessions carry a family ID, expiry, and
  revocation timestamp.
- Refresh rotates through a conditional update. A second use of an already
  rotated token revokes the whole family, which is the expected reuse-detection
  behavior.
- Logout requires an authenticated access token, looks up the session by hash
  and account ID, and revokes the family.

### Passwords and login

- Password hashing uses ASP.NET Core `PasswordHasher`.
- Registration requires 8-128 characters with at least one letter and one
  digit. Usernames are 3-32 ASCII letters, digits, underscore, or hyphen.
- Unknown users run a dummy password verification to reduce timing-based
  enumeration.
- Register returns 409 for an existing username. This is low-risk account
  enumeration because no email, profile, or personal data is exposed.

### Authorization

- Saves, leaderboard submission, and telemetry derive the account ID from the
  authenticated principal and filter every query by that ID.
- The BFF exposes an explicit route allowlist. Internal status, health, and
  upstream-only routes are not proxied.
- Protected BFF routes require authentication before proxying and return
  ProblemDetails on failure.
- Internal services are `ClusterIP`; only the BFF is exposed through ingress.

### Rate limits

| Surface | Limit | Partition |
|---|---|---|
| Identity register, login, refresh, logout | 20/min | client IP |
| BFF authentication routes | 10/min | client IP |
| BFF account routes | 60/min | account ID, or IP before authentication |
| BFF anonymous config and leaderboard read | 120/min | client IP |
| Game leaderboard submission | 30/min | account ID |

Rejected requests return 429 ProblemDetails with `Retry-After`.

## 4. Leaderboard and cloud saves

### Leaderboard

- Submission requires authentication, binds the run to the JWT account, and
  rejects empty run IDs, out-of-range rounds, kills, durations, and client
  versions.
- Duplicate `(accountId, runId)` submissions return the stored run instead of
  creating a second row.
- The username comes from the validated JWT, not the request body.
- The top query is static SQL with no user interpolation, uses one best run per
  account, and clamps the limit to the contract maximum.
- The values themselves are client-authored. Range validation prevents absurd
  values but cannot prove a legitimate run. This is an accepted v1.0.0 risk
  (S-02) and P2 follow-up #248.

### Cloud saves

- The primary slot is bound to the JWT account.
- The body is limited by `SaveContract.MaximumBodyBytes` and checked again after
  reading.
- The server requires `baseRevision`, validates `schemaVersion`, supplies, XP,
  level, skill points, highest round, list field types, and the full profile
  through `Profile.Validate()`.
- Revision conflicts return 409 with the current revision and timestamp. ETags
  support `If-None-Match` and return 304 when unchanged.
- Progression values are client-authored and intentionally local-first
  (ADR-0004 and ADR-0006). The server protects ownership and consistency, not
  legitimacy. This is an accepted v1.0.0 risk (S-03) and P2 follow-up #248.

## 5. BFF, network, and containers

- Kubernetes NetworkPolicies default-deny ingress and egress, allow DNS, allow
  ingress only to the BFF, allow the BFF to reach identity and game APIs, allow
  those services and the worker to reach PostgreSQL, and allow outbound 443.
- Containers run as non-root, with a read-only root filesystem, no privilege
  escalation, all capabilities dropped, `RuntimeDefault` seccomp, resource
  limits, and health probes.
- Secrets come from a Kubernetes Secret, not from the ConfigMap.
- Proxy failures map to safe 502/504 ProblemDetails without upstream detail.
- Correlation IDs are sanitized and length-limited.
- Gap: the BFF ingress policy allows any namespace, and forwarded-header trust
  is cleared. In the current single-tenant cluster this is low risk, but it
  weakens source-IP rate limiting if another workload is compromised. This is
  P3 follow-up #250.

## 6. Android release configuration

Tracked manifest and generated release merged manifest are quoted in
`evidence/2026-11-26-android-manifest.txt`.

- Permissions: `INTERNET` and `VIBRATE` only.
- Exported components: one launcher activity,
  `com.unity3d.player.UnityPlayerActivity`, with `MAIN`/`LAUNCHER`. No service,
  receiver, provider, or extra exported activity.
- No `android:debuggable`, `android:usesCleartextTraffic`, or network security
  config override.
- Production build sets IL2CPP, ARM64, minimum API 26, target API 36, landscape
  orientation, and `InsecureHttpOption.NotAllowed`.
- `BuildScript.BuildAndroid` sets `EditorUserBuildSettings.development = false`.
  The separate emulator build is the only development build and enables x86_64
  plus development-only insecure HTTP for loopback and emulator hosts.
- `GameBootstrap` development scenarios, the secure-storage probe, and the
  performance probe are compiled under `#if DEVELOPMENT_BUILD` and are absent
  from the production build.
- The development session seed accepts a refresh token only from an Android
  intent extra in a development build. No test credential is hardcoded.
- OpenAPI is mapped only when `app.Environment.IsDevelopment()`. The production
  ConfigMap sets `ASPNETCORE_ENVIRONMENT=Production`.
- TLS is enforced on the production API URL. The Kubernetes ingress uses
  cert-manager `letsencrypt-prod`.

## 7. Donations and ADR-0008

- No payment, wallet, receipt, premium currency, IAP, entitlement, or cosmetic
  store code is present.
- No donation link is rendered in game yet. Issue #236 is blocked on the owner's
  PayPal URL and a needs-human decision.
- ADR-0008, the game design, the economy document, and the security standard
  consistently describe donations as optional, external, voluntary, and
  reward-free.
- If the link is added later, it must open an external page, must be labeled as
  a donation rather than a purchase, and must not implement a purchase flow.

Result: compliant with ADR-0008 for the current phase.

## 8. Findings by severity

| ID | Severity | Finding | Status |
|---|---|---|---|
| S-01 | P2 | Login does not bound password length before PBKDF2 verification, and identity and leaderboard endpoints lack explicit small request-size limits. Rate limits reduce but do not remove the cost. | Accepted for v1.0.0; follow-up #247. |
| S-02 | P2 | Leaderboard values are client-authored and only range-validated. | Accepted risk; follow-up #248. |
| S-03 | P2 | Cloud-save progression is client-authored; the server validates ownership, schema, and ranges only. | Accepted by design; follow-up #248. |
| S-04 | P2 | Release artifacts have no SBOM or provenance attestation. | Follow-up #249. |
| S-05 | P3 | BFF ingress allows any namespace and forwarded-header trust is cleared. | Follow-up #250. |
| S-06 | P3 | Expired and revoked refresh sessions are not pruned. | Follow-up #251. |
| S-07 | P3 | Access tokens remain valid for up to 15 minutes after logout. | Accepted standard tradeoff. |
| S-08 | Info | Registration reveals whether a username exists through 409. | Accepted low risk; no personal data. |
| S-09 | Info | The BFF rate limiter is in-memory and per instance. | Accepted while `replicas: 1`; revisit if scaled. |
| S-10 | Info | Health and aliveness endpoints are reachable through ingress. | Accepted; no sensitive data. |

No P0 or P1 finding exists in this audit.

## 9. Accepted risks

- **Client-authoritative progression and leaderboard (S-02, S-03).** The game is
  offline-first and single-player, with no premium currency, paid reward, or
  competitive season. Server ownership, revision, range, and schema checks are
  present. Revisit before competitive rewards or monetization (ADR-0004,
  ADR-0006, follow-up #248).
- **In-memory BFF rate limiter (S-09).** The production deployment has one
  replica, so there is no cross-replica bypass today. Revisit when scaling.
- **Post-logout access-token lifetime (S-07).** Access tokens last at most 15
  minutes, and logout revokes the refresh family. A denylist is not justified
  for v1.0.0.
- **Username enumeration (S-08).** Registration returns 409 for a taken
  username. No email, profile, or personal data is exposed.
- **Public health endpoints (S-10).** The endpoints return only fixed status
  text and are needed by the Kubernetes probes.

## 10. Follow-ups

| Issue | Priority | Summary |
|---|---|---|
| #247 | P2 | Bound auth and leaderboard request sizes. |
| #248 | P2 | Evaluate server-authoritative leaderboard and cloud-save validation. |
| #249 | P2 | Generate SBOMs and artifact attestations for releases. |
| #250 | P3 | Restrict BFF ingress and pin forwarded-header proxies. |
| #251 | P3 | Prune expired and revoked refresh sessions. |

## 11. Verification evidence

- `evidence/2026-11-26-gates.txt`: restore, Release build, 272 tests, format,
  dependency audit, secret scan, and advisory counts.
- `evidence/2026-11-26-dependency-audit.txt`: full dependency audit output.
- `evidence/2026-11-26-secret-scan.txt`: secret-scan commands and file-only
  results.
- `evidence/2026-11-26-android-manifest.txt`: tracked and release merged
  Android manifest excerpts.

Summary:

```text
dotnet build JabaSurvivol.slnx -c Release --no-restore
Build succeeded. 0 warnings, 0 errors.

dotnet test JabaSurvivol.slnx -c Release --no-build --no-restore
272 passed, 0 failed, 0 skipped.

dotnet format JabaSurvivol.slnx --verify-no-changes --no-restore
Exit code 0.

dotnet list JabaSurvivol.slnx package --vulnerable --include-transitive
No vulnerable packages in 17 projects.
```

## 12. Reviewed files

- `src/Shared/Security/*`
- `src/Services/Identity.Api/Program.cs`,
  `Endpoints/IdentityEndpoints.cs`, `Auth/IdentityAuthServices.cs`,
  `Data/IdentityDbContext.cs`
- `src/Services/Game.Api/Program.cs`, `Saves/*`, `Leaderboards/*`,
  `Telemetry/*`, `Configuration/*`
- `src/Gateway/Bff/*`
- `src/ServiceDefaults/Extensions.cs`
- `src/Services/Telemetry.Worker/*`
- `unity/JabaSurvivol/Assets/Plugins/Android/AndroidManifest.xml`
- `unity/JabaSurvivol/Assets/Plugins/Android/jaba_locales.androidlib/*`
- `unity/JabaSurvivol/Assets/_Project/Scripts/Editor/BuildScript.cs`,
  `ProjectSetup.cs`
- `unity/JabaSurvivol/Assets/_Project/Scripts/Runtime/GameBootstrap.cs`,
  `Networking/*`
- `deploy/k8s/**`, `deploy/secrets/README.md`
- `.github/workflows/security.yml`, `container-ci.yml`, `release.yml`
- `.github/dependabot.yml`, `.env.example`, `.gitignore`

## 13. Limitations

- This audit is configuration, source, and automated-test based. It did not run
  a penetration test, dynamic application scan, or physical-device test.
- Unity editor and PlayMode tests were not run here. Unity CI remains disabled
  (#27).
- Container images were not rebuilt locally. The Trivy gate and pinned action
  are reviewed from CI configuration.
- Dependency results depend on the NuGet and GitHub advisory sources at the
  audit date.
