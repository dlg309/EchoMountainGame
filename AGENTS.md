# Echo Mountain project guidance

Read `docs/BUILD_BRIEF.md`, `PLANS.md`, `docs/STATUS.md`, and `docs/HANDOFF.md` before continuing. Preserve existing user changes and Unity GUIDs. The Unity project location is not yet established; do not create a second project or touch the running tutorial project. Check Git and applicable nested AGENTS.md files before editing.

Use Unity 6.3 LTS and URP as specified by the brief, subject to the actual project's recorded version and security status. Keep runtime and editor code separate, use reproducible scene setup that does not overwrite user scenes, and test in the actual editor or an isolated safe checkout. Never force a project lock. Record real test/build commands, outcomes and limitations in `docs/QA.md`; update status and handoff at checkpoints. Build/test entry points are pending project discovery.
