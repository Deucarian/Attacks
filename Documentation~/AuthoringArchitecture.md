# Authoring provider composition

The Attack, Enemy, and Wave V2 provider view composes package-local collaborators. These are
stateless operations over the existing provider session, not another draft store.

| Responsibility | Owner |
| --- | --- |
| Selected item layout and detail page routing | Existing ProviderV2View |
| Mutable editor session, preview state and reset hooks | Existing ProviderV2State |
| Draft initialization, save/revert/create commands and temporary validation lifetime | AuthoringSession |
| Asset-to-draft mapping and change fingerprints | AuthoringDraft, or existing mapping adapter |
| Domain field configuration and edit controls | AuthoringFields |
| Creation page navigation and review rendering | AuthoringWizard |
| Library search/card presentation | AuthoringLibrary and ProviderV2ListItem |
| Preview controls and preview snapshot construction | AuthoringPreview and ProviderV2PreviewModel |
| Human-readable domain summaries | AuthoringSummary |

The session command functions dispatch to AttackRecipeAssetCreator, EnemyDefinitionAssetCreator, and WaveDefinitionAssetCreator.
These existing adapters remain authoritative for validation, asset mutation,
Undo, persistence and transient object cleanup. The shared Game Content Authoring
package still owns cross-window editing, pack access and locking.

WaveAuthoringEntryCommands owns row reorder and duplication. Moving a row preserves
its EntryId; copying a row issues a new EntryId while retaining authored values.
Attack/Enemy mapping continues to use AttackGameContentPreviewSelection; the new
draft collaborators own fingerprints without adding an alternative asset mapper.

## Compatibility and verification

- Provider IDs and lens IDs (attack / enemy / wave) retain their existing contracts.
- Pack-aware selection remains in the original provider registration/facade;
  external records remain read-only and keep their canonical pack identity.
- Field labels, foldout IDs, page order, action behavior, serialized asset types,
  GUIDs and public runtime APIs are unchanged.
- Existing internal V2 helper entry points forward to their new owners so current
  regression tests and editor consumers keep compiling.
- Composition tests cover independent drafts/session resets and domain-specific
  edit/preview behavior. Existing tests retain registration, pack lenses, asset
  validation/update, preview, and gameplay coverage.

This increment decomposes the V2 surface responsibility cluster. Older combined
provider/asset-creator and preview-source files retain their existing boundaries;
they are not claimed as fully decomposed by this change.
