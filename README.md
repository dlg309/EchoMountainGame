# Echo Mountain

Glacier Pass is an 860 m snowboarding course for Windows. A major Snowboard Starter-inspired redesign of its riding physics, rider animation, camera, terrain, graphics and UI is the active development target (2026-11-19). The current released build still has the existing Echo Mountain presentation; the redesign is not implemented or verified yet. Ride the smooth **Pine line**, take the marked rollers on the **Summit line**, or use **Jump practice** for a shorter Summit run with a 250-point target.

## Play Glacier Pass

For the latest locally recorded R2 candidate, run `Builds/GlacierPass-Polished-r2/EchoMountain.exe` from its full build folder. The final R2 build passed automated tests/build but still needs manual visual and record-reopen checks; `Builds/GlacierPass-Integrated` is an older build. Choose a line and select **Drop in**, or select **Jump practice**. Results show time, top speed, and banked trick points. Use **Retry** to repeat the same route or practice segment.

| Action | Keyboard | Gamepad |
| --- | --- | --- |
| Choose a line | Left/Right | D-pad Left/Right |
| Drop in or retry | Enter | South button |
| Start jump practice | P | North button |
| Steer | A/D or Left/Right | Left stick |
| Brake | S or Down | Left trigger |
| Pop | Space | South button |
| Spin in the air | Z/C | Right stick Left/Right |
| Hold Indy / Melon | Q/E | Right/Left bumper |
| Reset | R | East button |
| Pause or resume | Escape | Start |

The on-screen Pause button freezes the run and trick timer. Sound can be switched off from the menu or ride display. Gamepad actions have been tested with a virtual device. Current source pauses when the active controller disconnects or the window loses focus; reconnecting does not resume automatically. Ride prompts follow keyboard/gamepad use. Camera obstruction handling keeps the view in front of solid obstacles; heavily enclosed spawn points still need manual review.

### Scoring

From Results, select **Replay [V / gamepad West]** to watch the current completed attempt. Space/South pauses; arrows/D-pad seek five seconds; use the slider and 0.5x/1x/2x buttons for review. H/North hides or restores the overlay; Escape/East returns to Results. Replay uses recorded motion and poses, disables collisions and riding, and leaves scores and records unchanged. Recordings are limited to three minutes and remain in memory until retry or exit. Saved ghosts and replay files are still pending.

Completed runs also update local previous, fastest, and highest-score records for each line and jump practice. The practice target, sound preference, and last-selected line survive restart. Saves use a versioned JSON file with a backup under Unity's persistent data folder; invalid or incompatible data produces a visible notice. Editor tests use separate temporary stores.

A completed 180 earns 180 base points and a 360 earns 360. Continuously holding an Indy or Melon for 0.15 seconds adds 80 or 90 points. Release the spin input before touching down so the board can settle. The in-air display shows rotation and pending base points.

A clean landing banks the points. A recovered landing earns half and ends the chain. A bail earns zero. Different clean tricks within eight seconds build a multiplier from ×1 to ×1.75. Consecutive repeats earn 60% of their usual value and restart the chain. For example, a clean 180 Indy starts at 260 points, followed by a clean 360 Melon for 562 points at ×1.25.

## Redesign target and status

The playable Snowboard Starter reference is local at `C:\Users\diego\Downloads\SnowboardStarter\20210215 Ver 0.4 Windows`. The user's menu and riding screenshots define a warm sunset, pale layered snow, low-poly firs, blue-gray distant mountains, dark-outlined rider/board, readable behind-rider camera, ramps/rails/signs and minimal HUD. The intended replacement includes acceleration, carve/skid, brake, jumps, air tricks, landings and bails; existing Pine/Summit, practice, scoring, pause/retry, records and replay must be adapted and verified. See `PLANS.md` for the active staged plan. The compiled reference does not include editable Unity scenes/prefabs/scripts. Keep extracted third-party assets out of the public repository.

## Edit, build, and test

Open the repository with Unity **6000.3.28f1** and load `Assets/EchoMountain/Scenes/GlacierPass.unity`. The Unity menu build targets `Builds/GlacierPass-Replay` if that folder is free. For a fresh automated output, pass `-echoBuildOutput <path-to-EchoMountain.exe>` to Unity batch mode; failed builds keep partial output in a sibling staging folder so the requested path remains retryable.

The rider lab remains at `Assets/EchoMountain/Scenes/PhysicsLabRider.unity`. Run **Window → General → Test Runner → PlayMode → Run All** in Unity. See the [QA record](docs/QA.md), [milestones](PLANS.md), and [snowboard research blueprint](agent-context/01-GAME-IDEAS.md).
