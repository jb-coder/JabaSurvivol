# Audio Standards

## Purpose

This document defines the v0.7.1 audio contract for JabaSurvivol. It records
the current implementation, the known defects, the event-to-cue matrix, the
settings contract, and the release gate. Final authored music and production
sound effects remain in v1.0.0.

## Baseline before v0.7.1

- `GameAudio` is a `MonoBehaviour` created by `GameSession`.
- Clips are generated procedurally at runtime at 22050 Hz and cached.
- Six cues exist: `Kill`, `Hit`, `LevelUp`, `Reward`, `Victory`, and `Defeat`.
- One `AudioSource` plays every cue with `PlayOneShot`.
- Mute is stored in `PlayerPrefs` under `audio.muted`.
- The pause menu is the only place that exposes the mute toggle.
- `GameSession` saves the profile on `OnApplicationPause` but does not suspend
  audio.
- There is no volume control, voice limit, or per-cue cooldown.

## v0.7.1 implementation

- 33 procedural cues cover combat, skills, the boss, rounds, and UI.
- A fixed pool of 12 sources bounds simultaneous one-shots.
- Per-cue cooldowns prevent duplicate cues from stacking.
- Master mute, master volume, and effects volume are persisted in
  PlayerPrefs and applied to every source.
- Pause, focus loss, and Android background suspend audio and stop active
  one-shots. Resume restores audio only when the session is playing.
- The main menu and pause menu open an audio panel with mute, VOL, and FX
  controls. VOL and FX are language-neutral technical abbreviations.
- The panel reuses the existing pause.sound.on, pause.sound.off, and
  settings.language.close keys, so no Spanish locale table change is needed.

## Known defects and gaps

| ID | Severity | Finding | Resolution | Tracking |
|---|---|---|---|---|
| A-01 | P1 | Mute only controls one source, so future UI or music sources would bypass it. | Centralize routing and apply mute and volume to every source. | #176 |
| A-02 | P1 | There is no master or effects volume. | Add both settings with clamped defaults and persistence. | #176 |
| A-03 | P1 | Pause, focus loss, and Android background do not suspend audio. | Add an explicit suspend and resume lifecycle. | #178 |
| A-04 | P2 | Mute is only reachable from the in-run pause menu. | Expose audio controls on the main and pause menus. | #181 |
| A-05 | P1 | Weapon fire, empty magazine, reloads, weapon switching, player bullet impacts, active skills, boss events, round transitions, and UI actions are silent. | Add the cues listed in the matrix. | #179, #180, #181 |
| A-06 | P2 | `Hit` means player damage; player bullet impacts on enemies are silent and ambiguous. | Keep `Hit` for player damage and add `Impact` and `BossHit` cues. | #179 |
| A-07 | P2 | Overlapping one-shots are unbounded and can clip. | Add a source pool, per-cue cooldown, and a global voice limit. | #176 |
| A-08 | P2 | No test asserts that every `GameAudioCue` value has a clip. | Add EditMode coverage for the cue catalog. | #182 |
| A-09 | P3 | Audio settings are device-local because they use `PlayerPrefs`. | Accept for v0.7.1 and document; cloud settings are out of scope. | Accepted risk |
| A-10 | P3 | There is no Android audio-focus or latency evidence. | Add filtered logcat and emulator smoke evidence. | #182 |

## Cue matrix

Every row is silent while the master mute is enabled or audio is suspended.
The mute column records that shared behavior explicitly. The implementation uses one
global pool of 12 sources and a shared 0.03 s per-cue cooldown. Priority is a
gameplay-importance hint, not a separate per-cue voice budget.

