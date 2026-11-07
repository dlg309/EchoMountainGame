# Physics lab: proposed first verification matrix

This is a test design, not an implementation or a test result. Tune dimensions and tolerances after the actual project is found, while keeping a reproducible, owned scene separate from user scenes. Baseline proposal: 1 unit = 1 metre, gravity from project settings, 1/60 s fixed timestep, interpolated primary body, fixed initial pose and velocity, and explicit physics inputs. Preserve a flat control area followed by an independently testable slope, crest, takeoff and landing, with boundary/reset and visible developer diagnostics.

| Experiment | Compare / observe | Failure to investigate |
| --- | --- | --- |
| Resting support | settle on flat; measure vertical drift, speed, normal and contact state over several seconds | falling through, persistent jitter, NaNs or ground-state chatter |
| Slope acceleration | released board versus flat control; same setup and duration | no meaningful downhill acceleration or invented constant speed |
| Edge and brake | equal initial speeds, zero/left/right input and brake | indistinguishable left/right paths, gain in energy, unbounded grip or brake increasing speed |
| Crest and jump | compare no-pop and buffered pop from same approach; measure actual height/time, takeoff and landing | sticky lip, fixed airtime, repeated jump, phantom landing |
| Landing | compare forward and switch alignment, lateral skid and excessive impact/rotation | valid switch rejected, duplicate landing events, invalid landing accepted |
| Reset and boundary | repeated resets from moving, airborne and out-of-bounds states | residual velocity/input/contact, repeated collision, invalid state |
| Frame rates and seams | same fixed-tick inputs at 30, 60, 120 render FPS; repeat seam and high-speed cases | divergent fixed-step outcome outside declared tolerances, tunneling or contact chatter |

Use PlayMode tests for quantitative state and integration and observe actual gameplay separately. Establish numerical thresholds against measured baseline before asserting a pass; never change thresholds solely to hide a failure. Verify physical contact rather than trusting only probe state. Report input injection and simulation conditions, actual test counts/results and limitations.
