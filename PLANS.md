# EchoMountain3D implementation plan

The current MVP audit, risk-ranked slices, and proposed surface-zone implementation are in `docs/PLAN.md` (2026-11-17 review draft). This historical milestone log remains as provenance; no gameplay work is authorized by the review draft.

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


2026-11-13: Opt-in surface classifier and supported grip/roll/braking integration passed 38/38 full tests, then 2/2 expanded focused tests. Existing course remains packed. Next surface gate: visible authored zones, transition tests and versioned comparable records; do not activate invisible zones.

2026-11-18: The user prioritized controls, bail logic, an in-game menu, and visual polish before surface zones. An isolated Unity copy passed 41/41 PlayMode tests, and a fresh polished Windows r2 build succeeded. Steering/landing tuning, a pause return-to-menu flow, and an independent polished scene are implemented. Manual feel, visual and pause-button review remain open; user-reported record persistence after reopen remains unresolved. Surface zones are still disabled. See `docs/QA.md` and `docs/STATUS.md`.

2026-11-23. Sunset Basin adds a 600 × 980 m Free Roam map, three drop-ins, and landing-based rail scoring within M5. The expanded demo milestone still includes separate halfpipe, surface, and challenge work. The sandbox riding and frame-time checklist is in `README.md`.

## Riding and park update

- Grounded deck and boots should follow the actual support plane. Check flat snow, slopes, edge changes, and a jump release so the board separates naturally.
- Use the playable Snowboard Starter Kit to judge turn onset and edge changes. Measure Echo Mountain's first turn and travel direction at low and high speeds against the previous Echo tuning. Keep grip bounded and brake, jump, and landing behavior usable.
- Add keyboard and controller inputs for frontflips and backflips in the air. Track the rotation the board actually completes and award completed flips on valid landings.
- Give the Sunset Basin box and rails solid colliders on their visible surfaces. Check a top-down entry, a missed approach, a ride-off and a pop-off, and score only after landing on snow.
- Remove opaque riding HUD backplates and use high-contrast text that stays readable over snow and sky. Keep menu and pause controls readable.
- Run PlayMode checks in Unity 6.3 LTS and verify Pine, Summit, Jump Practice, and Free Roam. Record reference observations, measured results, and any manual checks still open in `docs/QA.md`.

## Linked turns

Measure slow and fast edge changes on High Traverse. Track turn timing, speed retention, rider lean, and camera heading against the previous build. Keep skids and braking effective. Verify the roller jump, flips, grind entries and exits, and all existing modes. Use a separate record book when handling changes.
