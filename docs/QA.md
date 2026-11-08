# QA evidence

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
