### Task 8: Workspaces replace dialogs; colony ledger

**Files:** new `WorkspaceFrame.cs`, `ColonyLedger.cs`, `WorkerFigureRow.cs`; `GameScreen.cs`, `ColonyList.cs`, `ColonyPanel.cs`, `ResearchBar.cs`, `ShipDesignDialog.cs`, `DiplomacyDialog.cs`, `ContactsPanel.cs`, `FleetCommandDialog.cs`, `ScoutOverviewDialog.cs`, `CombinedUiNavigation.cs`.

- [ ] Add failing `WorkspaceExitNeverAnswersOrDropsRequiredResponse`, `BackAndEscRestoreSemanticContext`, `OnlyConfirmationsRemainModal`, `LedgerReassignIssuesOneWorkforceCommand` (drag and keyboard paths), `LedgerShowsRejectionVerbatim`, `ShortcutsAndDragsIssueAtMostOneExplicitCommand` (drag part).
- [ ] Build `WorkspaceFrame` (title, back key, Esc) on the existing `ShowWorkspace` router. Move Research, Fleets (+Scouts tab), Designs and Diplomacy (+Contacts tab) content into workspace pages; reduce the old windows to confirmation shells or remove them where unused. Keep route `Name`s.
- [ ] Build the ledger: one row per colony with worker figures per role, outputs in resource colors, build item with progress; drag or keyboard reassign issues the existing workforce command once and shows its actual result. Keep the detailed colony panel reachable from each row.
- [ ] Rerun cases, the full twelve-scenario matrix and M13 Navigation; record updated layout-check counts honestly.
