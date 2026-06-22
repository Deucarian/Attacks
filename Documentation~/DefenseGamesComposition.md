# Defense Games Composition

Attacks has no runtime dependency on Defense Games.

A game layer can compose both packages:

1. Defense Games tracks active attackers and objectives.
2. The game layer exposes selected active attackers as `AttackTargetCandidate` values.
3. Attacks emits a Combat request.
4. Combat resolves target damage.
5. The game layer reports `ReportKilled`, `ReportReachedObjective`, or objective damage signals back to Defense Games.

This preserves package ownership: Defense Games owns lifecycle, Attacks owns attack timing and intent, Combat owns damage resolution.
