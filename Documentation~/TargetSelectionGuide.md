# Target Selection Guide

Attacks never searches the world. The caller gathers candidates and supplies a bounded collection.

Good candidates can represent:

- nearest enemy
- highest threat enemy
- most progressed lane enemy
- lowest health enemy
- active defense attacker

The candidate score is caller-defined. `AttackTargetPolicy.HighestScore` treats higher score as better. `AttackTargetPolicy.LowestScore` treats lower score as better. Equal scores use `CombatantId` ordering, so insertion order does not change selection.
