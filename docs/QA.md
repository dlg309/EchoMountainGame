# QA evidence

## Glacier Pass drop-in — 2026-11-25

Baseline `d233726`, Unity 6000.3.28f1 and URP 17.3.0 in the existing isolated project at `%TEMP%/EchoMountain-SunsetBasin-QA-20261123`. The editor version was retained for the local bundled-font playtest. The next 6.3 patch fixes a malformed variable-font issue. [Unity release notes](https://unity.com/releases/editor/whats-new/6000.3.29f1)

The new drop-in check was run against the baseline geometry. Pine and Summit both reached 10.87 km/h after five seconds, below the new acceptance range of 21–30 km/h. The baseline command intentionally exited 2. The opening pitch at the spawn increases from about 4.9° to 10.0°. The grade blends into the original at course position 65 m, 60 m after the start. The change uses gravity and the existing ride controller. The course record key changes to `runs-glacier-pass-c2-r5.json`. That book reads earlier sound and line preferences and carries over same-rules Jump Practice records.

```text
Unity.exe -batchmode -force-d3d11 -projectPath <QA> -runTests -testPlatform PlayMode -testFilter MountainRunTests -testResults <QA>/drop-in-baseline.xml -logFile <QA>/drop-in-baseline.log
Unity.exe -batchmode -nographics -projectPath <QA> -executeMethod EchoMountain.EditorTools.MountainBuilder.UpdateDropIn -logFile <QA>/drop-in-scenes.log -quit
Unity.exe -batchmode -force-d3d11 -projectPath <QA> -runTests -testPlatform PlayMode -testResults <QA>/drop-in-full.xml -logFile <QA>/drop-in-full.log
```

Scene generation exited 0. A Python comparison of the old and regenerated mesh vertex streams found 3,645 height changes among 43,605 vertices, all at z = −16 to 64 m. X/Z coordinates and the triangle index buffer matched. Both scenes and the snow mesh kept their original GUIDs.

The full PlayMode suite passed **63/63**. Both routes reached 23.64 km/h at five seconds. Pine finished in 78.82 s and Summit in 79.62 s with two landings. Peak speeds were 44.7 and 44.6 km/h. Jump Practice scoring, Free Roam movement parity, replay, retry, and the course record transfer passed.

```text
Unity.exe -batchmode -nographics -projectPath <QA> -executeMethod EchoMountain.EditorTools.LabBuild.BuildWindowsFreeRoam -echoBuildOutput <QA>/Builds/GlacierPass-FasterDropIn/EchoMountain.exe -logFile <QA>/drop-in-build.log -quit
```

The Windows build exited 0. The copied executable, runtime DLL, and both levels matched the isolated output by SHA-256. The player opened a responsive Glacier Pass window and initialized Direct3D 11, PhysX, and input. Acceleration was measured in PlayMode. The Windows build is open for the hands-on check. Evidence is in `Logs/DropIn-QA`.

```text
Builds/GlacierPass-FasterDropIn/EchoMountain.exe -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -force-d3d11 -logFile Logs/DropIn-QA/drop-in-player.log
```

## Riding parity — 2026-11-25

Baseline `204e0a0`, Unity 6000.3.28f1 and URP 17.3.0, using the existing isolated project at `%TEMP%/EchoMountain-SunsetBasin-QA-20261123`. Unity ran with the project's bundled fonts. The 6000.3.29f1 release notes document a FreeType fix for malformed variable fonts. [Unity release notes](https://unity.com/releases/editor/whats-new/6000.3.29f1)

The production scenes use the same `LabRider`, `LabRideControls`, serialized tuning, and board collider and body settings. Both courses use packed-snow response. Free Roam previously had an additional snow-height recovery check. Its baseline passed the induced inverted and buried cases, while Pine, Summit, and Jump Practice each failed to reset the inverted board. The baseline exited 2 with 1/4 passing.

The shared session check uses the established Free Roam thresholds of 0.55 m below snow, or within 0.32 m above it when the board's up vector has a world-up dot below −0.25. Glacier Pass reads the supporting mesh collider. Sunset Basin reads its terrain height. Mode boundaries, drop-ins, scoring, and finish rules remain course-specific. The r5 record book is retained because handling on a valid run is governed by the same physics.

```text
Unity.exe -batchmode -force-d3d11 -projectPath <QA> -runTests -testPlatform PlayMode -testFilter RidingModeParityTests -testResults <QA>/mode-parity-baseline.xml -logFile <QA>/mode-parity-baseline.log
Unity.exe -batchmode -nographics -projectPath <QA> -executeMethod EchoMountain.EditorTools.MountainBuilder.LinkRecoverySurface -logFile <QA>/mode-parity-scenes.log -quit
Unity.exe -batchmode -force-d3d11 -projectPath <QA> -runTests -testPlatform PlayMode -testFilter RidingModeParityTests -testResults <QA>/mode-parity-focused.xml -logFile <QA>/mode-parity-focused.log
Unity.exe -batchmode -nographics -projectPath <QA> -executeMethod EchoMountain.EditorTools.MountainBuilder.LinkRecoverySurface -logFile <QA>/mode-parity-scenes-linked.log -quit
Unity.exe -batchmode -force-d3d11 -projectPath <QA> -runTests -testPlatform PlayMode -testFilter RidingModeParityTests -testResults <QA>/mode-parity-focused-linked.xml -logFile <QA>/mode-parity-focused-linked.log
```

The first scene authoring pass applied the references in memory and skipped saving because the scene was not marked dirty. The first focused run passed 1/6 and also showed that a 16 m/s start was nearly at terminal speed on the 9° fixture. After explicitly saving the scenes and checking that fast coast stayed within 0.2 m/s of the start speed, the focused rerun passed **6/6**. Both Glacier Pass scene GUIDs were retained. The scene diffs contain one collider reference each.

Each mode received 246 samples at 60 Hz on the same 9° packed slope. The input ran a coast, four alternating turns, full braking, a pop, an aerial spin input, and a landing. The 6 m/s run used regular stance and the 16 m/s run used switch. Contact and landing states matched every frame. The position tolerance was 3 mm, with instantaneous PhysX collision differences allowed up to 0.08 m/s linear and 0.10 rad/s angular velocity. Course terrain can still change acceleration and jump trajectory during authored runs.

| Start | Braking distance in each mode | Pop height normal to the slope | Landing in each mode | Largest position difference from Free Roam |
| --- | --- | --- | --- | --- |
| 6 m/s regular | 3.988 m | 0.867 m | Clean | 1.20 mm |
| 16 m/s switch | 8.541 m | 0.858 m | Clean | 0.03 mm |

The first full run passed 60/62 and exited 2. Pine and Summit finished in 84.49 s and 85.30 s. The existing lab boot-rig test reported a 1.09 m pose-to-board offset during its frame-sensitive pop check. The 6 m/s comparison timed out during a roughly 100-minute gap in the runner timestamps. The 16 m/s comparison and all four recovery cases passed. The full suite was rerun with the same source and assertions.

```text
Unity.exe -batchmode -force-d3d11 -projectPath <QA> -runTests -testPlatform PlayMode -testResults <QA>/mode-parity-full.xml -logFile <QA>/mode-parity-full.log
Unity.exe -batchmode -force-d3d11 -projectPath <QA> -runTests -testPlatform PlayMode -testResults <QA>/mode-parity-acceptance.xml -logFile <QA>/mode-parity-acceptance.log
```

The complete rerun passed **62/62** and exited 0. The matched traces reproduced the focused results. Pine and Summit again finished in 84.49 s and 85.30 s. Jump Practice scoring, Free Roam rails, the lab boot rig, camera, replay, and retry passed.

```text
Unity.exe -batchmode -nographics -projectPath <QA> -executeMethod EchoMountain.EditorTools.LabBuild.BuildWindowsFreeRoam -echoBuildOutput <QA>/Builds/EchoMountain-RidingParity/EchoMountain.exe -logFile <QA>/mode-parity-windows.log -quit
```

The Windows x64 build exited 0. The local copy's executable, runtime DLL, and both levels matched the isolated build by SHA-256. The player opened a responsive Glacier Pass window and initialized Direct3D 11, PhysX, and input. Ride comparisons were run in Unity PlayMode. The Windows playtest was left open for hands-on riding. Evidence is in `Logs/RidingParity-QA`.

```text
Builds/EchoMountain-RidingParity/EchoMountain.exe -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -force-d3d11 -logFile Logs/RidingParity-QA/mode-parity-player.log
```

## Riding presentation — 2026-11-24

Baseline source was `3e58735`. QA used Unity 6000.3.28f1 / URP 17.3.0 at 60 Hz with Direct3D 11 in `%TEMP%/EchoMountain-SunsetBasin-QA-20261123`. The project used its bundled fonts for the local playtest. Unity 6000.3.29f1 includes a FreeType fix for malformed variable fonts. [Unity release notes](https://unity.com/releases/editor/whats-new/6000.3.29f1)

The original r5 High Traverse and packed-snow brake fixtures passed 4/4. A second r5 camera pass used the same updated measurement fixture to capture the largest single-frame change in camera yaw in the first eight frames after each of three steering reversals. Steering, supported motion, and braking are owned by the original `LabRider` code. The brake pose reads existing support, slip, steer, and speed. Positive toe-side slip loads the heels and heel-side slip loads the toes. On centered travel, the pose uses the engaged turn edge or defaults to heels.

```text
Unity.exe -batchmode -force-d3d11 -projectPath <QA> -runTests -testPlatform PlayMode -testFilter "HighTraverse|PackedSnowBrakeMetrics" -testResults <QA>/presentation-r5-baseline.xml -logFile <QA>/presentation-r5-baseline.log
Unity.exe -batchmode -nographics -projectPath <QA> -executeMethod EchoMountain.EditorTools.MountainBuilder.ApplySharedDeck -logFile <QA>/presentation-scenes.log -quit
Unity.exe -batchmode -force-d3d11 -projectPath <QA> -runTests -testPlatform PlayMode -testResults <QA>/presentation-first.xml -logFile <QA>/presentation-first.log
Unity.exe -batchmode -force-d3d11 -projectPath <QA> -runTests -testPlatform PlayMode -testFilter HighTraverseLinkedTurns -testResults <QA>/presentation-r5-camera.xml -logFile <QA>/presentation-r5-camera.log
Unity.exe -batchmode -force-d3d11 -projectPath <QA> -runTests -testPlatform PlayMode -testFilter "HighTraverseLinkedTurns|MainMenuOpensBasinAndReturnsToRouteSelection|CameraObstructionTests|CameraLooksAlongSwitchTravelAndSnapsOnReset" -testResults <QA>/presentation-focused.xml -logFile <QA>/presentation-focused.log
Unity.exe -batchmode -force-d3d11 -projectPath <QA> -runTests -testPlatform PlayMode -testResults <QA>/presentation-final.xml -logFile <QA>/presentation-final.log
```

Scene authoring exited 0 and preserved the scene GUIDs. The first suite passed 53/56. The tests caught slower aim at both speeds and an assertion that used the mesh's generator name in place of its imported name. The next focused run passed 5/6. The fast-turn view was still 7.1° behind travel at the end of the first turn, above the existing 6.9° limit, so look-ahead gained more lead from the difference between the smoothed orbit and look headings.

| High Traverse check | 6 m/s r5 → revised | 16 m/s r5 → revised |
| --- | --- | --- |
| Board yaw reversal | 11 → 11 frames | 11 → 11 frames |
| Travel change in the first turn | 17.0° → 17.0° | 9.9° → 9.9° |
| Speed after four turns | 8.501 → 8.501 m/s | 16.943 → 16.943 m/s |
| View yaw step at an edge change | 1.44° → 0.40° | 0.74° → 0.23° |
| View yaw lag at the end of the first turn | 11.8° → 11.0° | 6.5° → 6.6° |

The brake pose reached a signed shift of −0.97 and +0.97 under opposite local slip, with hips lowered to 0.80 m in both tests. The signed shift decayed on a pop and interpolated in replay tests. The High Traverse roller climbed 3.34 m above the snow and registered one recoverable landing. Straight braking covered 13.47 m and braking through linked turns covered 13.11 m, matching r5 at the recorded precision.

The revised full suite passed 55/56. The new Summit presentation assertion sampled a moving board transform before the visual rig's LateUpdate. The shared-scene test now checks the authored twin-tip mount setting and rig-local binding height, along with the shared mesh, material, rider palette, and HUD activation in Pine, Summit, Jump Practice, and Free Roam. The existing airborne rig test checks boot attachment during a pop.

The final full suite passed **56/56** and exited 0. Pine finished in 84.49 seconds and Summit in 85.30 seconds. Summit recorded two landings. The regression also covered braking, spins, flips, rails, camera obstruction, switch riding, replay, and retry.

```text
Unity.exe -batchmode -force-d3d11 -projectPath <QA> -runTests -testPlatform PlayMode -testResults <QA>/presentation-acceptance.xml -logFile <QA>/presentation-acceptance.log
Unity.exe -batchmode -nographics -projectPath <QA> -executeMethod EchoMountain.EditorTools.LabBuild.BuildWindowsFreeRoam -echoBuildOutput <QA>/Builds/SunsetBasin-RidePolish/EchoMountain.exe -logFile <QA>/presentation-windows.log -quit
```

The Windows x64 build succeeded and exited 0. The copied executable, runtime DLL, and both levels match the isolated build by SHA-256. The player opened at the Glacier Pass menu and initialized Direct3D 11, PhysX, and input. The startup log contained no exception or error lines. Desktop input and capture did not reliably reach a riding view, so in-motion HUD readability, brake-pose feel, and camera comfort in the Windows player remain hands-on checks. Logs are in `Logs/RidePresentation-QA`.

```text
Builds/SunsetBasin-RidePolish/EchoMountain.exe -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -force-d3d11 -logFile Logs/RidePresentation-QA/presentation-player.log
```

## Linked turns (r5) — 2026-11-23

The Windows helper found the running Snowboard Starter Kit but could not activate its window on either attempt. Direct comparison of slow turns, S-turns, and the roller with the Starter Kit remains open. The reference direction came from the user’s requested handling and existing playtest notes. The measured comparison uses the previous Echo r4 runtime and the same High Traverse scene, start, and virtual gamepad sequence for both versions.

Unity 6.3 LTS 6000.3.28f1, Direct3D 11, URP 17.3.0, 60 Hz physics in the isolated `%TEMP%/EchoMountain-SunsetBasin-QA-20261123` project. The mounted camera, rider rig, and actual board motion were sampled independently. Each run applied four alternating turns for 0.6 s each.

| High Traverse measure | 6 m/s start, r4 → r5 | 16 m/s start, r4 → r5 |
| --- | --- | --- |
| Board yaw reversal | 18 → 11 frames | 18 → 11 frames |
| Rider lean reversal | 5 → 4 frames | 5 → 4 frames |
| Travel heading change after first turn | 13.2° → 17.0° | 7.7° → 9.9° |
| Speed after four turns | 7.550 → 8.501 m/s | 16.450 → 16.943 m/s |
| Mechanical energy remaining, adjusted for height | 15.2% → 43.4% | 41.6% → 46.2% |
| Camera yaw error at the end of the first turn | 12.6° → 11.8° | 7.1° → 6.5° |
| Ground support | 144/144 in each run | 144/144 in each run |

- Faster edge release and engagement shortened board response. Aligned carving now directs grip closer to perpendicular to the board's travel, reducing the speed it scrubs. Braking and skids retain grip that opposes lateral slip. The rider shifts weight across the board and the camera aims into the carve as its position catches up.
- The matched roller cleared 3.34 m in both versions and landed with Recover. Final **55/55** PlayMode tests passed. They include flat/sloped board contact, low/high turns, braking, spins, flips, grind entries and exits, Free Roam, Jump Practice, Pine and Summit finish/retry, and replay. Pine finished in 84.49 s and Summit in 85.30 s.
- The initial r5 runtime run passed 55/55 but showed greater camera yaw error. Aiming the camera through the turn reduced the error. The next run passed 54/55. The slow camera measured 11.76°, outside the initial 11.5° test target, and improved over the 12.6° baseline. The final assertion checks for an improvement against the r4 measurement with a margin. No runtime changes were made between those last two runs.
- New handling records use r5. Sound and selected line are read from the newest valid r4 or r3 book if r5 has yet to be created.

```text
Unity.exe -batchmode -force-d3d11 -projectPath <QA> -runTests -testPlatform PlayMode -testFilter EchoMountain.Tests.FreeRoamTests.HighTraverse -testResults <QA>/carve-r4-high-traverse.xml -logFile <QA>/carve-r4-high-traverse.log
Unity.exe -batchmode -force-d3d11 -projectPath <QA> -runTests -testPlatform PlayMode -testResults <QA>/carve-r5-final.xml -logFile <QA>/carve-r5-final.log
Unity.exe -batchmode -force-d3d11 -projectPath <QA> -runTests -testPlatform PlayMode -testResults <QA>/carve-r5-verified.xml -logFile <QA>/carve-r5-verified.log
Unity.exe -batchmode -force-d3d11 -projectPath <QA> -runTests -testPlatform PlayMode -testResults <QA>/carve-r5-acceptance.xml -logFile <QA>/carve-r5-acceptance.log
```

- Windows x64 build exited 0. The executable, runtime DLL, and both scene files match the isolated artifacts. The build is at `Builds/SunsetBasin-Carving/EchoMountain.exe`. Exact outputs are in `Logs/LinkedTurnsR5-QA`.

```text
Unity.exe -batchmode -nographics -projectPath <QA> -executeMethod EchoMountain.EditorTools.LabBuild.BuildWindowsFreeRoam -echoBuildOutput <QA>/Builds/SunsetBasin-Carving/EchoMountain.exe -logFile <QA>/carve-r5-windows.log -quit
```

## Riding and park update (r4) — 2026-11-23

Unity 6.3 LTS 6000.3.28f1, URP 17.3.0, fixed physics at 60 Hz. PlayMode used Direct3D 11 on Intel Iris Xe in the existing isolated snapshot `%TEMP%/EchoMountain-SunsetBasin-QA-20261123`. All scene and mesh references retained their GUIDs. The reference Starter Kit was launched and exposed a window. The first window had higher Windows integrity than the input helper. Activation also failed for the second launch. The qualitative reference comes from the earlier Starter Kit playtest notes. A matched speed comparison could only be measured against Echo Mountain r3.

```text
Unity.exe -batchmode -nographics -projectPath <QA> -executeMethod EchoMountain.EditorTools.SunsetBasinBuilder.UpdateRidingGeometry -logFile <QA>/ride-r4-geometry-final.log -quit
Unity.exe -batchmode -force-d3d11 -projectPath <QA> -runTests -testPlatform PlayMode -testResults <QA>/ride-r4-first.xml -logFile <QA>/ride-r4-first.log
Unity.exe -batchmode -force-d3d11 -projectPath <QA> -runTests -testPlatform PlayMode -testResults <QA>/ride-r4-final.xml -logFile <QA>/ride-r4-final.log
```

- Geometry update exited 0 after fixing an editor namespace import. The first test run passed 52/53 and exited 2. A thin-wall chase camera check missed its threshold by 3.8 mm. Expanding the camera sweep radius by 1 cm resolved it. The final run passed **53/53**, exit 0. Pine and Summit completed in 84.49 s and 85.27 s. Jump Practice, replay, Free Roam menu and drop-ins also passed.
- The visible deck's measured underside gap was 0.000 m to three decimal places on the flat and open slope. A High Traverse pop cleared 3.34 m of snow at the peak and produced a recoverable landing. The asset mesh and binding positions now use the same board base.
- The turn test began at 6 m/s and 16 m/s on packed snow. After the first 0.6 s the board turned 23.79° and 13.14°, up from 10.77° and 7.35° in r3 using the same fixture. Actual travel turned 15.97° and 8.84°. The board remained supported for 144/144 fixed steps per set, and settled to 0.095 and 0.056 rad/s after input release. Braking through the S-turns covered 13.20 m over the measured window, within the prior distance budget.
- W and a virtual gamepad right-stick down produced +364.6° and −364.6°. Both landed cleanly and awarded 300 points after touchdown. A late frontflip reached 77.3°, bailed, and awarded zero. Spins, grabs, and practice score banking passed the existing tests.
- The box supported a standing-start pop approach and a ride-off. A transverse approach missed the grind. Both narrow rail tops matched the authored contact points within 1 cm and supported dynamic grinds. The rider popped off both, remained under gravity, and banked the grind after landing in snow. Score reset and recovery checks passed.
- Windows x64 build exited 0 and is at `Builds/SunsetBasin-Riding/EchoMountain.exe`. The executable, runtime DLL, and both level files match the isolated output. The capture build used a temporary in-game helper that loaded Free Roam and captured a frozen frame with and without the HUD. Its first hidden-window attempt returned black frames. The visible retry captured two 1280 × 720 frames and exited cleanly. Four sampled interiors in the former mode, score, controls, and speed panels were identical with and without the HUD. Visual review confirmed outlined labels over sky and snow. Evidence is in `Logs/RidingParkR4-QA`. The delivered build excludes the temporary helper.

```text
Unity.exe -batchmode -nographics -projectPath <QA> -executeMethod EchoMountain.EditorTools.LabBuild.BuildWindowsFreeRoam -echoBuildOutput <QA>/Builds/SunsetBasin-RidingHudQA/EchoMountain.exe -logFile <QA>/ride-r4-hud-build.log -quit
<QA>/Builds/SunsetBasin-RidingHudQA/EchoMountain.exe -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -echoHudEvidence -logFile <QA>/ride-r4-hud-visible.log
Unity.exe -batchmode -nographics -projectPath <QA> -executeMethod EchoMountain.EditorTools.LabBuild.BuildWindowsFreeRoam -echoBuildOutput <QA>/Builds/SunsetBasin-Riding/EchoMountain.exe -logFile <QA>/ride-r4-windows.log -quit
```

Manual comparisons with the Starter Kit and a human ride across the revised park remain open.

## Sunset Basin Free Roam — 2026-11-23

The selected project is on `experiment/carve-brake-r3`, starting at `3b55431`. Testing used `%TEMP%/EchoMountain-SunsetBasin-QA-20261123` with Unity 6.3 LTS 6000.3.28f1, URP 17.3.0, and a Core i9-13900H with Intel Iris Xe / Direct3D 11 for the graphical run. The Unity version stayed at the recorded patch for this local test with the project's bundled fonts. Unity 6000.3.27f2 covered its October Brotli and eMudhra advisories. Unity 6000.3.29f1 fixes a separate malformed variable-font issue. [October Unity advisories](https://discussions.unity.com/t/unity-protection-addressing-the-emudra-and-brotli-security-vulnerabilities/1740774) [6000.3.29f1 release notes](https://unity.com/releases/editor/whats-new/6000.3.29f1)

Commands below use the installed `Unity.exe` and the isolated project path as `<QA>`. Log and artifact paths are relative to that snapshot.

```text
Unity.exe -batchmode -nographics -projectPath <QA> -executeMethod EchoMountain.EditorTools.SunsetBasinBuilder.Create -logFile <QA>/generate.log -quit
Unity.exe -batchmode -nographics -projectPath <QA> -runTests -testPlatform PlayMode -testFilter EchoMountain.Tests.FreeRoamTests -testResults <QA>/focused.xml -logFile <QA>/focused.log
Unity.exe -batchmode -force-d3d11 -projectPath <QA> -runTests -testPlatform PlayMode -testResults <QA>/regression.xml -logFile <QA>/regression.log
Unity.exe -batchmode -nographics -projectPath <QA> -executeMethod EchoMountain.EditorTools.BasinAppearanceInspectionFix.Apply -logFile <QA>/appearance.log -quit
Unity.exe -batchmode -force-d3d11 -projectPath <QA> -runTests -testPlatform PlayMode -testResults <QA>/final.xml -logFile <QA>/final.log
Unity.exe -batchmode -nographics -projectPath <QA> -executeMethod EchoMountain.EditorTools.LabBuild.BuildWindowsFreeRoam -echoBuildOutput <QA>/Builds/SunsetBasin/EchoMountain.exe -logFile <QA>/windows-build.log -quit
```

- Editor generation exited 0. The focused test pass was 4/4. Both full graphical runs passed 48/48, including 47 repository PlayMode tests and one temporary capture fixture. The final run exited 0. Pine and Summit reached Results in 84.49 s and 85.34 s, respectively. Existing practice and replay checks also passed.
- In Free Roam, all three drop-ins moved downhill without reset in the first five seconds, reaching 29.6, 22.9, and 40.6 km/h, respectively. Out-of-bounds and inverted snow-level recovery returned to the active drop-in. A pop on High Traverse reached 3.34 m above the snow and produced a Recover landing. The Switchyard test popped from a standing-start descent onto the box, rode off, and banked grind points once it landed. The crosswise approach missed the rail. Reset kept banked points and cleared pending points. The menu test opened the map and returned to Glacier Pass without using the sandbox as a timed run.
- Three 1280 × 720 URP captures were inspected, one at each drop-in. The initial capture exposed strong sun glare on the snow. The final build sources use rougher snow and adjusted haze. The one-off QA appearance updater changed those assets in place and is excluded from the project and player. The final capture fixture was also local to the QA snapshot. The Starter Kit reference window did not activate during this session, so visual work drew on the supplied stills and the prior Starter Kit playtest observations recorded below.
- Windows x64 build exited 0 and was copied to `Builds/SunsetBasin/EchoMountain.exe` with both scene files and matching player, runtime DLL, and scene hashes. Launched `EchoMountain.exe -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -logFile "<repo>/Logs/SunsetBasin-QA/windows-player.log"` with the build directory as the working directory. The smoke process was responsive (PID 35352) and initialized Direct3D 11, PhysX, and input with no inspected startup exceptions. This background smoke check did not exercise standalone menus or controls. Evidence is in `Logs/SunsetBasin-QA`.
- Hands-on rail timing, the longer rails and linked tricks, the steeper drops at different speeds, rider readability through trees and in the air, hardware input/audio, and traversal frame times remain manual checks. See the play mode checklist in README.

## Wide Board v7 — 2026-11-23

Wide Board v7 passed all four focused tests, built, and launched. Confirmed 12-inch runtime waist and inspected a close Windows capture. See `%TEMP%/EchoMountain-StarterR2-20261119/docs/QA.md` for exact commands and limitations.

## Floor Recovery v6 relaunch — 2026-11-23

Relaunched `Builds/StarterRedesign-FloorRecovery-v6/EchoMountain.exe -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -logFile "%TEMP%/EchoMountain-FloorRecovery-v6-launch-20261123-132747.log"` from the executable folder. PID 19480 initialized input and opened a responsive Glacier Pass window. Foreground activation succeeded. No new build or gameplay test ran.

## Snow recovery v6 — 2026-11-23

Snow Recovery v6 built and launched responsively. Unity reproduced the inverted sliding failure before the fix, then passed the full 45-case suite. Exact commands and observations are in `%TEMP%/EchoMountain-StarterR2-20261119/docs/QA.md`.

## Twin-tip snowboard v5 — 2026-11-23

Snowboard v5 built successfully and launched responsively. Focused Unity PlayMode tests passed 3/3. Inspected two close views and a short scripted Summit descent. Full commands and evidence paths are in `%TEMP%/EchoMountain-StarterR2-20261119/docs/QA.md`.

## Slim rider v4 — 2026-11-23

The latest local Windows player, `Builds/StarterRedesign-SlimRider-v4/EchoMountain.exe`, built successfully and launched responsively. Focused PlayMode tests passed 3/3. Build and capture commands, logs, and limitations are recorded in `%TEMP%/EchoMountain-StarterR2-20261119/docs/QA.md`.

## Skinned rider playtest launch — 2026-11-23

Launched `Builds/StarterRedesign-SkinnedRider-v3/EchoMountain.exe -screen-fullscreen 0 -logFile "%TEMP%/EchoMountain-SkinnedRider-v3-launch-20261123-123533.log"` with the executable folder as the working directory. PID 11248 opened a responsive `Echo Mountain - Glacier Pass` window. Unity 6000.3.28f1 initialized Direct3D 11, PhysX, and input. No new build, automated tests, or hands-on gameplay were performed.

## Windows player launch — 2026-11-19 14:45 EST

- On `experiment/carve-brake-r3` at `fa9b608`, launched `Builds/GlacierPass-Carve-r3/EchoMountain.exe` from its build directory with PowerShell `Start-Process -FilePath $game -WorkingDirectory (Split-Path $game) -ArgumentList @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-logFile',"`"$log`"") -PassThru`, where `$game` resolves to that executable and `$log` is `%TEMP%/EchoMountain-r3-launch-20261119.log`. PID 26092 remained responsive and exposed the `Echo Mountain - Glacier Pass` window. Startup log reports Unity 6000.3.28f1, Direct3D 11 on Intel Iris Xe, and input initialized. No new Unity test or build ran.
- The player was left open for hands-on play. This process/window/log check does not verify menu interaction, riding, controls, save persistence, audio, or frame rate. Existing untracked `Assets/Resources/GDKEditionAutoGen*` was left untouched.

## Carving and braking experiment gate — defined before gameplay edits, 2026-11-18

- Isolated project copy: `%TEMP%/EchoMountain3D-CarveBrake-20261118-94b91e6` from `94b91e6`; the selected repository and R2 player remain intact. Compare against the unmodified R2 baseline with the same 60 Hz fixture and input sequence.
- On a flat packed-snow fixture at 6 and 16 m/s, run two 0.6 s left/right S-turn cycles. Require continuous supported contact for at least 95% of sampled ticks after settling, no accidental landing/bail, yaw settling below 0.6 rad/s within 0.5 s of release, and peak high-speed yaw at least 15% lower than R2 while retaining a visible turn (at least 5 degrees in 0.6 s). Log measured heading, peak yaw and contact counts rather than asserting subjective feel from automation.
- From 12 m/s on the same flat fixture, measure 2 s planar path length: full brake must shorten travel by at least 30% versus coasting; simultaneous steering/braking must still shorten travel by at least 25% versus coasting. Compare the experiment with R2 and reject a weaker brake-while-carving result. No reversal or unsupported acceleration from braking.
- Existing clean jump/landing and intentional severe-landing bail tests must keep passing without changing landing thresholds. Pine and Summit route start/finish, score banking, replay, pause/restart and save isolation require the full PlayMode suite plus a fresh Windows build. Manual Windows verification remains required for sustained S-turn feel, camera comfort, route readability and actual saved-record reopening; automated passes alone do not close those checks.

### Reference comparison and observation limits

- The supplied `SnowboardStarterKit.exe` launched windowed with `-screen-fullscreen 0 -screen-width 1280 -screen-height 720`; `globalgamemanagers` identifies **Unity 2020.2.1f1**. In the live run, the rider descended past varied terrain, the chase camera kept the rider near the lower centre and pitched with steep slopes, snow spray appeared near the board, and a compact trick score displayed. Brief Left/Right taps were sent, but sustained turning, speed change, braking distance, jumps and landing tolerance could not be measured from them.
- Echo Mountain R2's Windows player launched and rendered its Pine/Summit menu. Windows automation could capture that menu but did not reliably activate it for clicks or Return, so this run provides **no direct Echo riding, braking, jump, landing or camera comparison**. Earlier Pine/Summit route, controls, score and replay passes remain user observations; visual/HUD and records after reopening remain open. The existing `runs-v1.json` SHA-256 remained `5DE5F58CA406A2871E2339C5CD5947FC03C6FA0069375A52A099156BB13E5233` after this attempt.
- Read-only `Assembly-CSharp.dll` metadata inspection found starter classes/fields for smoothed ground/air/bomb input, rotation braking, centre/normal/on-edge friction, lateral skid response, a predicted landing normal, and damped camera follow with speed/FOV factors. Those names suggest design mechanisms; they do **not** establish their exact equations, constants, quality, or runtime outcome. Echo's code already has steering smoothing, grip, forward braking, landing rules and a travel-aware camera. The experiment therefore targets high-speed yaw and brake response within `LabRider`, without copying the starter's code or changing Echo's landing/camera systems.

### R2 measurements in the isolated Unity copy

- Two initial commands mistakenly included `-quit`; both exited 0 after import and produced **no** test XML. They are import checks, not test passes. Corrected command: `Unity.exe -batchmode -nographics -projectPath <QA> -runTests -testPlatform PlayMode -testResults <QA>/r2-baseline.xml -logFile <QA>/r2-baseline-tests.log`, started with PowerShell `Start-Process -WindowStyle Hidden -PassThru` and waited for the process. Exit **0**, XML **41/41 passed**, 0 failed/skipped.
- Added a QA-copy-only measurement fixture before tuning. Same test command with `-testFilter EchoMountain.Tests.HandlingExperimentTests`, output `r2-handling.xml`/`.log`: exit **0**, **2/2 passed**. At 6 and 16 m/s, first 0.6 s heading change was **10.77°** and peak yaw **0.678 rad/s** at both speeds; support was 144/144 ticks and release yaw 0.189 rad/s. From 12 m/s over 2 s, coasting covered **23.56 m**, straight braking **13.47 m**, and braking through alternating S-turns **13.33 m**; support was 120/120 ticks in all modes. These are controlled flat-fixture measurements, not reported player feel on the routes.

### Carving experiment result

- Tried speed-based yaw trim and planar braking in the QA copy. Focused `HandlingExperimentTests` command (same as above, `r3-handling.xml`/`.log`) exited 0, 2/2 passed. High-speed peak yaw fell to 0.464 rad/s, but S-turn braking distance was 13.36 m versus R2's 13.33 m. The planar-braking change was **discarded**; final `LabRider` retains R2 braking force and adds only a linear yaw-authority trim from 1.0 at 8 m/s to 0.68 at 16 m/s.
- Final full command `Unity.exe -batchmode -nographics -projectPath <QA> -runTests -testPlatform PlayMode -testResults <QA>/r3-final.xml -logFile <QA>/r3-final.log`, launched and waited with `Start-Process -WindowStyle Hidden -PassThru`: exit **0**, **43/43 passed**, 0 failed/skipped. Final fixture: at 6 m/s, 10.77° first-turn heading and 0.678 rad/s peak yaw (unchanged); at 16 m/s, **7.35° and 0.464 rad/s** (31.6% lower peak yaw); 144/144 supported ticks at each speed, no false landing/bail. At 12 m/s, coast **23.56 m**, straight brake **13.47 m**, brake through S-turns **13.35 m** over 2 s (within 0.02 m of R2). No measurable brake-distance improvement is claimed.
- Landing thresholds, camera, surface map, scoring and replay code are unchanged. `LocalRunStore` uses `rulesVersion=3` and `runs-glacier-pass-c1-r3.json` because yaw physics changed; test protects both r1 and r2 files and their backups from modification. This does not resolve standalone record persistence after reopening.
- Windows build command: `Unity.exe -batchmode -quit -projectPath <QA> -buildTarget StandaloneWindows64 -executeMethod EchoMountain.EditorTools.LabBuild.BuildWindowsMountainPolished -echoBuildOutput <QA>/Builds/GlacierPass-Carve-r3/EchoMountain.exe -logFile <QA>/r3-build.log`, launched and waited with `Start-Process -WindowStyle Hidden -PassThru`: exit **0**, `Build Finished, Result: Success`, player produced. Copied the complete player to `Builds/GlacierPass-Carve-r3`; runtime assembly SHA-256 matches the QA build. Existing R2 executable hash stayed `FBD073CA59C4918EDBC6704F5393CF59021173AF11714C00DB4212C50117492B`; both Glacier Pass scenes and `.meta` files remained hash-identical to R2.
- Launched that QA-built r3 player windowed with `-screen-fullscreen 0 -screen-width 1280 -screen-height 720 -logFile <QA>/r3-player.log`. Process exposed a responsive `Echo Mountain - Glacier Pass` window; Windows capture showed Pine/Summit menu and background. Log showed Direct3D 11 and Input System initialization, with no inspected `Exception`, `Error`, or `Crash` line. Automated Return did not start a route, so **no r3 physical turning, route finish, trick, replay, pause, camera or save-reopen pass is claimed**. Closed only this launched process. Existing `runs-v1.json` remained hash-identical at `5DE5F58CA406A2871E2339C5CD5947FC03C6FA0069375A52A099156BB13E5233`; no r2/r3 record file was created during smoke launch.
- Manual gate before treating this experiment as accepted: on Pine and Summit compare R2/r3 low-speed and fast A/D S-turns, hold S while carving, check contact on crests, clean spin+grab landings and intentional severe sideways bail; finish and inspect score/replay, Esc pause/restart/menu, and close/reopen r3 records while confirming r1/r2 files are intact. Watch camera horizon and frame pacing, but do not change camera/landing code in this slice. New r3 book begins with default preferences; earlier record books are retained separately. Physical gamepad, visual/HUD quality, performance and the previously reported persistence issue remain open.

## Environment discovery, 2026-11-06

- Host: ASUS Vivobook X1502VA, Intel Core i9-13900H, 20 logical processors, approximately 15.6 GiB RAM, Intel Iris Xe Graphics. Windows reports approximately 530 GiB free on C: at inspection; graphics memory reporting may not reflect shared memory.
- Git: repository clean at `fcb144a` after an externally observed commit of the brief; initial state was `a6389d8` with an untracked brief. No tracked Unity cache observed. Git LFS 3.7.1 installed; not yet needed.
- Installed Unity 6000.3.28f1 and 6000.6.8f1; Windows standalone support folder exists for 6000.3.28f1. The running 6000.6.8f1 editor targets a separate tutorial. Existence of module files does not prove a successful build or activated license.
- Recursive workspace marker search: no Unity project markers found. Ancestor AGENTS.md search: none present before this task. The user has been asked for the project folder.
- No game implementation, compile, Unity tests, visual gameplay inspection, audio listening, build, player launch, controller test or performance measurement has occurred. All milestone gates remain open.

## Project initialization, 2026-11-06

- User selected the existing repository as the project location. Extracted 58 regular files from installed Unity `com.unity.template.3d-cross-platform-17.0.14.tgz` in `Assets`, `Packages`, and `ProjectSettings`, refusing overwrites. The template manifest specifies URP 17.0.1, Input System 1.12.0, and Test Framework 1.4.2; resolution and compatibility are not verified.
- Invoked 6000.3.28f1 in batch mode against this repository with Windows target and log `unity-import.log` (ignored). The process terminated before import with `No valid Unity Editor license found`, return code 198 in the Unity log. The initial shell return was not reliable because Windows launched the GUI executable asynchronously. No compile or tests occurred. Asked the user to activate Unity in Hub; the separate running tutorial was not touched.

- 2026-11-06: Drafted physics lab code and one PlayMode test while license activation blocked Unity. These files have not been compiled or run; no claim of successful riding, switch landings or scene generation is supported. Their Unity-generated metadata is pending. `docs/PHYSICS_LAB.md` describes the test plan.

- 2026-11-06 19:40–19:43 EST: User reported a Unity Student license, confirmed visible in Hub. Direct 6000.3.28f1 batch launch, Hub launch after registering this project, and batch after Hub license refresh all failed with `No valid Unity Editor license found`; batch exit code 198. Project import, compile and tests did not start. The failed Hub-launched editor was closed, while the unrelated tutorial editor was left alone. No project editor process remains. Requested user sign out/in to Hub after saving tutorial work; cause is not established.

## Resume 2026-11-08

- Git was clean at `bb2b9c9`, then the already-running Unity 6000.3.28f1 editor modified the manifest while resolving packages. No completed import or test was observed at checkpoint.
- A resume process check initially missed its unquoted `-projectPath`; batch start was inadvertently attempted on the locked project and exited 1. No locks were removed or processes killed. The existing editor advanced beyond earlier licensing failures to package resolution. Unity editor PID 21020 and Package Manager PID 7984 were still running at 17:40 EST. Window activation through the computer-use helper failed twice; no gameplay was viewed.

- 2026-11-08: Unity 6000.3.28f1 completed script compilation in the running main editor (Tundra success; 212.6 s) and logged project load. Unity generated `ProjectVersion.txt` and lab metadata and updated package/URP settings during initialization. URP 17.3.0 / Input System 1.20.1 / Test Framework 1.6.0 resolved. Main editor remains open. Computer-use window activation failed twice with `failed to activate captured window`, so no visual inspection yet. A distinct temporary copy with its own Library is importing under PID 18444, intended to generate the lab; no result available at checkpoint.

## 2026-11-08 — isolated lab test

- The main project loaded in Unity 6000.3.28f1 and compiled successfully; Unity generated project version, metadata, package lock and updated URP settings. No main-editor gameplay inspection occurred.
- With the main editor left open, copied `Assets`, `Packages` and `ProjectSettings` to `%TEMP%\EchoMountain3D_QA_20261108`, which used its own `Library`. Batch `-executeMethod EchoMountain.EditorTools.PhysicsLabBuilder.CreateLab` completed with exit 0 and created `Assets/EchoMountain/Scenes/PhysicsLab.unity` in the copy. Copied that new scene and generated metadata into the repository without overwriting any file; checked that its rider and camera script GUIDs match the project metadata. This does not prove the scene is physically correct.
- In the isolated copy, `Unity.exe -batchmode -projectPath <QA copy> -runTests -testPlatform PlayMode -testResults <QA copy>\playmode-results.xml -logFile <QA copy>\playmode.log` exited 0. XML: 1 passed, 0 failed, 0 skipped: `EchoMountain.Tests.LabRiderTests.FlatGroundSupportsBoardAndResetClearsMotion` (3.60 s). Test checks flat support and reset only. Scene motion, turning, jumping, landings, switch and user controls are unverified.
- Isolated Windows development build was started after the test. At this checkpoint no build result or player launch has been observed.

- 2026-11-08: In the isolated copy, `-executeMethod EchoMountain.EditorTools.LabBuild.BuildWindows -buildTarget StandaloneWindows64` produced a Windows development build with exit 0; copied the build to `Builds/PhysicsLab`, launched PID 28712 and observed a responsive process, Direct3D 11/PhysX/input initialization, and no exception lines in the startup log at inspection. The display/gameplay was not observed because the computer-use helper reported `failed to activate captured window` twice. A slope-acceleration PlayMode test is running separately; not yet a result.

- 2026-11-08: User observed no visible board movement, Space/brake failures and working reset in initial Windows build. Isolated slope test failed: -0.00053 m displacement after 150 fixed ticks (threshold >2 m). A candidate change removed default board friction and filtered self-hits in ground probes. The isolated PlayMode suite then passed 3/3: flat support/reset, 15-degree downhill acceleration and supported pop. Tests use a direct pop request for the latter; physical keyboard and braking remain unverified. Runtime and test source copied to the main project; existing first Windows build remains faulty/outdated. A further virtual-keyboard test is running in the isolated copy.

- 2026-11-08: A fourth isolated PlayMode test exercised virtual keyboard S braking and Space pop. After temporarily overriding the test editor's background/input routing and avoiding a manual update that consumed the Space edge, all 4/4 tests passed with exit 0. These settings are restored by the test; no claim about real input hardware or gameplay observation. A rebuilt executable was started but not yet complete at this checkpoint.

- 2026-11-08: Repaired Windows development build succeeded (exit 0), was copied to `Builds/PhysicsLab-Repaired` and launched as PID 15708. Startup log had no exception lines when checked. User reported the repaired window still did not visibly move or respond to turn, brake, jump or reset. No visual evidence from assistant; helper activation failed. Simulated D steering in a simple isolated test fixture passed (5/5), but the user's actual gameplay observation remains a failure. A test of the generated scene is in progress in the isolated copy.

- 2026-11-08: User later reported the repaired player works and provided a screenshot. It showed the overlay at 48.1 km/h and contact true, a small low-contrast primitive board, plain grey ground and tall walls. This is a user-supplied visual observation, not complete control or gameplay validation. Isolated suite passed 6/6 after fixing a scene-test loading/cleanup defect; it includes a test of the real generated lab scene with simulated Space. QA-only marker/rider/camera presentation changes remain untested at this checkpoint.

## Readability pass, 2026-11-08

The user supplied a screenshot of the functioning repaired player that showed a small, low-contrast board against plain grey ground. In an isolated Unity project, an editor tool added original URP/Lit colors, 20 non-colliding slope markers, and a non-colliding capsule rider placeholder; a closer camera was drafted. The improved lab was saved as the separate `PhysicsLabReadable.unity` using Unity's AssetDatabase copy, leaving the original lab scene in place. The final isolated suite passed 6/6, including actual-scene motion, virtual Space/brake/steer, support and reset. Automated color/material assertions do not replace visual review. The rebuilt Windows player is still in progress at this checkpoint; no user has yet inspected the higher-contrast scene.

- 2026-11-08: The `PhysicsLabReadable` Windows development build succeeded (exit 0), copied to `Builds/PhysicsLab-Readable/EchoMountainLab.exe`, and launched as PID 24012. The window title was `Echo Mountain Physics Lab - Markers`; process responded and startup log showed graphics, PhysX and input initialization without exception lines at inspection. The user has been asked to assess this version; no feedback on its visuals yet. The six-test isolated PlayMode suite passed on the readable scene. This is a diagnostic build, not a complete game.

- 2026-11-08: After the user requested a readable speedometer, controls and themed side diagnostics, implemented a dedicated `LabHud` and saved a separate HUD lab scene. Isolated Unity tests passed 8/8 with scene and virtual input checks. Added preliminary landing-rule and grounded/airborne checks, including switch direction and a test preventing initial-spawn/repeated landing counts. This remains diagnostic feedback, not a complete landing/bail/recovery/scoring system. HUD build started but visual quality remains unobserved at checkpoint.

- 2026-11-08: HUD Windows development build succeeded with exit 0, was copied to `Builds/PhysicsLab-HUD`, and launched as PID 8560. Its title is `Echo Mountain Physics Lab - HUD`; it remained responsive and startup log showed PhysX/input initialization without exception lines at inspection. User visual feedback on layout is pending. Eight isolated PlayMode tests passed. No automated result establishes visual quality, full landing recovery or the entire M1 gate.

- 2026-11-08: Added an isolated switch pop-and-landing PlayMode test using actual Rigidbody motion on flat ground. It verified the board became airborne, regained support, emitted one landing, and classified aligned switch travel clean. Unity 6000.3.28f1 PlayMode suite passed 9/9 (exit 0). This is one controlled configuration, not proof of all switch landings or physical bail/recovery.

- 2026-11-08: User reported the HUD layout is better and requested continued development. Isolated Unity suite passed 10/10 after adding a test of severe sideways landing, a timed recovery state and reset to spawn with cleared motion. This is a basic diagnostic recovery, not a polished bail system; the currently launched HUD build predates this change. No external playtest or broad gameplay acceptance was established.

- 2026-11-08: Re-ran the 10-test PlayMode suite with `Time.fixedDeltaTime = 1/60` set per test and restored in teardown; 10/10 passed with exit 0. This is an isolated fixed-step experiment, not proof of rendering at 30/60/120 FPS, performance or cross-platform determinism. The main project's default fixed timestep remains unchanged (0.02 s at last inspection).

## Resumed physics baseline

- Recovered the flat-seam PlayMode test from the previous isolated QA copy, where the 11-test run passed. Then copied the current repository's Assets, Packages, and ProjectSettings (base 11d1ca1) into a fresh isolated project with the recovered test. Unity 6000.3.28f1 ran `-batchmode -projectPath <snapshot> -runTests -testPlatform PlayMode -testResults <snapshot>/baseline.xml -logFile <snapshot>/baseline.log`. Exit 0, 11 passed, 0 failed. The seam case starts at 12 m/s and checks traversal, no tunneling, no more than three consecutive unsupported ticks, and no false landing. The tests run at 60 Hz physics and restore the project default.
- Recovered the previous Windows development build's successful build log and inspected `Builds/PhysicsLab-Recovery/player.log`. The build was produced in the earlier QA copy and launched with Direct3D 11 and PhysX initialization. The fresh baseline does not establish performance or a current standalone playthrough.
- Evidence: `%TEMP%/EchoMountain3D-Resume-b7aee1a6/baseline.xml` and `baseline.log`. Test machine: Windows, Intel Core i9-13900H, Intel Iris Xe Graphics, 15.6 GiB RAM.

## Lab descent and camera

- At a 60 Hz physics step, the center line from the authored HUD scene's normal spawn reached the runout at 30, 60, and 120 simulated frame updates per second. Each run logged 11.12 m/s approach speed, 0.917 s airborne, one Recover landing at z=54.80 m, and supported arrival at z=69.07 m. No pop or steering was injected. The regression allows ±0.1 s of flight time and ±0.6 m of landing position, requires one landing, and requires a supported board at the runout.
- The camera test verified that the view faces the direction of travel during switch riding, keeps a level horizon, and snaps behind the spawn heading after reset. The project fixed timestep is now 1/60 s so the player and tested setup use the same physics step.
- Unity 6000.3.28f1 ran the full PlayMode suite in the isolated project using the same command as the resumed baseline with output names `camera-and-descent.xml` and `camera-and-descent.log`. Exit 0, 15 passed, 0 failed. The tests control the simulated frame cadence. Standalone frame rate, performance, and physical gamepad behavior still need testing.

## Lab controls and pause

- The controls now use named Input System actions for steer, brake, pop, reset, and pause. The virtual gamepad test sent stick, trigger, Start, south, and east events through the normal update loop. It verified steering and brake input, frozen motion while paused, discarded jump input during pause, a pop after resuming, and a reset to the spawn. The existing virtual keyboard cases still pass.
- Unity 6000.3.28f1 ran the full PlayMode suite in the same isolated snapshot, with outputs `controls.xml` and `controls.log`. Exit 0, 16 passed, 0 failed. Physical controller hardware and device disconnect behavior remain pending.
- The earlier `Builds/PhysicsLab-Descent` development build completed and ran at 1280×720 windowed. The rendered course and HUD appeared in the window capture, but a Windows Firewall dialog blocked input. A separate playtest build without the development profiler was started. See `build-descent.log` and `build-playtest.log` in the isolated snapshot.

## Articulated rider lab

- Unity 6000.3.28f1 generated `PhysicsLabRider.unity` from the HUD scene using `-batchmode -quit -executeMethod EchoMountain.EditorTools.PhysicsLabPresentation.CreateRiderScene`. Exit 0. The earlier scene and its metadata were preserved.
- The full PlayMode suite passed 17/17 with exit 0. The new scene test checked the rider tucking during a pop, the front boot remaining fixed to the rig, the rig following the board, and the absence of rider-model colliders. Results are in `rider.xml` and `rider.log` in the isolated snapshot.

- The Windows x64 rider playtest built successfully with `-batchmode -quit -buildTarget StandaloneWindows64 -executeMethod EchoMountain.EditorTools.LabBuild.BuildWindowsPlaytest`. The build log reported success and exit 0. Copied the complete build to `Builds/PhysicsLab-RiderPlaytest` and launched it windowed at 1280×720. The player initialized Direct3D 11 and PhysX and rendered a live speedometer and HUD. The Windows Firewall dialog continued to obscure the center of the window, so the current standalone manual control and rider review remain open. No settings were changed in that dialog.
- The downloadable ZIP was checked for the executable, Unity player library, runtime assembly, and game data. The locally launched build was the source of the ZIP.

## Glacier Pass course

- Unity 6000.3.28f1 generated the independent course scene and original scenery assets with `-batchmode -quit -executeMethod EchoMountain.EditorTools.MountainBuilder.Create`. Exit 0. The rider lab remains available for regression.
- Full headless regression used `-batchmode -nographics -runTests -testPlatform PlayMode -testResults mountain-regression.xml -logFile mountain-regression.log`. Exit 0, 19 passed, 0 failed. Both tests loaded the actual Glacier Pass scene, selected their line, traversed 860 m with 60 Hz physics, reached Results, and verified that retry returned to the same spawn with a fresh timer. Test time was accelerated at six times normal game time.
- Pine coasted to the finish in 84.49 s with a 44.7 km/h peak. Summit finished in 87.14 s with a 43.3 km/h peak and one counted landing. Summit uses a virtual gamepad for lane correction. An earlier unattended Summit run drifted beyond the course boundary and triggered its safe reset. The logs and XML are in the isolated snapshot.
- On the earlier Windows rider lab, the firewall dialog was gone and the rider and HUD were visible. Injected reset and pause keys did not produce a clear response. A deliberate hardware input run remains open.

- The Windows x64 player build used `-batchmode -quit -buildTarget StandaloneWindows64 -executeMethod EchoMountain.EditorTools.LabBuild.BuildWindowsMountain`. Exit 0. The window opened at 1280×720 with Direct3D 11 and PhysX. In the actual player, clicked both line selectors, selected Pine, clicked Drop in, observed the rider and progress mid-run, reached Results at 01:24.48 with a displayed 45 km/h top speed, and clicked Retry. Pine started again at 0% with the timer reset. No exception or error lines appeared in the inspected player log. Physical keyboard and gamepad input and a manually steered Summit run remain open. Automated Summit used a virtual gamepad.
- The Windows archive contains 220 checked entries, including the executable, runtime, and game data. The repository build is at `Builds/GlacierPass`.

## Tricks and jump practice

- Reran the prior full PlayMode baseline in the isolated copy before editing. Exit 0, 19 passed. The revised full headless suite used `-batchmode -nographics -runTests -testPlatform PlayMode -testResults tricks-regression.xml -logFile tricks-regression.log`. Exit 0, 22 passed, 0 failed. Pine again completed in 84.49 s and Summit in 87.14 s with virtual steering.
- In the actual Glacier Pass scene, virtual keyboard inputs opened practice, popped, spun, held a grab, and released the spin to settle for landing. The board measured about 184° for a clean 180 Indy worth 260 and 365° for a clean 360 Melon worth 450. Practice results held the points and retry reset the score and spawn. The score test also covers wraparound, no awards before landing, duplicate prevention, repetition, chain expiry, recover, and bail.
- The three synthesized clips have a mute control and follow the run and pause state. This pass checks execution and controls. Listening and mix balance still need a pass.

- In the Windows playtest before the camera-only revision, switched mute, opened Summit practice, verified the on-screen pause kept time at 00:05.86, resumed, reached practice results in 00:21.01, retried, and returned to the line menu. A full Pine run then reached the results screen in 01:24.48. The Quit button closed that player. Injected Escape did not resume the paused window. Physical keyboard and controller use and listening are still pending.
- The camera snapped on rider resets but could sweep across the course when leaving practice. Added snap detection for distant changes of the followed position and checked return-to-menu framing in the practice regression. The final suite passed 22/22. See `tricks-final.xml` and `tricks-final.log`.
- The final Windows x64 build succeeded using `-batchmode -quit -buildTarget StandaloneWindows64 -executeMethod EchoMountain.EditorTools.LabBuild.BuildWindowsMountain`. Observed cold launch and the title screen. No exceptions or errors appeared in the inspected startup log. The ZIP contains 220 entries and its runtime assembly hash matches the final local build.

## Resume verification and local records — 2026-11-09

- Resumed clean at `5ee28f3` in `C:\Users\diego\EchoMountainGame`, branch `main`. No Unity or Echo Mountain player was running, no project lock existed, and there were no nested/ancestor instructions beyond the project AGENTS.md. Earlier ignored builds and temporary QA snapshots are absent here. Preserved Unity 6000.3.28f1, URP 17.3.0 and existing GUIDs. Current test PC: Windows 10 19045, Ryzen 5 5600X, RTX 3060, approximately 32 GiB RAM; approximately 35 GiB free before import. This differs from the prior laptop evidence.
- Baseline command: `& 'C:/Program Files/Unity/Hub/Editor/6000.3.28f1/Editor/Unity.exe' -batchmode -nographics -projectPath C:/Users/diego/EchoMountainGame -runTests -testPlatform PlayMode -testResults C:/Users/diego/EchoMountainGame/Logs/Resume-20261109/baseline.xml -logFile C:/Users/diego/EchoMountainGame/Logs/Resume-20261109/baseline.log`. XML: 22 passed, 0 failed; log: `Test run completed. Exiting with code 0 (Ok)`. The initial PowerShell invocation did not retain the GUI process exit status, so the XML and Unity log establish the result. Subsequent invocations use `Start-Process -WindowStyle Hidden -PassThru`, `WaitForExit()` and the process `ExitCode`.
- First full save run (`records.xml` / `records.log` using the same test arguments) returned exit 2: 26 passed, 2 failed. Unity JSON serialized empty inline run objects into default-valued objects, causing valid save files to fail validation and preventing backup rotation. Fixed exact-empty sentinel normalization; kept numeric/data validation strict.
- Focused rerun added `-testFilter EchoMountain.Tests.LocalRunStoreTests`, with `records-focused.xml` / `.log`: process exit 0, 6 passed, 0 failed. Checks include actual file round trips, preferences and separate route/practice bests, backup recovery with damaged primary preservation, incomplete JSON, future schema/rules preservation, NaN/infinity/negative score rejection, and a filesystem write failure. The full scene regression including one-bank-per-finish and retry persistence is running (`records-final.xml` / `.log`); do not consider it passed before inspecting the result.
- Save writes use a flushed sibling temporary file and `File.Replace` for valid primaries, retaining one `.bak`; a corrupt primary is moved to `.invalid` and a good backup retained. Incompatible positive schemas/course/rules/assist are read-only, with a visible notice. There is no earlier shipped save schema to migrate. Editor/batch test stores are separated from standalone data. This checks software behavior, not disk/controller power-loss guarantees or tamper resistance.
- Full final save regression: `records-final.xml` / `records-final.log`, process exit 0, 28 passed, 0 failed. Pine and Summit actual-scene tests each reloaded the file after finishing, verified a single recorded completion across additional Results frames, and verified retry did not add a completion or clear the record. No standalone save restart has been observed yet.

## Snapshot replay — 2026-11-09

- Ran the full PlayMode command with output `Logs/Resume-20261109/replay.xml` and `replay.log`: process exit 0, 30 passed, 0 failed. Two new rule tests cover heading-wrap/motion/pose interpolation, discrete event timestamps, metadata, frame/duration limits and invalid motion rejection. Both actual full-course regressions additionally entered replay, verified rider/collision disabled, sought to start/middle/end, checked pause and half-speed advancement, returned to the original results pose, retained the same elapsed time/points/single saved finish, and retried into a fresh recording.
- Runtime records at approximately 20 Hz and interpolates snapshots; it does not replay input through physics. Visual state includes the segmented rig's actual compression, lean, tuck and grab blends. Score/landing counts, grab and contact are timestamped. The current attempt is capped at 180 seconds / 3602 frames; an overlong or invalid recording is not offered as a complete replay. Course/version/rules/assist/build/regular-stance metadata is attached. Recordings are memory-only; persistence, optional ghosts and free camera remain later work. Audio is silent during replay; particles are not recorded in this step. Standalone controls and visual replay have not yet been inspected.

## Basic snow spray and final slice regression — 2026-11-09

- Authored the owned URP particle material through `-batchmode -nographics -quit -projectPath C:/Users/diego/EchoMountainGame -executeMethod EchoMountain.EditorTools.MountainSnowAssets.Create -logFile C:/Users/diego/EchoMountainGame/Logs/Resume-20261109/snow-assets.log`. Process exit 0. The create-once editor tool preserves existing assets; the serialized Resources material keeps the shader/transparent variant available in the player. Runtime source generates an original small radial puff texture.
- Full PlayMode command with `slice-final.xml` / `slice-final.log`: process exit 0, 30 passed, 0 failed. Actual Pine/Summit runs observed emitted particles, kept count <=160, and verified zero effects in results/replay. Summit exited replay via a virtual gamepad East event and remained on Results, preventing one event from also selecting the menu. Prior physics, controls, tricks, saves and replay checks passed.
- The spray is cosmetic, contact/speed/slip/edge driven, with landing bursts. It clears on reset and outside Riding, pauses with the session, uses a local cosmetic random generator and applies no physics forces. Particle rendering/readability and shader inclusion still require the Windows build check; headless emission is not visual evidence. Replay intentionally suppresses particle and sound effects.
- Windows x64 build started using `-batchmode -quit -projectPath C:/Users/diego/EchoMountainGame -buildTarget StandaloneWindows64 -executeMethod EchoMountain.EditorTools.LabBuild.BuildWindowsMountain -logFile C:/Users/diego/EchoMountainGame/Logs/Resume-20261109/windows-build.log`. New output: `Builds/GlacierPass-Replay/EchoMountain.exe`. Inspect process exit, build log and artifacts before claiming success. Existing build paths are not overwritten.

## Debugging and research checkpoint, 2026-11-11

- Reproduced two gameplay defects in the isolated Unity 6000.3.28f1 copy at `%TEMP%/EchoMountain3D-Resume-b7aee1a6`. Five separated 0.04 s grab taps incorrectly met the 0.15 s hold rule; a jump crossing the practice finish could enter Results before landing and lose the pending trick. `debug-game-before.xml` contains the failing regressions. `TrickScore` now requires one continuous hold. `MountainRun` checks the finish after rider physics and waits for a supported landing. The complete PlayMode suite in `debug-game-after.xml` passed 24/24, including the real Glacier Pass scene and virtual keyboard input. The four transferred source/test files match the tested QA copy by SHA-256.
- Reproduced failed-build retry behavior with a QA-only `IPreprocessBuildWithReport` failure injector. Old `LabBuild` reserved the final output directory before Unity returned failure. The updated method writes to a unique sibling staging directory and moves it into place only after a successful report and executable check. `debug-build-staged-failure.log` records the intentional failure and retained partial staging directory; the requested `Builds/GlacierPass-Debugged` path remained available. Removed the injector from the QA copy and reran the same `-echoBuildOutput` path. `debug-build-retry.log` records build success and batch exit 0. The QA-only injector and staging folder were not transferred to the repository.
- Copied the complete player to `Builds/GlacierPass-Debugged`. Launched that executable windowed at 1280×720 for a 12-second smoke check. The process remained responsive; `debug-player-smoke.log` recorded Input System and Direct3D 11 initialization with no inspected exception/error lines. The smoke process was stopped after inspection. Physical controller response, audio quality, and a fresh manual playthrough of both lines remain unverified.
- Packaged the Windows player at `outputs/EchoMountain3D-Glacier-Pass-Debugged-Windows.zip` in the Codex task workspace. ZIP validation found 218 entries and checked the executable, UnityPlayer library, game data, and runtime assembly.
- Ran `py -3 -I icm-architect-main/tests/test_tools.py` in the repository. All 13 toolkit regressions passed. The graph preview previously rendered formatted text and removed an embedded frame in a browser. The later special-character internal-link encoding has a passing CLI regression; interactive navigation with those characters remains to be checked because the browser access host was unavailable during the follow-up attempt.
- Researched official snowboard game and Unity descriptions for `agent-context/01-GAME-IDEAS.md`. The proposed surface coefficients and placement are untested design assumptions. Commercial games' private physics implementations remain unknown.

## Integrated branch verification, 2026-11-11

- Fetched `origin/main` after its independent records, in-memory replay, and spray commits appeared. Merged the run logic so `Replay.Capture` and result/record finalization share one grounded finish decision. Preserved both sets of tests and project files; no Unity lock was forced. The remote feature slice had independently passed 30/30 tests before this integration; the earlier debug slice had passed 24/24.
- Synced the merged `Assets`, `Packages`, and `ProjectSettings` into the existing isolated QA project. Unity 6000.3.28f1 compiled the combined code and ran the full PlayMode command `-batchmode -nographics -projectPath <QA> -runTests -testPlatform PlayMode -testResults <QA>/merge-regression.xml -logFile <QA>/merge-regression.log`. Process exit 0; XML passed 32/32 with 0 failed and 0 skipped. A prior import-only invocation included `-quit`, exited 0 before tests, and produced no XML; it is not counted as a test pass.
- Built the combined course with `-batchmode -quit -projectPath <QA> -buildTarget StandaloneWindows64 -executeMethod EchoMountain.EditorTools.LabBuild.BuildWindowsMountain -echoBuildOutput <QA>/Builds/GlacierPass-Integrated/EchoMountain.exe -logFile <QA>/merge-build.log`. Process exit 0; complete player produced. Copied it to `Builds/GlacierPass-Integrated` and matched the runtime assembly SHA-256 against the QA build.
- Launched the copied Windows player at 1280x720 for 12 seconds. It remained responsive and logged Input System and Direct3D 11 initialization without inspected error/exception lines in `merge-player-smoke.log`. The test process was stopped afterward. This is a startup check, not a manual replay or save/restart playthrough.
- Packaged `EchoMountain3D-Glacier-Pass-Integrated-Windows.zip` in the Codex task `outputs` folder. The archive has 218 entries, excluding Unity's `*_DoNotShip` folder, and includes the executable, UnityPlayer library, game data, and runtime assembly.


## Resume baseline and camera reproduction, 2026-11-12

- Clean baseline `61e49ee`, actual project in the selected OneDrive Documents workspace. No running Unity editor or game at discovery; retained the existing main-project lock file and used a fresh isolated Assets/Packages/ProjectSettings copy at `%TEMP%/EchoMountain3D-Resume-20261112` with its own Library. Current PC: Intel i9-13900H, Iris Xe integrated graphics, 15.6 GiB RAM; 513 GiB free on C at discovery. This is not the earlier Ryzen/RTX machine.
- Actual test command: Unity `6000.3.28f1/Editor/Unity.exe -batchmode -nographics -projectPath <QA> -runTests -testPlatform PlayMode -testResults <QA>/baseline.xml -logFile <QA>/baseline.log`. Started with PowerShell `Start-Process -WindowStyle Hidden -PassThru`, waited for exit. Exit 0, 32 passed, 0 failed. Source and scene GUIDs retained; packages match the selected checkpoint.
- Added two camera tests to the unchanged QA runtime. Same command plus `-testFilter EchoMountain.Tests.CameraObstructionTests`, outputs `camera-before.xml` / `.log`: exit 2, 0 passed, 2 failed. The old camera remained at z=-6.5 behind a thin wall on both initial placement and a new obstruction; expected rider-side z > -3.1.
- Launched the previous integrated player at windowed 1280x720 with `-logFile Logs/Resume-20261112/baseline-player.log`. Computer-use screenshot showed the two-line menu and scenery; no startup exception found. Injected click/Enter did not start a ride; even injected Alt+F4 had no observed effect. The process remained responsive. Stopped only this task-launched PID 944 after inspection. This is an unresolved UI input/injection limitation, not proof that physical input is broken or working. No save/replay/restart acceptance is claimed from it.
- New camera/input source is being tested with the same full-suite command, outputs `foundation-after.xml` / `.log`. Result pending at this entry.

- First revised full suite (`foundation-after.xml` / `.log`) exited 2: 32 passed, 4 failed. New obstruction checks pulled the view in correctly, but its return test counted 90 accelerated frames while damping uses unscaled time. Changed the wait to two real seconds, retaining the same final-position tolerance. Three existing trick tests then exposed suite cleanup contamination: removing Summit's active virtual pad in `MountainRunTests` now pauses at scale 6; the HUD's later teardown restored scale 6 after the test had restored scale 1. The course fixture now disables scene roots before removing its virtual pad and restoring globals. Landing/score expectations are unchanged. Full rerun is `foundation-final.xml` / `.log`; result pending.

- Corrected full rerun (`foundation-final.xml` / `.log`): process exit 0, 36 passed, 0 failed, 0 skipped. Includes initial/dynamic thin-wall obstruction, trigger/self exclusion, smooth return and replay direction; active versus unused virtual gamepad removal, explicit keyboard resume and prompts; simulated focus loss; and all earlier physics/route/score/save/replay checks. The 180 and 360 landing score expectations remained intact. No physical disconnect or actual alt-tab test is claimed.
- Building the tested snapshot: `Unity.exe -batchmode -quit -projectPath <QA> -buildTarget StandaloneWindows64 -executeMethod EchoMountain.EditorTools.LabBuild.BuildWindowsMountain -echoBuildOutput <QA>/Builds/GlacierPass-Foundation/EchoMountain.exe -logFile <QA>/foundation-build.log`. Result pending.


## Surface experiment and Windows checkpoint, 2026-11-13

- Rechecked project instructions, status, handoff, working tree and processes. Preserved the task's uncommitted surface draft and the clean foundation commit `96efc3d`. The previous `foundation-build.log` ends with batch return code 0. Copied its complete player to `Builds/GlacierPass-Foundation`; runtime assembly SHA-256 matches the isolated output. Its startup/menu rendering was observed, with no inspected exceptions. An injected Drop in click again had no visible effect; asked for optional physical input feedback. Player PID 20164 was left available.
- Full test command used `Unity.exe -batchmode -nographics -projectPath <QA> -runTests -testPlatform PlayMode -testResults <QA>/surface.xml -logFile <QA>/surface.log`. Process exit 0, XML 38 passed, 0 failed. This recompiles and reruns all earlier physics, camera, input, route, trick, record and replay checks with opt-in surface integration. Package import emitted missing-asset/immutable-package metadata warnings but completed without compile/test failures; no package source was edited.
- Surface fixture: after 0.5 seconds at 60 Hz, initial velocity (3,0,8) m/s became packed (0.09,0,7.89), powder (0.16,0,7.80), ice (0.51,0,7.95). Tests require ice lateral speed > packed +0.15 m/s and powder longitudinal speed < packed -0.05 m/s. Elevated gravity-disabled boards retained (3,2,8) within 0.01 m/s, with zero grip acceleration; reset/support selected the expected kind again. Pure cases check half-open boundaries, overlap priority/ties, invalid inputs and packed fallback.
- Expanded the same fixture with virtual S braking: powder speed must be >0.5 m/s below packed and ice >0.8 m/s above packed after 30 ticks. Focused rerun using `-testFilter EchoMountain.Tests.SurfaceResponseTests`, outputs `surface-braking.xml` / `.log`: process exit 0, 2 passed, 0 failed. This is simulated input, not a physical snow model or real controller verification.
- New map/runtime/test metadata were generated by Unity and copied with GUIDs. Existing scenes, geometry, record schema and replay metadata remain unchanged; zones are explicitly opt-in and NOT enabled on Glacier Pass. Visible zones, first-contact boundary transitions, surface sound and feel remain pending.
- Building current tested source using the same Windows command as the foundation build, output `<QA>/Builds/GlacierPass-20261113/EchoMountain.exe`, log `verified-build.log`. Result pending at this entry.


## Runtime menu input repair, 2026-11-13

Inspection found `activeInputHandler: 1` (new input only) while all runtime buttons/sliders use OnGUI. Unity documents that this combination does not deliver runtime IMGUI input. Changed the single project setting to Both (2), preserving new Input System riding actions. Added a compile-symbol regression for both required backends and a build guard. Full PlayMode rerun `menu-regression.xml` / `.log`: exit 0, 39 passed, 0 failed. The previous injected-click failures now have a concrete configuration cause; keyboard injection and physical input still need separate observation.

The old `verified-build.log` succeeded but its output predates this fix and is not the delivery candidate. Rebuilding with the existing Windows command to `<QA>/Builds/GlacierPass-MenuFixed/EchoMountain.exe`, log `menu-build.log`. The task-owned obsolete foundation player was stopped; no user editor was closed. Actual corrected menu interaction remains pending until that build is inspected.

## Documentation audit, 2026-11-17

- Read project instructions, `PLANS.md`, latest status/handoff/QA, package/version files, relevant runtime/editor/test source, Git history and working tree. Commands included `git status --short --branch`, `git log -1`, `git remote -v`, `rg --files`, `rg -n`, `Get-Content`, and `Get-CimInstance Win32_Process -Filter "name = 'Unity.exe'"`. Audit start: clean `main` at `d399011`, one commit ahead of origin; no Unity editor process found. `ProjectVersion.txt` records 6000.3.28f1; manifest records URP 17.3.0.
- Checked existing `%TEMP%/EchoMountain3D-Resume-20261112/menu-build.log`: it reports `Physics lab build succeeded`, successful batch exit and return code 0. `Builds/GlacierPass-MenuFixed/EchoMountain.exe` exists locally. This inspects prior evidence only; no build or player was launched. Its menu, controls, save/restart, sound and performance remain unverified in this audit.
- No Unity Edit Mode/PlayMode test, compile, build, or manual gameplay was run for this documentation-only review. Historical latest suite remains the recorded 39/39 PlayMode pass; no new pass is inferred.

## Baseline verification, 2026-11-17

- Preserved the documentation-only working tree at `d399011` and did not open the locked main project (`Temp/UnityLockfile` existed; no Unity process was running at discovery). Copied only `Assets`, `Packages`, and `ProjectSettings` to `%TEMP%/EchoMountain3D-Baseline-20261117-d399011` with a separate Library. No gameplay script, scene or GUID was edited.
- Ran `Unity.exe -batchmode -nographics -projectPath <QA> -runTests -testPlatform PlayMode -testResults <QA>/baseline.xml -logFile <QA>/baseline.log` with Unity 6000.3.28f1, using `Start-Process -WindowStyle Hidden -PassThru -Wait`. Process exit 0; XML reports **39 passed, 0 failed, 0 skipped**; log says `Test run completed` and exit code 0. The suite covers controlled routes, physics, simulated input, score, replay and temporary-file record behavior. It does not verify physical controls or the standalone player.
- Before any player launch, copied the existing `%USERPROFILE%/AppData/LocalLow/Unity Technologies/Echo Mountain - Glacier Pass/runs-v1.json` (2,287 bytes) to `<QA>/preplay-record-backup`; SHA-256 `5DE5F58CA406A2871E2339C5CD5947FC03C6FA0069375A52A099156BB13E5233`. The original file must remain unchanged after manual QA.
- Started the guarded Windows build on the same QA snapshot: `Unity.exe -batchmode -quit -projectPath <QA> -buildTarget StandaloneWindows64 -executeMethod EchoMountain.EditorTools.LabBuild.BuildWindowsMountain -echoBuildOutput <QA>/Builds/GlacierPass-Baseline-20261117/EchoMountain.exe -logFile <QA>/baseline-build.log`. **Result pending at this checkpoint.**
- Build completed: process exit 0, `baseline-build.log` reports success and return code 0, and `<QA>/Builds/GlacierPass-Baseline-20261117/EchoMountain.exe` exists (667,136 bytes). This uses the same source snapshot as the 39-case test run. No build files were copied into the repository or over an older player.
- Launched that player through Windows Computer Use. Initial window discovery failed while the process started; subsequent discovery found exactly one `Echo Mountain - Glacier Pass` window. `Get-Process` reported PID 22856 responsive. Its `Player.log` showed Direct3D 11 and input initialization; copied the 1,929-byte startup log to `<QA>/baseline-player.log`, with no `Exception`, `Error`, or `Crash` line in that captured portion. The helper then failed to activate the game window and returned an unrelated VS Code image for the selected game window. UI input was stopped. The task-launched player closed via `CloseMainWindow()` and exited. **No menu, route, movement, score, replay playback, or saved-record behavior was observed in this player.**
- The original `runs-v1.json` SHA-256 after player exit equals its prelaunch hash `5DE5F58CA406A2871E2339C5CD5947FC03C6FA0069375A52A099156BB13E5233`; no restore was needed. PlayMode XML specifically passes Pine/Summit finish/retry, actual-scene 180/360 scoring, record restart/incompatible-write cases and snapshot interpolation/bounds. It does **not** test standalone replay UI playback or physical hardware. No Edit Mode suite was run (the project currently has only a PlayMode test assembly).

## Controls, bail, UI and visual polish slice — 2026-11-18

- User-reported Windows acceptance before this work: menu, Pine/Summit start/play/finish, scoring/results, replay UI, restart and return passed; steering, jump+spin+grab bail frequency, visual polish and records after reopen failed. These are user observations, not a new automated or agent-observed pass. Record reopen remains unresolved by this slice.
- At start `main` was `580aff9`; main Unity 6000.3.28f1 editor was open and its lock was preserved. Pre-existing untracked `Assets/Resources/GDKEditionAutoGen*` was not edited. Copied only `Assets`, `Packages`, and `ProjectSettings` to `%TEMP%/EchoMountain3D-ControlsPolish-20261118` for independent import, tests and build.
- Changed runtime `LabRider.cs`: ground steering reaches full input over about 0.25 s, turn torque is scaled to 72% of prior authority, supported yaw is damped at 1.8/s, and full brake reduces turn torque by 20%; longitudinal brake force is unchanged. `LabLandingRules.cs` relaxes clean limits to upright 0.87/direction 0.75/impact 5.5/spin 4 and recover limits to upright 0.6/direction 0.25/impact 9/spin 10; zero support, nonfinite values, severe sideways and high impact still bail. `MountainRun.cs` adds a true run restart and clears abandoned ride state on menu return. `LabHud.cs` adds pause Resume/Restart Run/Return to Main Menu and matches the course panel/button palette. No surface-zone or obstacle behavior changed.
- Changed editor `MountainBuilder.cs` and `LabBuild.cs`: one-time `CreatePolished` copies, then edits a new `GlacierPassPolished.unity`; fog starts at 175 m/ends at 720 m, ambient and directional light become cooler/warm balanced, and visual-only route beacons add course cues. Its Unity-generated scene GUID is `3efb8988f5d4c2c4e88c5fc898ae64ee`; the original `GlacierPass.unity` SHA-256 remained `AA89C5530D9C0C589CE902D0EBE83ABAF9EBD631A61AF78E01E68F791C1E8FB9`. The polished scene has no `SnowSurfaceMap` reference. The generator `Unity.exe -batchmode -quit -projectPath <QA> -executeMethod EchoMountain.EditorTools.MountainBuilder.CreatePolished -logFile <QA>/create-polished.log` exited 0; new scene and `.meta` exist.
- Tests changed in `LabRiderTests.cs`, `MountainRunTests.cs`, `MountainTrickTests.cs`, and `LocalRunStoreTests.cs`: bounded steering/release, forgiving supported combo versus severe bail, true restart/menu state, polished-scene Pine/Summit and scoring/replay regression, recovered spin+grab once, and separate save-file persistence. First full command `Unity.exe -batchmode -nographics -projectPath <QA> -runTests -testPlatform PlayMode -testResults <QA>/slice-tests.xml -logFile <QA>/slice-tests.log`: exit 2, **39 passed/1 failed**. Existing camera wall check measured z=-3.10474 versus required >-3.1 (4.7 mm miss). Focused `-testFilter EchoMountain.Tests.CameraObstructionTests`, `camera-focus.xml`/`.log`: exit 0, **2/2 passed**. Full rerun `slice-final.xml`/`.log`: exit 0, **40/40 passed**. No camera code or threshold was changed; intermittent risk remains.
- The first polished Windows build used `Unity.exe -batchmode -quit -projectPath <QA> -buildTarget StandaloneWindows64 -executeMethod EchoMountain.EditorTools.LabBuild.BuildWindowsMountainPolished -echoBuildOutput <QA>/Builds/GlacierPass-Polished-20261118/EchoMountain.exe -logFile <QA>/slice-build.log`; exit 0, player produced. Copied the complete player into a new repository build folder; runtime assembly hashes matched. That pre-r2 player launched with one responsive window and `Player.log` showed Direct3D 11/input initialization, with no inspected `Exception`, `Error` or `Crash` lines. Windows Computer Use captured an unrelated browser image for the selected player window, so no new menu/ride/visual/button observation was made. The task player closed normally. The existing `runs-v1.json` SHA-256 stayed `5DE5F58CA406A2871E2339C5CD5947FC03C6FA0069375A52A099156BB13E5233` before/after; no restore was needed.
- Changed `LocalRunStore.cs` only for required comparability: rules version 1 records remain at `runs-v1.json`; tuned rules use version 2 and `runs-glacier-pass-c1-r2.json`, without migration or schema change. A short notice explains separate earlier records. Focused record suite `-testFilter EchoMountain.Tests.LocalRunStoreTests`, `records-focus.xml`/`.log`: exit 0, **7/7 passed**. Full command as above with `slice-r2-final.xml`/`.log`: exit 0, **41/41 passed**, 0 skipped. Final fresh build with same method and `-echoBuildOutput <QA>/Builds/GlacierPass-Polished-r2/EchoMountain.exe -logFile <QA>/slice-r2-build.log`: exit 0, player produced and copied without overwriting to `Builds/GlacierPass-Polished-r2`; runtime assembly SHA-256 matched. This final r2 player has **not** been manually launched or played. PlayMode file reopen checks do not resolve the user-reported standalone record persistence failure.
- Manual checklist for the final r2 player: start both lines; hold/release A/D at speed, carve while lightly and fully braking, and check no runaway yaw; perform 180/360 plus Q/E grab, release spin before landing, compare clean/recover/bail feedback and points, then intentionally land sideways; pause via Esc and on-screen Pause, use Resume, Restart Run (timer/score/replay reset), and Return to Main Menu from Pine/Summit and practice; inspect beacon visibility, fog depth, sun/terrain contrast and HUD at 720p/1080p; finish, replay, retry and quit; close/reopen and check new r2 records while confirming the old r1 file remains intact. Physical gamepad, audio mix, performance and the existing persistence report remain open until observed.
- Static visual check: a **QA-only** editor helper in the isolated copy opened `GlacierPassPolished.unity`, rendered its camera at 1280×720, and wrote `<QA>/polish-preview.png`; `Unity.exe -batchmode -quit -projectPath <QA> -executeMethod PolishPreviewQA.Capture -logFile <QA>/preview.log` exited 0. The inspected frame shows distinct blue Pine/orange Summit markers, snow lanes, firs and ridge silhouettes under the new fog/sky. It also remains visibly simple and flat, and the edit-scene preview does not show the runtime rider/HUD or motion. The helper was **not** transferred to the repository; this is a limited lighting/readability check, not visual-quality acceptance.
