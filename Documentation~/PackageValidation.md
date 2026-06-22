# Package Validation

Validation target:

- Unity editor: `6000.3.5f1`
- Validation project: `C:\Repositories\Deucarian\Attacks-TestProject`
- Package path: `C:\Repositories\Deucarian\Attacks`
- Package reference mode: local file package

## Completion Gate

- Package imports without errors.
- EditMode tests pass twice.
- Combat integration proof passes.
- Defense Games composition proof passes.
- Donor proof passes.
- Idle Auto Defense and Classic Tower Defense proofs pass.
- Benchmark output is recorded honestly.

## Results

- Import command: `Unity.exe -batchmode -quit -projectPath C:\Repositories\Deucarian\Attacks-TestProject\ -logFile C:\Repositories\Deucarian\Attacks-TestProject\Logs\import-2.log`
- Import result: passed, no compiler or package-manager errors. The log contains the usual Unity licensing-token warning.
- EditMode pass 1: `11` total, `11` passed, `0` failed. Results: `C:\Repositories\Deucarian\Attacks-TestProject\Logs\editmode-results-1.xml`, duration `0,150141`.
- EditMode pass 2: `11` total, `11` passed, `0` failed. Results: `C:\Repositories\Deucarian\Attacks-TestProject\Logs\editmode-results-2.xml`, duration `0,1534825`.

## Benchmark

`C:\Repositories\Deucarian\Attacks-TestProject\Logs\attacks-benchmark-results.json`

- `1,000` attack evaluations, `3` candidates, ready zero cooldown: `2.924 ms`, `0` bytes allocated.
- `5,000` attack evaluations, `3` candidates, ready zero cooldown: `14.917 ms`, `0` bytes allocated.
- `10,000` attack evaluations, `3` candidates, ready zero cooldown: `28.942 ms`, `0` bytes allocated.

These are Unity EditMode Mono results and do not claim mobile performance.
