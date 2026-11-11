# Snowboard game research and Echo Mountain integration

Status: research and implementation blueprint, 2026-11-11. This note covers representative snowboard game styles rather than every released title. `[Documented]` means an official game, developer, or Unity source states the cited fact, or the named Echo Mountain source file implements it. `[Proposed]` is an original design for Echo Mountain. `[Assumption]` needs local playtesting or tuning. Commercial games' internal physics code and numerical settings are unknown.

## Target game teardown summary

| Style and reference | Confirmed player-facing pattern | Useful Echo Mountain lesson |
| --- | --- | --- |
| Arcade racing and spectacle, *SSX* | [Documented] EA describes descending mountains to Race, Trick, and Survive, and the series as action racing with oversized stunts. [EA SSX](https://www.ea.com/games/ssx), [EA release](https://news.ea.com/press-releases/press-releases-details/2012/Defy-Reality-Own-the-Planet-EA-Sports-SSX-in-Stores-Now/default.aspx). | [Proposed] Keep separate timed and trick objectives on the same authored run. Show the objective before drop-in and the result immediately after. |
| Freeride exploration and sharing, *Steep* | [Documented] Ubisoft describes open slopes, skill challenges, editable replays, and created run sharing. [Ubisoft overview](https://news.ubisoft.com/en-us/article/7jAdiKBdiUehSiZK3OWO2Z/steep-action-sports-go-open-world). | [Proposed] Keep compact selectable lines; add a local previous/best run before any online system. |
| Expressive snowboard control, *Shredders* | [Documented] The developer describes *Shredders* as a snowboard-focused game inspired by snowboard films and the *Amped* series. The publisher describes style and a high degree of rider control. [Developer FAQ](https://i-illusions.com/faq/shredders), [publisher page](https://www.microids.com/us/shredders-us/). | [Proposed] Make line choice, pop timing, body rotation, grabs, and landing legible as separate decisions. Do not infer proprietary control equations from marketing copy. |
| Skill-based park sessions, *The Snowboard Game* | [Documented] Its developer-authored Steam listing describes a skill-based snowboard game with mountain and snow-park locations, plus session spawn points and placeable jumps. [Steam listing](https://store.steampowered.com/app/803830/The_Snowboard_Game/). | [Proposed] Expand practice into repeatable, short sessions with explicit objectives and feedback. |
| Accessible freestyle, *Riders Republic* | [Documented] Ubisoft describes freestyle as rotation, tricks, and grinds, with automatic landing help and a manual option. [Ubisoft Q&A](https://www.ubisoft.com/en-sg/game/riders-republic/news-updates/2HFvoOtBvWRtrlXOF8ZXjo/answering-your-riders-republic-questions), [accessibility spotlight](https://news.ubisoft.com/en-us/article/6zMpXnJAI44AWNqmm2N6LK/accessibility-spotlight-riders-republic). | [Proposed] Offer an optional landing assist after the current unassisted rule has stable tests. Assists should be visible in results and records. |

**Core loop.** [Proposed] Choose a line or practice target → read terrain and set speed → carve, pop, spin or grab → land or recover → finish and compare → retry or replay. [Documented] The current project already covers line choice, two descents, a jump practice entry, landing-based score, results, and retry in `MountainRun.cs`, `MountainCourse.cs`, `LabRider.cs`, and `TrickScore.cs`. [Documented] The newly merged runtime also has local route records, bounded in-memory replay, and cosmetic snow spray in `LocalRunStore.cs`, `RunRecording.cs`, `MountainReplay.cs`, and `MountainSnowSpray.cs`; ghosts, rails, and a physical halfpipe remain open. The combined branch passed 32/32 PlayMode tests in the isolated Unity project; see `docs/QA.md`.

**Game feel.** [Documented] `LabRider.cs` implements a 0.15 s jump input buffer, 0.07 s contact grace, speed-dependent steering torque, lateral grip, braking, impact-based landing outcomes, and timed recovery. `LabCamera.cs` follows travel direction; `MountainRideAudio.cs` responds to speed, skid, and landing. [Assumption] Terrain-dependent grip and sound are likely to make the lines more readable, but perceived improvement requires playing the Windows build with sound and physical controls.

## Algorithmic breakdown and math specs

[Documented] The rider uses a Rigidbody, four filtered ray probes, projected forward and lateral velocity, and `ForceMode.Acceleration` for grip and rolling resistance. Pop uses `ForceMode.VelocityChange`. See `Assets/EchoMountain/Runtime/LabRider.cs`. [Documented] Unity documents the different velocity effects of force modes in [Rigidbody.AddForce for Unity 6.3](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody.AddForce.html).

Current ground steering can be summarized as `v_side = dot(v, cross(n, f))`, `a_grip = clamp(-v_side * edgeGrip, -maxGripAcceleration, +maxGripAcceleration)`, and `a_roll = -sign(v_forward) * min(|v_forward|/dt, rollingResistance + brake * braking)`. Here `n` is the probe normal and `f` is the board forward projected onto the contact plane. [Documented] This matches the implementation in `LabRider.cs`; it is a summary of our code, not another game's algorithm.

[Proposed] A simple vertical launch estimate is `y(t) = y0 + v0*t + 0.5*g*t?`, using the contact normal component of pop velocity and gravity along that axis. It is only a first-order check: the actual Rigidbody trajectory includes slope orientation, drag, rotation, and collision. Test measured airtime in Unity rather than awarding tricks from this estimate.

**First integration feature: authored surface response.** [Proposed] Assign compact course regions a surface kind such as packed snow, powder, and ice. Sample one kind from the rider's contact position each fixed tick. Map it to grip, roll, and brake multipliers. Apply those multipliers only while supported, and expose the active kind to the HUD and sound. Keep mesh collision and normal calculation unchanged. [Assumption] Start with packed `(1,1,1)`, powder `(0.85,1.8,1.25)`, and ice `(0.55,0.45,0.5)` for `(grip, rolling resistance, braking)`; these are prototype tuning ratios, not measured snow properties.

```text
Input actions ──> LabRider.Update (sample edges, steer, brake)
                           │
Physics contact ──> four probes ──> support + normal
                           │
              SurfaceMap.Sample(position)
                           │
              LabRider.FixedUpdate ──> force + torque ──> Rigidbody
                           │                                 │
                 HUD/audio surface label           airborne / landing
                                                     │
                                            TrickScore.Land + MountainRun
                                                     │
                                              Results / retry

Ready ── drop-in ──> Riding grounded ── pop/lip ──> Riding airborne
  ^                         ^                              │
  │                         └──── clean/recover landing ───┘
  │                         └──── bail → recovery → reset
  └──── choose line <──── Results <──── finish while grounded
```

**State and event rules.** [Proposed] Surface classification is sampled during supported physics ticks. A brief airborne interval retains the last *displayed* kind but applies no surface force. Landing resumes sampling immediately, before force application. Reset clears the display state to the spawn's kind. A finish remains pending until the board lands, as now verified in the gameplay regression. Boundary selection is deterministic from world coordinates and line; a thin blending band can be considered after a discrete version passes tests. [Assumption] A 1–2 m visual transition band may look better, but the width is untested.

**Input and data.** [Documented] `LabRideControls.cs` maps keyboard and gamepad steer, brake, pop, reset, pause, spin, Indy, and Melon through Unity Input System actions. [Proposed] Surface response introduces no new input. Store authoring data as a serializable `SurfaceZone` with `kind`, `minX`, `maxX`, `minZ`, `maxZ`, and stable priority; use a packed-snow default. Preserve these data independently of trick and record state. [Proposed] If/when records are added, save course ID, line, mode, time, points, assist setting, and schema version, then compare only runs with matching course/mode/assist settings.

## Echo Mountain codebase mapping and prerequisites

| Existing module | Current responsibility | Change for surface slice |
| --- | --- | --- |
| `LabRider.cs` | [Documented] probes, support, forces, pop, landing, recovery | [Proposed] sample a surface after valid support; multiply existing bounded accelerations |
| `MountainCourse.cs` and `MountainBuilder.cs` | [Documented] authored height function and Glacier Pass scene | [Proposed] place visible powder/ice patches and a small serializable zone map in the course scene |
| `MountainRun.cs` | [Documented] Ready/Riding/Results/Replay, selection, retry, practice, and record finish | [Proposed] show active surface and retain result comparability |
| `MountainRideAudio.cs`, `MountainSnowSpray.cs`, and `LabHud.cs` | [Documented] synthesized ride/landing sound, cosmetic spray, and HUD | [Proposed] parameterize sound/spray and show a short surface label |
| PlayMode tests | [Documented] rider physics and actual course flows | [Proposed] add a deterministic zone, grip, transition, airborne, and reset regression |

[Documented] URP and Unity 6000.3.28f1 are the project baseline, with a 1/60 s fixed step. [Documented] The course is currently one authored height-function mesh with two lines and normal-based contact. [Proposed] Keep the first surface slice coordinate-authored; it requires no terrain system or mesh rebuild. The main architectural concern is keeping a surface lookup out of landing evaluation and scoring so those rules remain deterministic and independently testable.

## Implementation blueprint

[Proposed] Add a pure `SurfaceRules` utility in `Assets/EchoMountain/Runtime/SurfaceRules.cs`. Initial code for the classifier and coefficients follows. Coordinates and multipliers are provisional assumptions. Later, replace literals with serialized zones owned by the scene without changing `LabRider`'s force call sites.

```csharp
using UnityEngine;

namespace EchoMountain.Riding
{
    public enum SnowSurface { Packed, Powder, Ice }

    public readonly struct SurfaceResponse
    {
        public readonly SnowSurface Kind;
        public readonly float Grip, Roll, Brake;

        public SurfaceResponse(SnowSurface kind, float grip, float roll, float brake)
        {
            Kind = kind;
            Grip = grip;
            Roll = roll;
            Brake = brake;
        }
    }

    public static class SurfaceRules
    {
        public static SurfaceResponse Sample(Vector3 world)
        {
            // Prototype authored zones inside Glacier Pass. Validate against mesh bounds.
            if (world.z >= 330f && world.z < 390f && world.x > 3f)
                return new SurfaceResponse(SnowSurface.Powder, 0.85f, 1.8f, 1.25f);
            if (world.z >= 530f && world.z < 575f && world.x < -3f)
                return new SurfaceResponse(SnowSurface.Ice, 0.55f, 0.45f, 0.5f);
            return new SurfaceResponse(SnowSurface.Packed, 1f, 1f, 1f);
        }
    }
}
```

[Proposed] In `LabRider.FixedUpdate`, after `contact` and `normal` have been resolved and before forces, use `var surface = SurfaceRules.Sample(body.position);`. Replace the existing grip expression with `Mathf.Clamp(-slip * edgeGrip * surface.Grip, -maxGripAcceleration * surface.Grip, maxGripAcceleration * surface.Grip)`. Replace `rollingResistance + brake * braking` with `rollingResistance * surface.Roll + brake * braking * surface.Brake`. Keep the existing `min(|v_forward|/dt, ...)` bound to prevent a single step reversing longitudinal velocity. Expose `SnowSurface? ActiveSurface` for presentation; assign it only on supported ticks, clear it in `ResetRider`, and leave airborne physics untouched. [Proposed] Use separate visuals and audio parameters sourced from `ActiveSurface`, without affecting score.

[Proposed] Implement in small verified slices: (1) pure classifier and tests; (2) rider force integration and flat fixture tests; (3) authored visible zones on Glacier Pass and full course regression; (4) surface HUD/audio feedback and Windows playtest. Check Unity's actual collision material and scene setup before choosing final zone boundaries. [Assumption] The example coordinates lie in reachable parts of each line; automated course traversal and visual inspection must confirm that.

## Testing and validation checklist

1. [Proposed] At packed/powder/ice boundaries, sample exact coordinates on either side and verify one stable kind per tick, including reset to spawn.
2. [Proposed] On the same flat fixture and initial speed, verify lateral speed decays faster on packed snow than ice, and forward speed decays faster in powder than packed snow. Set tolerances after measuring a first run.
3. [Proposed] Pop through a surface boundary and confirm airborne trajectory has no added ground force; landing selects the new surface on the first valid supported tick.
4. [Proposed] Traverse both full Glacier Pass lines with virtual controls, then verify finish/retry, landing points, and no unintended boundary reset. Compare the new run durations with the recorded 84.49 s Pine and 87.14 s Summit baselines.
5. [Proposed] Launch a fresh Windows build, inspect the colored patches/HUD and listen to the sound transitions with a real keyboard and controller. The prior automated suite cannot validate perceived traction, sound, or hardware feel.

## Research limits and next decisions

[Documented] The sources above establish visible game features and developer descriptions. They do not disclose proprietary force equations, state machines, or tuning constants. [Proposed] Echo Mountain should use the existing Unity architecture and independently tuned behavior. [Assumption] Whether a given style improves fun requires controlled playtests, particularly approach speed, landing fairness, camera readability, and sound. [Proposed] After surface response, extend the existing local records and in-memory replay into an optional previous-run ghost. Rails and a physical halfpipe need their own collision and state tests.
