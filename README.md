# Echo Mountain

Glacier Pass is an 860 m snowboarding course for Windows. Ride the smooth **Pine line**, take the marked rollers on the **Summit line**, or use **Jump practice** for a shorter Summit run with a 250-point target.

## Free Roam in Sunset Basin

Open `Assets/EchoMountain/Scenes/GlacierPassPolished.unity` and enter Play mode. Choose **Free Roam / Sunset Basin** with **F**, the right bumper, or the menu button. For a standalone build, use **Echo Mountain → Build Windows with Free Roam**.

The **High Traverse** has open snow and rolling jumps. **West Ridge** has longer approaches and steeper landings. **Switchyard** has a wide box, two narrower rails, and side jumps. All three drop-ins lead into the same mountain. Pick a new drop-in from the pause menu, or press **R** / gamepad **B** to reset to the current one. Crashes also return you to that drop-in. Banked points stay with you until you return to the main menu.

At a rail, follow the teal markers, pop onto the solid top, and land with the board in line with the rail. Ride off the end or pop out. Grind points bank after the snow landing. The sandbox has no race clock or finish line.

### Play mode checks

- **High Traverse and West Ridge** — build speed, take the marked jumps with and without a pop, and check that the camera shows the landing early.
- **Switchyard** — try the wide box first, then the narrower rails. Approach from the side once to check that the board makes physical contact without starting a grind. Try riding off, popping out, and linking two features.
- **Recovery** — compare aligned, sideways, and switch landings. Bail or press **R/B** and confirm the rider returns upright with banked points and a clear camera.
- **Visibility and performance** — watch the board and limbs against the sunset and in the fir shadows. Use the Unity Profiler to compare CPU/GPU frame times at the summit, through a grove, and across the park.

## Play Glacier Pass

Run `Builds/SunsetBasin-RidePolish/EchoMountain.exe` from its full build folder. Choose a line and select **Drop in**, or select **Jump practice**. Results show time, top speed, and banked trick points. Use **Retry** to repeat the same route or practice segment.

| Action | Keyboard | Gamepad |
| --- | --- | --- |
| Choose a line | Left/Right | D-pad Left/Right |
| Drop in or retry | Enter | South button |
| Start jump practice | P | North button |
| Steer | A/D or Left/Right | Left stick |
| Brake | S or Down | Left trigger |
| Pop | Space | South button |
| Spin in the air | Z/C | Right stick Left/Right |
| Frontflip / Backflip | W / X | Right stick Up / Down |
| Hold Indy / Melon | Q/E | Right/Left bumper |
| Reset | R | East button |
| Pause or resume | Escape | Start |

The on-screen Pause button freezes the run and trick timer. Sound can be switched off from the menu or ride display. Gamepad actions have been tested with a virtual device. Current source pauses when the active controller disconnects or the window loses focus; reconnecting does not resume automatically. Ride prompts follow keyboard/gamepad use. Camera obstruction handling keeps the view in front of solid obstacles; heavily enclosed spawn points still need manual review.

### Scoring

From Results, select **Replay [V / gamepad West]** to watch the current completed attempt. Space/South pauses; arrows/D-pad seek five seconds; use the slider and 0.5x/1x/2x buttons for review. H/North hides or restores the overlay; Escape/East returns to Results. Replay uses recorded motion and poses, disables collisions and riding, and leaves scores and records unchanged. Recordings are limited to three minutes and remain in memory until retry or exit. Saved ghosts and replay files are still pending.

Completed runs also update local previous, fastest, and highest-score records for each line and jump practice. The practice target, sound preference, and last-selected line survive restart. Saves use a versioned JSON file with a backup under Unity's persistent data folder; invalid or incompatible data produces a visible notice. Editor tests use separate temporary stores.

A completed 180 earns 180 base points and a 360 earns 360. Continuously holding an Indy or Melon for 0.15 seconds adds 80 or 90 points. Release the spin input before touching down so the board can settle. The in-air display shows rotation and pending base points.

A clean landing banks the points. A recovered landing earns half and ends the chain. A bail earns zero. Different clean tricks within eight seconds build a multiplier from ×1 to ×1.75. Consecutive repeats earn 60% of their usual value and restart the chain. For example, a clean 180 Indy starts at 260 points, followed by a clean 360 Melon for 562 points at ×1.25.

Press a flip direction with the pop or early in the air. A completed frontflip or backflip adds 300 base points on a valid landing. The board can complete one flip per jump. The updated carving uses a separate r5 record book and reads sound and line preferences from the most recent valid r4 or r3 file.

## Edit, build, and test

Open the repository with Unity **6000.3.28f1** and load `Assets/EchoMountain/Scenes/GlacierPassPolished.unity`. The Free Roam build targets `Builds/SunsetBasin` if that folder is free. For a fresh automated output, pass `-echoBuildOutput <path-to-EchoMountain.exe>` to Unity batch mode; failed builds keep partial output in a sibling staging folder so the requested path remains retryable.

The rider lab remains at `Assets/EchoMountain/Scenes/PhysicsLabRider.unity`. Run **Window → General → Test Runner → PlayMode → Run All** in Unity. See the [QA record](docs/QA.md), [milestones](PLANS.md), and [snowboard research blueprint](agent-context/01-GAME-IDEAS.md).
