# Temnorid Arena: Spells

`temnorid-arena-spells` is the Temnorid Arena spells instead of guns: casting, mana, and hits.

It is a game package: it holds Temnorid Arena's own rules and content, built on the **IT-Kamianets 3D Engine** packages.

### Logic

Spells take the place of weapons. Each has a mana cost, a cooldown and an effect; v0.1 has four, in hotbar order (keys 1-4, cast with the main button): **Fireball** (projectile), **Frost nova** (burst around the caster), **Arcane shield** (absorbs damage), **Blink** (short teleport). Balance is data in `SpellBook`.

The owner asks to cast; the server checks the caster is alive, the cooldown and the mana, then casts: it flies projectiles, applies damage through `Combatant` and teleports, so hits cannot be faked. Every client sees the effects. Cast sounds play through `3d-unity-audio` when events `spell.<id>.cast` exist.

Bots live here because they fight with spells.

### Status

Version 0.1.

* `SpellBook` / `SpellDefinition`: fireball (25 damage, 12 mana, 0.5 s), frost nova (35 damage within 6 m, 30 mana, 6 s), arcane shield (50 for 5 s, 25 mana, 12 s), blink (9 m, stops at walls, 20 mana, 5 s).
* `SpellCaster`: slot selection, `TryCast` (owner) → server `ServerCast` with cooldown and mana checks (0.15 s slack for remote players), server-side projectiles, nova, shield, blink; effects on every client (ball, ring, bubble, flashes); cooldowns for the HUD.
* `ArenaCharacterVisuals`: the model's cast animation per spell (triggers `CastProjectile`, `CastNova`, `CastShield`, `CastBlink`), `Hit`, `Dead`, and the accent color (you cyan, other players gold, bots red) through a property block; driven by networked events, so everyone sees the same.
* Aim: the screen centre, or an enabled `IAimSource` next to the caster (a VR hand: `AimRay`, `AimsFromSource`). Projectiles still start at the eyes and the server checks every cast as before, so VR and flat players are equal.
* VR: `NextSlot` (right stick click) cycles spells; `InputBlocked` stops the trigger casting while the casting hand points at a panel; the controllers buzz on a cast and on a hit taken (`EngineInput.Haptic`).
* `SpellAimVisual`: in VR, a glow at the casting hand in the selected spell's colour (dim while cooling down) and a short aim line; hidden on flat screens, for others and while dead.
* `SpellBot`: server-side bot: nearest enemy, approach and strafe, hop when stuck, fireballs with aim error when in sight, nova up close, shield when hurt, blink away when low.

Checked in Play mode with simulated XR controllers: the trigger casts along the hand ray, a right stick click selects the next spell, a spell picked on the wrist panel is cast next. Earlier: a fireball hits a bot for 25, frost nova for 35 at 2 m, the shield gives 50, blink moves 9 m; 5 bots fight each other and the player (66 kills in 30 s).

### Roadmap

* Spell definitions as data
* Casting: cast time, mana, cooldown
* Projectile, area, shield and movement spells
* Server-side hit checks
* Damage and health
* Mana regeneration
* Cast and impact effects and sounds
* Spell balance settings
