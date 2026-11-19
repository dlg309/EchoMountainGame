# EchoMountain3D implementation plan

The active user-approved Snowboard Starter redesign plan is below. `docs/PLAN.md` is an older MVP/surface-zone review draft and does not set the current priority.

The original brief is in `docs/BUILD_BRIEF.md`; this user directive updates its visual and physics target. The latest observed results are in `docs/QA.md` and `docs/STATUS.md`.

## Active redesign plan (2026-11-19)

**Goal:** Make Echo Mountain's movement, physics, animations, camera, rider/board, mountain art, menu and HUD closely match the user's Snowboard Starter reference screenshots and playable Windows build, while retaining Echo Mountain's name and functional Pine/Summit, Jump practice, scoring, pause/retry, records and replay. The user dislikes the existing rider movement and explicitly authorizes replacing its implementation and art direction. This plan supersedes the surface-zone next step and the historical milestone sequence below. Keep Unity 6000.3.28f1 and URP unless a separately verified patch is necessary. The starter is a compiled Unity 2020.2.1 build, not editable source.

- [ ] **R0 Baseline and reference:** checkpoint current Git/editor state; read latest QA/status; record the R2 manual checks and unresolved reopen persistence. Play starter and Echo Mountain with matched input/slope/camera cases; inventory assembly classes, scene/assets, and current dependencies. Make a visual/behavior specification with the three screenshots, measurements, observed/inferred labels, and acceptance thresholds.
- [ ] **R1 Movement replacement:** implement a new supported rider controller/physics with measured acceleration, edge/carve/skid, braking, pop, air control, landing/bail and rail behavior. Replace old `LabRider` assumptions as needed. Validate in a dedicated lab and both authored routes with focused PlayMode and manual Windows checks.
- [ ] **R2 Character and camera:** rebuild the rider silhouette, board attachment, animation states, grabs/bails and dynamic behind-rider camera. Compare matched turns, jumps and obstacle approaches in motion; check visibility and camera obstruction.
- [ ] **R3 World and visuals:** rebuild terrain presentation, snow, tiered firs, distant blue-gray mountains, golden sky, sunset lighting, outlines, ramps/rails/signs, shadow, trail/spray. Compare the menu and downhill screenshots at matching views and quality settings.
- [ ] **R4 UI/audio/integration:** redesign character/course preview menu, GO/navigation/quality controls, minimal readable HUD and ride sounds; reconnect Pine/Summit, Jump practice, score, pause/retry, records and replay to the new rider. Version records if motion/scoring rules change. Verify saves across a real standalone restart.
- [ ] **R5 Acceptance and cleanup:** full Unity regression, fresh Windows build and manual side-by-side play. Log remaining differences and performance. Only after acceptance, audit scene/prefab GUIDs, Build Settings, Resources and runtime references; remove unused legacy code/assets/scenes in a separate commit and rerun the same gates.

**Acceptance:** The player can select either line or practice, steer/carve/brake/pop/spin/grab/land/bail/ride a rail, finish and retry; scores and replay remain isolated and records survive restart. Matched reference captures demonstrate the specified look and camera, and manual play compares handling. Compilation or automated tests alone do not prove visual or feel parity. Do not label an unrun check passed. Keep reference files local and extracted third-party assets out of the public repository.

## Historical milestone log

- [x] **M0 Foundation:** repository root selected; Unity 6000.3.28f1 / URP imported and compiled; recovery records, ignore rules, and a Windows lab build entry point exist. The isolated Windows development build succeeded and launched. See QA for caveats.
- [ ] **M1 Physics lab** (after M0): construct an owned flat/slope/crest/jump/landing/reset test scene, rigidbody and multiple contact probes, force-limited edge and slip, braking, buffered pop, switch-aware landing and diagnostics. Verify support, acceleration, turning, braking, trajectory, landing and reset through Unity PlayMode tests and observed gameplay; record tolerances and results.
- [ ] **M2 Rider/input/camera:** keyboard and controller-ready semantic actions, settings, intermediate rider and camera; test pause/reset, a deliberate run and foot/board relationships. Record real hardware limitations.
- [ ] **M3 Vertical slice:** connected mountain with alternate lines; tricks, grabs, scoring, practice, HUD, menus/results, saves, sound, spray and replay. Verify cold launch through retry in a Windows build.
- [ ] **M4 Echo Sessions:** safe retry markers, comparable previous/best ghosts, feedback, replay controls, visual/audio polish and accessibility; verify persistence and integrity across restart.
- [ ] **M5 Expanded demo:** rails, physical halfpipe, surfaces, challenges, cosmetics, weather and authoring tools; validate each addition without regressing the slice.
- [ ] **M6 Release candidate:** regression suite, Windows launch, reproducible build, gameplay performance and QA, known limitations and handoff.

## Historical decisions and dependencies

The Unity project is on 6000.3.28f1 with URP. The current physics step is 60 Hz. The standalone target remains 1080p at 60 FPS, pending gameplay measurement.

M1 covers the center-line descent, switch landing, high-speed seam, and basic recovery in PlayMode. The current standalone input check is pending. M2 has named keyboard and gamepad actions, pause/reset, a travel-aware camera, and an intermediate articulated rider. Finish the manual controls, rider/camera review, and device behavior checks before expanding the course.

M3 is underway. Glacier Pass has two selectable lines, an 860 m connected surface, elapsed time and top speed, and a finish-to-retry flow. Both lines met the 60–90 second target in PlayMode. Tricks, scoring, audio, and replay remain open.

Glacier Pass now includes spins, grabs, landing-based scoring, a short practice segment, and riding audio.

2026-11-09: Local route/practice records, practice-goal status, sound and last-line preferences now persist through versioned recoverable files. Snapshot replay provides pause, seeking, speed and HUD hiding. The full 30-case Unity regression passed, including both routes' record banking, replay isolation and retry. Next: basic snow spray and the actual Windows acceptance flow. Standalone persistence/replay, M1/M2 hardware review, persistent ghosts and further progression remain open.


2026-11-12: Reproduced 32/32 stable baseline; camera obstruction and active-controller/focus-loss pause plus dynamic ride prompts passed the expanded 36/36 suite. Windows build verification underway. No milestone checkbox advanced on simulated hardware evidence. Next foundation gaps: real standalone interaction/camera review, sensitivity/deadzone/stance settings; then the current research site's surface response experiment and Echo Sessions.


2026-11-13: Opt-in surface classifier and supported grip/roll/braking integration passed 38/38 full tests, then 2/2 expanded focused tests. Existing course remains packed. Next surface gate: visible authored zones, transition tests and versioned comparable records; do not activate invisible zones.

2026-11-18: The user prioritized controls, bail logic, an in-game menu, and visual polish before surface zones. An isolated Unity copy passed 41/41 PlayMode tests, and a fresh polished Windows r2 build succeeded. Steering/landing tuning, a pause return-to-menu flow, and an independent polished scene are implemented. Manual feel, visual and pause-button review remain open; user-reported record persistence after reopen remains unresolved. Surface zones are still disabled. See `docs/QA.md` and `docs/STATUS.md`.
