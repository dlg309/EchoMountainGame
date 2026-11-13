# EchoMountain3D implementation plan

The complete brief and acceptance gates are in `docs/BUILD_BRIEF.md`. The latest observed results are in the QA record.

- [x] **M0 Foundation:** repository root selected; Unity 6000.3.28f1 / URP imported and compiled; recovery records, ignore rules, and a Windows lab build entry point exist. The isolated Windows development build succeeded and launched. See QA for caveats.
- [ ] **M1 Physics lab** (after M0): construct an owned flat/slope/crest/jump/landing/reset test scene, rigidbody and multiple contact probes, force-limited edge and slip, braking, buffered pop, switch-aware landing and diagnostics. Verify support, acceleration, turning, braking, trajectory, landing and reset through Unity PlayMode tests and observed gameplay; record tolerances and results.
- [ ] **M2 Rider/input/camera:** keyboard and controller-ready semantic actions, settings, intermediate rider and camera; test pause/reset, a deliberate run and foot/board relationships. Record real hardware limitations.
- [ ] **M3 Vertical slice:** connected mountain with alternate lines; tricks, grabs, scoring, practice, HUD, menus/results, saves, sound, spray and replay. Verify cold launch through retry in a Windows build.
- [ ] **M4 Echo Sessions:** safe retry markers, comparable previous/best ghosts, feedback, replay controls, visual/audio polish and accessibility; verify persistence and integrity across restart.
- [ ] **M5 Expanded demo:** rails, physical halfpipe, surfaces, challenges, cosmetics, weather and authoring tools; validate each addition without regressing the slice.
- [ ] **M6 Release candidate:** regression suite, Windows launch, reproducible build, gameplay performance and QA, known limitations and handoff.

## Decisions and dependencies

The Unity project is on 6000.3.28f1 with URP. The current physics step is 60 Hz. The standalone target remains 1080p at 60 FPS, pending gameplay measurement.

M1 covers the center-line descent, switch landing, high-speed seam, and basic recovery in PlayMode. The current standalone input check is pending. M2 has named keyboard and gamepad actions, pause/reset, a travel-aware camera, and an intermediate articulated rider. Finish the manual controls, rider/camera review, and device behavior checks before expanding the course.

M3 is underway. Glacier Pass has two selectable lines, an 860 m connected surface, elapsed time and top speed, and a finish-to-retry flow. Both lines met the 60–90 second target in PlayMode. Tricks, scoring, audio, and replay remain open.

Glacier Pass now includes spins, grabs, landing-based scoring, a short practice segment, and riding audio.

2026-11-09: Local route/practice records, practice-goal status, sound and last-line preferences now persist through versioned recoverable files. Snapshot replay provides pause, seeking, speed and HUD hiding. The full 30-case Unity regression passed, including both routes' record banking, replay isolation and retry. Next: basic snow spray and the actual Windows acceptance flow. Standalone persistence/replay, M1/M2 hardware review, persistent ghosts and further progression remain open.


2026-11-12: Reproduced 32/32 stable baseline; camera obstruction and active-controller/focus-loss pause plus dynamic ride prompts passed the expanded 36/36 suite. Windows build verification underway. No milestone checkbox advanced on simulated hardware evidence. Next foundation gaps: real standalone interaction/camera review, sensitivity/deadzone/stance settings; then the current research site's surface response experiment and Echo Sessions.
