# Changelog

## Unreleased

- Added `Deucarian.Attacks.Authoring` ScriptableObject recipe assets for root attack definitions, mechanics, targeting, delivery, status effects, and presentation.
- Added `Deucarian/Game Content Authoring` editor wizard for creating linked attack recipe assets under `Assets/GameContent/Attacks/{AttackId}`.
- Added recipe validation, presentation invocation helpers, and tests for runtime conversion, status hooks, missing optional presentation assets, and asset-creation validation.
- Added Enemy and Wave authoring providers with root assets plus stats, presentation, schedule, and entries section assets.
- Added shared authoring validation and editor asset/path helpers used by Attack, Enemy, and Wave creation flows.

## 0.1.0 - 2026-06-22

- Added attacks package for source registration, attack definitions, fixed-tick cooldowns, supplied-candidate target selection, direct attack intents, Combat request creation, snapshots, tests, docs, sample, and validation notes.
