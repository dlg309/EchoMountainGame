# Echo Mountain project guidance

Read `docs/BUILD_BRIEF.md`, `PLANS.md`, `docs/STATUS.md`, and `docs/HANDOFF.md` before continuing. The repository root is the selected Unity project; preserve existing user changes and Unity GUIDs. Check Git and applicable nested AGENTS.md files before editing, and do not create another project or touch the separate running tutorial.

Use the requested Unity 6.3 LTS and URP, subject to the project's recorded version and security status. Keep runtime and editor code separate, use reproducible scene setup that does not overwrite user scenes, and test in the actual editor or an isolated safe checkout. Never force a project lock. Record real test/build commands, outcomes and limitations in `docs/QA.md`; update status and handoff at checkpoints. Draft lab entry points are documented in `docs/PHYSICS_LAB.md` and remain unverified until Unity can import the project.
