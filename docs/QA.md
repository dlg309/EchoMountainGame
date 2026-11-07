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
