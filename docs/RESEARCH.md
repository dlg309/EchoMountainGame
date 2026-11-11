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

## 2026-11-08 — security recheck

- https://unity.com/security and https://unity.com/security/unity-brotli-decoder-vulnerability, accessed 2026-11-08. Unity lists October 2026 eMudhra and Brotli advisories; check the current fixed-version tables against the exact editor and deployed target before shipping. 6000.3.28f1 is running, but the advisory-specific result and security posture need confirmation; no general security guarantee is implied.

Unity's Brotli advisory, updated November 7, lists 6000.3.27f2 as the first fixed 6.3 LTS release; installed/running 6000.3.28f1 is later. Source: https://gw-prd.hexagon.unity.com/security/unity-brotli-decoder-vulnerability, accessed 2026-11-08. This finding addresses that advisory only; application exposure also depends on use of the affected APIs and data.

## 2026-11-08 — Simulated frame cadence

- Unity 6.3 documents `Time.captureFramerate` as the reciprocal of `Time.captureDeltaTime`. The lab test uses it to vary the frame time supplied to the engine while keeping physics at 60 Hz. This experiment measures simulation behavior across update cadences. Standalone frame times require separate measurement. https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/time/captureframerate

## 2026-11-09 — Resume references

- Rechecked https://unity.com/security, https://unity.com/security/unity-emudhra-compromise and https://gw-prd.hexagon.unity.com/security/unity-brotli-decoder-vulnerability. Both October 2026 advisory tables list 6000.3.27f2 as the first patched 6.3 LTS editor. The project's installed 6000.3.28f1 is later; retaining it is an advisory-specific inference, not a general security certification. No release-stream or package upgrade is needed for these findings.
- https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/application/persistentdatapath documents the Windows persistent location under LocalLow/company/product. Keep the product identity stable across player builds; isolate automated-test files from player records.
- https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/jsonutility/fromjson documents construction/default behavior and missing JSON fields. Proposed save implementation explicitly validates schema, metadata, required objects, collection bounds and numeric ranges after deserialization; successful parsing alone will not establish validity. Actual save tests remain pending at this entry.

## 2026-11-09 — Grab pose reference

- Snowboard Addiction identifies Indy as the rear hand on the toe edge between the bindings and Melon as the front hand on the heel edge. The segmented rig uses those hand and edge combinations. The current pose is an approximation with board-mounted boots. https://snowboardaddiction.com/blogs/training-education/basic-grabs-on-a-snowboard

## 2026-11-11 snowboard reference survey

Accessed official publisher/developer material and mapped it to `agent-context/01-GAME-IDEAS.md`. Sources: [EA SSX](https://www.ea.com/games/ssx) and [EA Race/Trick/Survive release](https://news.ea.com/press-releases/press-releases-details/2012/Defy-Reality-Own-the-Planet-EA-Sports-SSX-in-Stores-Now/default.aspx); [Ubisoft Steep overview](https://news.ubisoft.com/en-us/article/7jAdiKBdiUehSiZK3OWO2Z/steep-action-sports-go-open-world); [Shredders developer FAQ](https://i-illusions.com/faq/shredders) and [publisher page](https://www.microids.com/us/shredders-us/); [The Snowboard Game developer-authored Steam listing](https://store.steampowered.com/app/803830/The_Snowboard_Game/); [Riders Republic Q&A](https://www.ubisoft.com/en-sg/game/riders-republic/news-updates/2HFvoOtBvWRtrlXOF8ZXjo/answering-your-riders-republic-questions) and [accessibility spotlight](https://news.ubisoft.com/en-us/article/6zMpXnJAI44AWNqmm2N6LK/accessibility-spotlight-riders-republic). These sources document public-facing game patterns, not internal algorithms or tuning. Proposed Echo Mountain surface response is original and requires local testing.
