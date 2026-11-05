# JabaSurvivol.Core

Pure C# game rules shared by the Unity client and the .NET backend.

Rules:

- No `UnityEngine` references.
- Deterministic and unit-testable.
- Targets .NET Standard 2.1 compatibility.
- Contains stats, skills, economy, rounds, damage, armor, and save models.
- Unity consumes the same source through the local UPM package at `unity/JabaSurvivol/Packages/com.jabasurvivol.core` once the Unity project exists.
