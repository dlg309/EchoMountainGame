# Decisions

- 2026-11-06: Do not create or nest a Unity project before the real project is located. The workspace has no Unity project markers, and the open editor belongs to a different tutorial. This preserves any existing scenes and avoids a duplicate project.
- 2026-11-06: Retain the brief's Unity 6.3 LTS / URP target pending discovery of the actual project. 6000.3.28f1 is locally installed and later than the 6000.3.27f2 fix listed for the October 2026 eMudhra advisory. This is advisory-specific, not a blanket security guarantee; project version is unknown.
- 2026-11-06: Keep browser prototype and research repository separate; use them as design inputs, not as 3D physics implementations.

- 2026-11-06: User confirmed that EchoMountainGame in Documents is the intended location, without an existing project path. The repository root is now the chosen Unity project root. Seeded only missing `Assets`, `Packages`, and `ProjectSettings` from the locally installed 6000.3.28f1 Unity 3D cross-platform URP template; existing repository files were not overwritten. This is an initialization in the requested location, not a nested project.
