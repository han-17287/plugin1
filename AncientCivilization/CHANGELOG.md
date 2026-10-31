# v0.6 — Aimed Wire Controls

- Bind left mouse to pointer-aimed wire, Shift to melee and Alt/C to dodge.
- Add Tab switching between automatic swing and direct zip-pull, preserving the mode preference.
- Limit snapping to the cursor neighbourhood and add direction-unrestricted fixed-surface targets in zip mode.
- Add a pixel reticle with target/cooldown feedback, mode-specific shape and a menu pointer.
- Restore the native cursor outside the Game view, on focus loss, disable and teardown.

Unity input handling, cursor behaviour, wire physics and rendering remain unverified here.

# v0.5 — Impact and Motion

- Add dedicated anticipation/contact, heavy-strike, jump and hurt sprites with stable actor pivots.
- Apply melee damage on contact timing and add a short next-attack input buffer.
- Add short global hit emphasis, impact rings, landing feedback and pooled afterimages/impact pixels.
- Add readable evade/absorb windup indicators, cooldown meters and the nearest-core locator.
- Add a consistent HUD/button theme and trailing health feedback.
- Separate camera shake from its follow position and align to actual display pixels.
- Refine synthesized cues and add quiet original ambience with an F7 mute preference.

Unity C#/shader compilation, rendering, input responsiveness and combat remain unverified here.

# v0.4 — Neon Sentinels

- Add a dedicated high-resolution collision-aligned rooftop texture, closer adaptive camera and 1080p defaults.
- Add additive local lights, character rim shading, subtle screen glow/grade, distant rain, haze and foreground cables.
- Add an F6 Enhanced/Light presentation option with supported-shader fallbacks.
- Replace shared enemy art with six distinct silhouettes and a dedicated thrown scrap sprite.
- Add telegraphed lancer lunges, gunner three-round bursts, ballistic scrap throws, flying drone fans and directional shield guards.
- Add guard breaks with combo finishers, particle bursts and motorcycle explosions; add vulnerable recovery windows.

Unity C#/shader compilation, rendered appearance and combat remain unverified here.

# v0.3 — Particle Wire

- Add an actual maximum-length DistanceJoint2D tether with automatic reeling and immediate forward/upward launch.
- Automatically release on the rising side of the arc; preserve aerial momentum and count distinct-anchor chains.
- Allow elevated fixed rooftop surfaces as well as preset anchors, with live target highlighting and line-of-sight checks.
- Add a bright cable core, translucent sheath, energy pulses, rotating hook and latch/release particles.
- Clean up the joint on dash, jump, explosion, death and scene teardown.

Unity import, compilation and physical movement remain unverified in this cloud environment.

# v0.2 — City of the Warden

- Replace placeholder pixel grids with shipped city, hero and world PNG artwork.
- Add animated hero poses, detailed repeating rooftops, parallax, trails, impact effects and synthesized cues.
- Add a three-hit combo, directional particle burst, invulnerable dash and buffered/coyote-time jumps.
- Add four upgrades with core gates and separate technology-fragment currency.
- Add persistent core/upgrades/checkpoint progress, healing beacons and guarded save reset.
- Add a telegraphed, two-phase Sanctum Warden encounter and final gate sequence.
- Preserve full-speed absorption and immediate automatic wire acceleration.

This is source-only. Unity compilation, Play Mode and builds remain unverified.
