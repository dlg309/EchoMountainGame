# Echo Mountain

Glacier Pass is the first connected Windows course in Echo Mountain. Ride an 860 m descent on the smooth **Pine line** or follow the **Summit line** over three marked rollers. The run ends at a finish arch with your time, top speed, and a retry option.

## Play Glacier Pass

Run `Builds/GlacierPass/EchoMountain.exe` from its full build folder. Choose the blue Pine line or amber Summit line and select **Drop in**. Follow the markers to the finish. Use **Retry** to ride the same line again or **Choose line** to switch.

| Action | Keyboard | Gamepad |
| --- | --- | --- |
| Choose a line | Left/Right | D-pad Left/Right |
| Drop in or retry | Enter | South button |
| Steer | A/D or Left/Right | Left stick |
| Brake | S or Down | Left trigger |
| Pop | Space | South button |
| Reset | R | East button |
| Pause or resume | Escape | Start |

The pause panel also has Resume and Reset Run buttons. The course uses the articulated lab rider and travel-aware camera. Gamepad actions have been tested with a virtual device.

## Edit, build, and test

Open the repository in Unity Hub with **6000.3.28f1** and load `Assets/EchoMountain/Scenes/GlacierPass.unity`. Use **Echo Mountain → Build Windows Glacier Pass** to produce the course build. The menu preserves existing output folders.

The earlier physics lab remains at `Assets/EchoMountain/Scenes/PhysicsLabRider.unity`. Its Windows build uses **Echo Mountain → Build Windows lab playtest**.

Run **Window → General → Test Runner → PlayMode → Run All** in the editor. The course tests cover both descents, timing, finish, and retry. See the [QA record](docs/QA.md) and [milestones](PLANS.md).
