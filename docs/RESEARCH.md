# Research log

## 2026-11-06 — Unity security and version

- Source: https://activation.unity3d.com/security/unity-emudhra-compromise, accessed 2026-11-06. Unity lists 6000.3.27f2 as the first fixed 6.3 LTS version for the October 2026 eMudhra certificate compromise and recommends patched editors. Installed 6000.3.28f1 is later; this inference applies to that advisory only and does not establish general security or the actual project's version.
- Source: https://unity.com/releases/editor/whats-new/6000.3.28f1, accessed 2026-11-06. Unity dates 6000.3.28f1 to November 3, 2026. It is installed locally; project compatibility/import remains untested.

## 2026-11-06 — Existing project references

- https://github.com/dlg309/Echo-Mountain, accessed 2026-11-06: README describes a browser-based top-down prototype and its rules/tests. It is a conceptual input, not a validated 3D model. Only repository overview/README was inspected so far.
- https://github.com/dlg309/ai-game-dev-research, accessed 2026-11-06: README describes a VitePress planning notebook, not a game implementation. The research site could not be opened by the browsing tool; detailed research remains to be reviewed.
- https://github.com/dlg309/EchoMountainGame: local checkout inspected; initially only README tracked, then brief appeared in commit `fcb144a` during discovery. No project markers in this checkout.

## 2026-11-06 — 6.3 physics references (documented; not yet tested in game)

- https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody.AddForce.html, accessed 2026-11-06: forces accumulate for the next simulation and ForceMode changes the velocity update and mass/time dependence. Proposed: apply continuous forces per fixed tick, and pop as a deliberate impulse; tune only after measured tests.
- https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.SphereCast.html, accessed 2026-11-06: describes a swept sphere query and its parameters. Proposed: evaluate multiple board probes with filtering; collision, starting overlap, seams and contact normals must be tested in the actual editor.
- https://docs.unity3d.com/6000.3/Documentation/Manual/physics-optimization-cpu-frequency.html, accessed 2026-11-06: fixed step frequency trades accuracy and stability against CPU work; the documented default is 0.02 seconds. Proposed 1/60-second step is a project experiment, not a performance result.

## 2026-11-06 — Additional reference review

- https://github.com/dlg309/Echo-Mountain/blob/main/echo-mountain/README.md, accessed 2026-11-06: the fuller prototype README describes practice recovery, multiple modes, landing-awarded tricks and separate local records. It also describes timed predefined tricks and automatic halfpipe traversal; those are reference behavior, not the target 3D physics.
- https://github.com/dlg309/ai-game-dev-research/tree/main/docs, accessed 2026-11-06: inspected planning decisions, roadmap, engine comparison, research overview and source list through their raw files. The notebook remains early research and explicitly marks its engine choice and experiments as open; the current user brief supplies the Unity decision. No benchmark or playtest is implied.

## 2026-11-06 — Unity test and build entry points

- https://docs.unity3d.com/Packages/com.unity.test-framework@1.4/manual/reference-command-line.html, accessed 2026-11-06: Test Framework 1.4 documents `-runTests`, platform and result-file options. The template declares 1.4.2, while the documentation page identifies 1.4.6; actual package resolution and commands remain untested.
- https://docs.unity3d.com/6000.3/Documentation/ScriptReference/BuildPipeline.BuildPlayer.html, accessed 2026-11-06: documents editor-driven player builds. A local development-build menu has been drafted but has not been compiled or used.
- https://docs.unity.com/en-us/engine/6000.3/manual/get-started/install-and-upgrade/licenses-and-activation/activation-faq, accessed 2026-11-06: Unity documents license activation and troubleshooting; signing out and back into Hub is a possible troubleshooting step in some activation cases, not proof of the cause here.
