# Changelog

## 0.1.1 - 2026-07-17

- Aligned package metadata and samples with the portfolio contract; direct Deucarian dependencies now use the coordinated patch versions.
- Replaced duplicated Attack, Enemy, and Wave provider state, validation, reference, list, and summary code with shared Game Content Authoring primitives.
- Converted the stable Attack, Enemy, and Wave providers into pack-aware Attacks, Enemies, and Wave / Encounter lenses with immutable typed projection contracts, read-only external-pack previews, and preserved Project Content ScriptableObject workflows.
- Updated Attack, Enemy, and Wave authoring provider documentation/tests for the shared `Tools/Deucarian/Game Content Authoring` menu path and shared Deucarian card styling.
- Added `Deucarian.Attacks.Authoring` ScriptableObject recipe assets for root attack definitions, mechanics, targeting, delivery, status effects, and presentation.
- Moved the `Tools/Deucarian/Game Content Authoring` editor shell to `com.deucarian.game-content-authoring`; Attacks now contributes Attack, Enemy, and Wave providers to that shared window.
- Added editor providers for creating linked attack recipe assets under `Assets/GameContent/Attacks/{AttackId}`.
- Added recipe validation, presentation invocation helpers, and tests for runtime conversion, status hooks, missing optional presentation assets, and asset-creation validation.
- Added Enemy and Wave authoring providers with root assets plus stats, presentation, schedule, and entries section assets.
- Added dependency on `com.deucarian.game-content-authoring` for shared authoring validation display and editor asset/path helpers used by Attack, Enemy, and Wave creation flows.

## 0.1.0 - 2026-06-22

- Added attacks package for source registration, attack definitions, fixed-tick cooldowns, supplied-candidate target selection, direct attack intents, Combat request creation, snapshots, tests, docs, sample, and validation notes.
