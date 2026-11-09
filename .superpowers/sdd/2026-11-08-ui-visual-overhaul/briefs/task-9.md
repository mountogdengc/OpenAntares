### Task 9: Map dressing and overlays

**Files:** `GalaxyMap.cs`, new `MapOverlays.cs`, theme `GalaxyMap/*` constants and icons.

- [ ] Add failing `MapShowsNoUnknownFacts` (rings/flags/overlays only from requester-visible data), `RouteEtaMatchesExistingCalculation`, `OverlayTogglesIssueNoCommands`, `LabelsStillUseM13Layout`.
- [ ] Draw star glow and a backdrop derived from existing `GalaxyBackgroundPoints` (no new randomness); ownership rings and territory wash in empire colors; fleet flags with counts; selected-route dashed line with ETA badge. Overlays: Owners, Range, Threat, Habitat, each enabled only when the existing view provides its data. Single neutral star tint per D1.
- [ ] Rerun cases plus Rival/Expansion/Conflict scenarios; capture frames at all sizes.
