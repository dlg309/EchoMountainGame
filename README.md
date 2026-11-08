# Echo Mountain

Unity 6.3 LTS / URP snowboarding project. Current state: **physics laboratory in progress**, not a finished game. See [PLANS.md](PLANS.md) and [docs/STATUS.md](docs/STATUS.md) for milestones and the latest checkpoint.

## Open the lab

Open this repository folder in Unity Hub with Unity **6000.3.28f1**. The original diagnostic scene is `Assets/EchoMountain/Scenes/PhysicsLab.unity`; the newer contrast/marker version is `Assets/EchoMountain/Scenes/PhysicsLabReadable.unity`. Open the scene in the editor and press Play. The rider is a diagnostic primitive, not final character art.

Controls: **A/D or left/right arrows** steer, **S or down arrow** brakes, **Space** pops, and **R** resets. A gamepad mapping is present but has not been verified on physical hardware. The overlay is for physics diagnostics. There are no menus, scoring, saves, complete landings or authored mountain yet.

A previously built Windows diagnostic executable is at `Builds/PhysicsLab-Repaired/EchoMountainLab.exe` if present locally; build outputs are ignored by Git. It predates the higher-contrast scene. For a new development build use **Echo Mountain > Build Windows physics lab** after Unity imports the project; the command refuses to overwrite `Builds/PhysicsLab` if that folder already exists. Archive an old build to a safe location before rebuilding. Check `docs/QA.md` for tests actually run and remaining limitations.
