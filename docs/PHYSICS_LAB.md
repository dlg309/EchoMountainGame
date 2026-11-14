# Physics lab: proposed first verification matrix

This is a test design, not an implementation or a test result. Tune dimensions and tolerances after the actual project is found, while keeping a reproducible, owned scene separate from user scenes. Baseline proposal: 1 unit = 1 metre, gravity from project settings, 1/60 s fixed timestep, interpolated primary body, fixed initial pose and velocity, and explicit physics inputs. Preserve a flat control area followed by an independently testable slope, crest, takeoff and landing, with boundary/reset and visible developer diagnostics.

| Experiment | Compare / observe | Failure to investigate |
| --- | --- | --- |
| Resting support | settle on flat; measure vertical drift, speed, normal and contact state over several seconds | falling through, persistent jitter, NaNs or ground-state chatter |
| Slope acceleration | released board versus flat control; same setup and duration | no meaningful downhill acceleration or invented constant speed |
| Edge and brake | equal initial speeds, zero/left/right input and brake | indistinguishable left/right paths, gain in energy, unbounded grip or brake increasing speed |
| Crest and jump | compare no-pop and buffered pop from same approach; measure actual height/time, takeoff and landing | sticky lip, fixed airtime, repeated jump, phantom landing |
| Landing | compare forward and switch alignment, lateral skid and excessive impact/rotation | valid switch rejected, duplicate landing events, invalid landing accepted |
| Reset and boundary | repeated resets from moving, airborne and out-of-bounds states | residual velocity/input/contact, repeated collision, invalid state |
| Frame rates and seams | same fixed-tick inputs at 30, 60, 120 render FPS; repeat seam and high-speed cases | divergent fixed-step outcome outside declared tolerances, tunneling or contact chatter |

Use PlayMode tests for quantitative state and integration and observe actual gameplay separately. Establish numerical thresholds against measured baseline before asserting a pass; never change thresholds solely to hide a failure. Verify physical contact rather than trusting only probe state. Report input injection and simulation conditions, actual test counts/results and limitations.

## Unverified implementation in progress

The `Assets/EchoMountain` scripts are a first diagnostic prototype, not an accepted riding controller. After activation and successful import, use **Echo Mountain > Create physics lab (once)**. It refuses to overwrite an existing lab and prompts to save modified scenes. Inspect and repair the generated geometry, contact behavior, scene references and control response in Play Mode before calling any acceptance gate complete. The scene is not yet generated or added to build settings. Input System package resolution, scripts, and 60 Hz configuration are not yet verified.

A first PlayMode test draft checks flat support and that reset restores spawn and clears linear/angular velocity. It has not been imported, compiled, or executed; its thresholds and test assembly configuration require validation in the installed editor. It does not establish turning, braking, jump, landing, frame-rate or controller acceptance.

## Proposed lab entry points (not yet run)

Once the project imports successfully, use **Echo Mountain > Create physics lab (once)**, inspect the scene, then **Echo Mountain > Build Windows physics lab**. The draft builder requires the lab scene and refuses to overwrite an existing `Builds/PhysicsLab` directory. A successful build operation alone is not a gameplay or performance verification. For command-line tests, close the editor on this project first and use Unity Test Framework 1.4's `-runTests -testPlatform PlayMode -testResults <absolute XML path> -logFile <absolute log path>` with `-batchmode -projectPath <project root>`; inspect the exit code, XML and log. These entry points and flags have not been exercised in this project.

## 2026-11-08 partial validation

Unity generated this scene from the editor tool in a separate project copy; it was transferred without replacing an existing scene. The only PlayMode test so far passed in the copy and covers flat support/reset. Slope behavior, geometry seams, rider motion, landing and camera remain unobserved. The fixed timestep remains the template default unless changed and tested; 60 Hz is a proposal, not an established setting.

## First reported gameplay failure and repair

A user observed a stationary board and nonworking Space/brake in the initial Windows build; reset worked. The isolated slope test reproduced essentially no movement before repair. An isolated candidate that sets board friction to zero while applying explicit script forces and filters self hits in probes passed flat support/reset, slope acceleration, and direct supported pop (3/3 PlayMode). This is not yet evidence that keyboard input, braking, camera readability, or gameplay works; the original player build predates the repair.


## Surface experiment (2026-11-13, fixture-verified)

`SnowSurfaceMap` is opt-in. Add the component to an owned lab object and assign it to the rider's serialized `surfaceMap` field. Its zone rectangles use world X/Z, minimum-inclusive/maximum-exclusive bounds; higher priority wins, first serialized zone breaks ties. Empty, invalid or unassigned maps use packed snow. Existing lab/course scenes remain unchanged.

Prototype grip/roll/brake ratios: packed 1/1/1, powder 0.85/1.8/1.25, ice 0.55/0.45/0.5. Only supported ticks apply them. `ActiveSurface` retains the last display kind in flight and clears on reset. These are tuning assumptions from the research blueprint, not real material measurements. Before enabling zones on Glacier Pass, author matching visible patches, validate both routes, and version records so changed physics cannot compete with old course times. No surface art/audio or changed course records are delivered yet.