| Event | Cue | Priority | Mute behavior |
|---|---|---|---|
| Player takes damage | `Hit` | High | Silent |
| Player bullet impacts a zombie or boss | `Impact` | Low | Silent |
| Player bullet damages the boss | `BossHit` | Low | Silent |
| Zombie killed | `Kill` | High | Silent |
| Boss killed | `BossDeath` | High | Silent |
| Pistol fired | `PistolFire` | High | Silent |
| SMG fired | `SmgFire` | High | Silent |
| Shotgun fired | `ShotgunFire` | High | Silent |
| Rifle fired | `RifleFire` | High | Silent |
| LMG fired | `LmgFire` | High | Silent |
| Empty magazine | `Empty` | Medium | Silent |
| Reload started | `ReloadStart` | Medium | Silent |
| Reload completed | `ReloadComplete` | Medium | Silent |
| Weapon switched | `WeaponSwitch` | Medium | Silent |
| Turret deployed | `TurretDeploy` | Medium | Silent |
| Turret fired | `TurretFire` | Low | Silent |
| Dash activated | `Dash` | Medium | Silent |
| Shockwave activated | `Shockwave` | Medium | Silent |
| Boss spawned | `BossSpawn` | High | Silent |
| Boss telegraph started | `BossTelegraph` | High | Silent |
| Boss slam resolved | `BossSlam` | High | Silent |
| Boss enraged | `BossEnrage` | High | Silent |
| Round started | `RoundStart` | Medium | Silent |
| Round cleared | `RoundClear` | High | Silent |
| Reward selected | `Reward` | High | Silent |
| Level up | `LevelUp` | High | Silent |
| Boss killed and victory shown | `Victory` | High | Silent |
| Player health reaches zero in `EndRun` | `Defeat` | High | Silent |
| UI button clicked | `UiClick` | Low | Silent |
| Panel opened | `UiOpen` | Low | Silent |
| Panel closed | `UiClose` | Low | Silent |
| Purchase or upgrade succeeded | `Purchase` | Medium | Silent |
| Rejected action or error | `Error` | Medium | Silent |

## Settings contract

| Setting | Type | Default | Range | Persistence key |
|---|---|---:|---|---|
| Master mute | Boolean | `false` | `true` or `false` | `audio.muted` |
| Master volume | Float | `1.0` | `0.0` to `1.0` | `audio.masterVolume` |
| Effects volume | Float | `0.8` | `0.0` to `1.0` | `audio.effectsVolume` |

Rules:

- Effective volume is `0` when muted or suspended; otherwise it is
  `masterVolume * effectsVolume`.
- A cue is not started and does not increment the played-cue count when the
  effective volume is zero.
- Volume changes clamp to the valid range and apply immediately.
- `audio.muted` from v0.5.1 remains the source of truth for mute, so existing
  preferences keep working.
- Audio controls are available on the main menu and the pause menu.
- The controls use existing localization keys and language-neutral symbols so they render in English and Spanish without new translations.

## Audio control labels

The audio panel reuses existing keys:

| Control | Key |
|---|---|
| Main menu and pause menu entry | `pause.sound.on` / `pause.sound.off` |
| Mute button | `pause.sound.on` / `pause.sound.off` |
| Close button | `settings.language.close` |
| Volume rows | `VOL` and `FX` language-neutral abbreviations with percentages |

## Lifecycle contract

| Situation | Behavior |
|---|---|
| Session created | Build clips, create the source pool, load preferences. |
| Game paused | Suspend and stop active one-shots. |
| Game resumed | Resume only when the session is playing and not muted. |
| Application paused or focus lost | Suspend and stop active one-shots. |
| Application resumed or focus regained | Resume when the session is playing and not muted. |
| Return to main menu | Stop all one-shots. |
| Session destroyed | Destroy generated clips and release the source pool. |

## Performance contract

- No allocation in `Play`, `Update`, or other hot paths.
- Clips are generated once and cached.
- The source pool is fixed at creation time.
- The default global voice limit is 12 simultaneous one-shots.
- Per-cue cooldowns are short and documented in code.

## Verification

Automated coverage in the implementation PR:

- PlayMode coverage for cue generation, cue catalog completeness, volume
  clamping and persistence, mute, suspend and resume, voice limits, source
  release, and every event-to-cue mapping.
- Pseudo-locale coverage for the audio panel layout.
- PlayMode coverage for the main-menu and pause-menu audio controls.

Release evidence tracked in #182:

- English and Spanish phone and tablet screenshots.
- Android APK build, install, launch, mute, combat, pause, background, resume,
  and filtered logcat.
- Evidence stored under `docs/playtests/` with honest automated-verification
  language.

## Release gate

- [ ] Every `GameAudioCue` value maps to a non-empty generated clip.
- [ ] Mute and volume apply to every source and persist across restarts.
- [ ] Pause, focus loss, and Android background suspend audio.
- [ ] Main menu and pause menu expose mute and volume in English and Spanish.
- [ ] The cue matrix is implemented or has an explicit accepted gap.
- [ ] No new per-frame allocation in combat or UI hot paths.
- [ ] Android filtered logcat has no audio errors.
- [ ] Release notes state that final authored audio remains in v1.0.0.

## Out of scope

- Final authored music, ambience, and production sound effects.
- Voice acting.
- Audio middleware.
- Spatialized 3D audio.
- Cloud-synced audio settings.
- Audio focus ducking for external media.
