# Review 2 account extension — implementation plan

Source: `ai-game-dev-research/docs/spec/review-2-creative-spec.md` (main, read 2026-11-26). The SPEC calls the account architecture a candidate, not a selected stack. The 2026-11-26 implementation request adopts the candidate for a local prototype: Unity client, ASP.NET Core HTTPS API, server-owned SQLite, and ASP.NET Core Identity password hashing. Public hosting and an external identity provider remain unselected.

Tracking issue: [EchoMountainGame #2](https://github.com/dlg309/EchoMountainGame/issues/2). The research SPEC's older 41-test description was historical; this checkout started from the later 65-test branch with Free Roam and customization.
Implementation checkpoint: `7a955a2` on `experiment/carve-brake-r3`.

## Audit before edits

| SPEC story | Baseline | Work |
| --- | --- | --- |
| Account access and settings | Missing | Registration, login, logout, private profile, display-name update, session expiry/revocation. |
| Ride and results | Implemented | Preserve `MountainRun`, physics, scoring, replay, and Free Roam. |
| Completed-run save | Local summary only; no account history | Separate remote repository boundary; one explicit save per completed attempt; retry-safe key. |
| Private history and bests | Missing | Owner-filtered API queries and client history view. |
| Keyboard and phone | Desktop keyboard menu exists; no account UI | Keyboard-operable Unity account UI. Phone game remains outside SPEC target; check research-site phone layout separately. |

## Phases and gates

1. Add the small API and tests for credential handling, expiring sessions, ownership, validation, duplicate saves, and profile updates. Bind locally by default; do not embed secrets.
2. Add Unity account client and a repository boundary. Capture immutable finished-run data once in `MountainRun` and leave local records unchanged. Add sign-in, save, history, and settings UI with keyboard support.
3. Run API tests, Unity tests, an isolated Unity build, and the complete player workflow. Log each observed outcome in `docs/QA.md`. Do not mark unexecuted checks passed.

Run scores are client-reported and private/unverified. The API bounds and validates data but cannot prove gameplay integrity.

## Traceability

| SPEC criterion | Implementation | Verification |
| --- | --- | --- |
| Registration, sign-in, sign-out, private session | `AccountService/Program.cs`, `Assets/EchoMountain/Runtime/AccountPanel.cs` | `AccountService.Tests/AccountTests.cs`, `ApiTests.cs` |
| Completed run, duplicate retry, measurements and tricks | `MountainRun.cs`, `TrickScore.cs`, `AccountPanel.cs`, API `runs` table | `MountainRunTests.cs`, `MountainTrickTests.cs`, `ApiTests.cs` |
| Owner-only history and bests | API owner-filtered `Run`, `Runs`, `Bests`; account history UI | `ApiTests.cs` cross-account and signed-out checks |
| Profile update | API `/v1/me`; account settings UI | `ApiTests.cs` valid/invalid/cross-account checks |
| Offline ride regression | Existing scenes, rider and local records preserved | Full Unity PlayMode suite; see `docs/QA.md` |

Local service uses a 12-hour revocable bearer session held only in game memory, HTTPS only, parameterized SQL, password hashing, request size and rate limits. Public deployment still needs an external identity/provider decision, consent and deletion rules, backup/restore plan, TLS hosting, and a security review. Unity [6000.3.29f1 release notes](https://unity.com/releases/editor/whats-new/6000.3.29f1) report a FreeType security fix after the project's pinned 6000.3.28f1; a same-stream patch upgrade should be tested from a rollback checkpoint before public release.
