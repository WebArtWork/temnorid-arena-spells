# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Aim from an `IAimSource` (VR hand) when one is enabled; `AimRay`, `AimsFromSource`, `InputBlocked`.
- `NextSlot` cycles spells; haptics on cast and on a hit taken.
- `SpellAimVisual`: casting-hand glow and aim line in VR.

### Added

- `ArenaCharacterVisuals`: cast, hit and death animations, accent color per combatant.

### Added

- Initial Unity package structure (`Runtime`, `Editor`, `Tests` assemblies).
- `SpellBook`, `SpellCaster`, `SpellBot`, simple spell effects.
