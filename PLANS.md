# EchoMountain3D implementation plan

The complete brief and acceptance gates are in `docs/BUILD_BRIEF.md`. No milestone has been accepted yet. Do not treat a checklist as evidence of working gameplay.

- [ ] **M0 Foundation:** locate the actual project and its instructions; preserve existing work; confirm editor version, URP, activation, compile/import, Windows target and reproducible entry points. Recovery documents and ignore rules are started. Blocked on project location.
- [ ] **M1 Physics lab** (after M0): construct an owned flat/slope/crest/jump/landing/reset test scene, rigidbody and multiple contact probes, force-limited edge and slip, braking, buffered pop, switch-aware landing and diagnostics. Verify support, acceleration, turning, braking, trajectory, landing and reset through Unity PlayMode tests and observed gameplay; record tolerances and results.
- [ ] **M2 Rider/input/camera:** keyboard and controller-ready semantic actions, settings, intermediate rider and camera; test pause/reset, a deliberate run and foot/board relationships. Record real hardware limitations.
- [ ] **M3 Vertical slice:** connected mountain with alternate lines; tricks, grabs, scoring, practice, HUD, menus/results, saves, sound, spray and replay. Verify cold launch through retry in a Windows build.
- [ ] **M4 Echo Sessions:** safe retry markers, comparable previous/best ghosts, feedback, replay controls, visual/audio polish and accessibility; verify persistence and integrity across restart.
- [ ] **M5 Expanded demo:** rails, physical halfpipe, surfaces, challenges, cosmetics, weather and authoring tools; validate each addition without regressing the slice.
- [ ] **M6 Release candidate:** regression suite, Windows launch, reproducible build, gameplay performance and QA, known limitations and handoff.

## Decisions and dependencies

Project discovery precedes all Unity project mutations. Unity 6000.3.28f1 is installed and is beyond the first fixed 6.3 version for Unity's October 2026 eMudhra advisory, but actual project version, import and activation remain unknown. The open 6000.6 editor is running a separate setup tutorial. Do not silently migrate it or change streams. Physics and scene decisions are proposals until tested. Initial target: fixed 60 Hz physics and 1080p/60 FPS, measured in a standalone gameplay run; neither is established yet.

Current update: the user chose the repository root as the project location; a URP starter template is present, but editor import is blocked by missing Unity license activation. M0 remains incomplete and M1 is unverified.
