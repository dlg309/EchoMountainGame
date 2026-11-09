# Echo Mountain

A Windows snowboarding project built with Unity 6.3 LTS and URP. The current playtest is a physics course with a slope, takeoff, landing, runout, and automatic recovery. The rider is an intermediate segmented rig with board-mounted boots and a pose that responds to steering and airtime.

## Play the lab

Run `Builds/PhysicsLab-RiderPlaytest/EchoMountainLab.exe` from its full build folder. To run in the editor, open the repository in Unity Hub with **6000.3.28f1**, load `Assets/EchoMountain/Scenes/PhysicsLabRider.unity`, and press Play.

| Action | Keyboard | Gamepad |
| --- | --- | --- |
| Steer | A/D or Left/Right | Left stick |
| Brake | S or Down | Left trigger |
| Pop | Space | South button |
| Reset | R | East button |
| Pause or resume | Escape | Start |

The pause panel also has Resume and Reset Run buttons. Gamepad actions have been checked with a virtual device. Physical gamepad testing is pending.

## Build and verify

Use **Echo Mountain → Build Windows lab playtest** to build the rider scene without the development profiler. The menu preserves existing output folders. Use **Echo Mountain → Build Windows physics lab** for a development build of the earlier HUD scene.

Run **Window → General → Test Runner → PlayMode → Run All** in the editor. The tests cover support, simulated controls, landings, reset, the descent, the camera, and the rider pose. See the [QA record](docs/QA.md) and [milestones](PLANS.md).
