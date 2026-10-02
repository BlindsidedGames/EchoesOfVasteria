# Repeatable Mac tests

Echoes already uses Unity Test Framework (1.8.0), NUnit, and separate EditMode and
PlayMode assemblies. Extend these tests rather than introducing another game
framework. Python 3 is needed only for the local runner; it adds no game runtime
code or packages.

From the repository root:

```sh
python3 tools/testing/run.py --platform EditMode --filter ResourceDropTests
python3 tools/testing/run.py --platform both
python3 tools/testing/run.py --platform PlayMode --filter SaveSystemStressPlayModeTests
python3 -m unittest discover -s tools/testing -p 'test_*.py' -v
```

The runner uses the exact editor version in `ProjectSettings/ProjectVersion.txt`
under `/Applications/Unity/Hub/Editor`. Override its executable with `--unity`.
`--filter` is Unity's test-filter expression. A misspelled filter, no discovered
cases, all skipped cases, missing/malformed XML, suite setup failure, nonzero
Unity exit, or timeout produces exit code 1. A successful suite must contain at
least one passing case; skipped cases are listed separately. Exit code 0 from
`--prepare-only` means prepared inputs, **not** passing game tests.
The command explicitly selects Echoes' `EditMode.Tests` and `Tests` assemblies;
vendor package tests are excluded even when packages are embedded for offline use.

## Disposable runs and evidence

Each invocation creates a new `/tmp/eov-tests-*` run with a copied Unity project
containing `Assets`, `Packages`, and `ProjectSettings`. Current dirty files are
included. Input paths, SHA-256 hashes and sizes go in `inputs.json`; concurrent
input changes detected during copying fail preparation. Coordinate with active
authors and wait for temporary build/save guards to be restored before taking a
snapshot. Unsaved Editor changes are not included. No Git index or authored
project files are changed.

The disposable project's company/product names are changed to a unique test
identity. This prevents macOS preference services from targeting the game's
normal PlayerPrefs namespace, including tests that call `PlayerPrefs.DeleteAll`.
`HOME`, temporary paths and package cache point into the run directory. The
macOS OS sandbox denies direct external IP access and regular file writes outside
scratch, with narrow Unity infrastructure exceptions below. HOME alone does not
guarantee native macOS save/preference isolation.
The runner refuses an unisolated fallback.
The disposable home links to installed `~/Library/Unity/licenses` for offline
entitlement reads. License bytes are never copied into evidence or logged; the
sandbox denies writes to the original directory. No account activation or
external license refresh is performed.

Locked registry packages are copied from `Library/PackageCache` into the disposable
project's `Packages` folder as [supported embedded packages](https://docs.unity3d.com/6000.6/Documentation/Manual/upm-embed.html).
They retain exact locked versions and bytes. Missing, mismatched or linked caches
fail preparation; `package-inputs.json` records package file hashes. This avoids
registry metadata requests that still occur with a cache-only copy. A full
Library, UserSettings and player saves are never copied. Linked project inputs
are rejected. A cold snapshot imports assets and can be slow. Provision the
project's editor/packages normally first;
do not remove the sandbox or enable real services to make a test pass. A sandbox
or licensing failure is an infrastructure failure, not game regression evidence.

Native Mac paths also ignore HOME. Actual runs create unique
`~/Library/Application Support/EchoesTests/<test identity>` and
`~/Library/Caches/EchoesTests/<test identity>` links to scratch's `native/saves`
and `native/cache`. The runner refuses existing namespaces/preferences and removes
only its own links and matching test preference files afterward. Empty parents
are removed only if it created them. Prepare-only creates no native links.
Save/cache bytes remain in run evidence. A hard kill can leave links; inspect the
target before manually removing only that run's test namespace.

The task-local sandbox permits Unix sockets and localhost IPC for Unity licensing,
UPM, compilers and shader imports. Direct external outbound IP connections stay
denied. It allows temporary Unix socket files (not arbitrary regular files),
Unity preference/shader IPC lock files, .NET shared-memory/lock files, and only
Unity's CoreBusinessMetrics/Curl cache SQLite files and sidecars, and its
`~/Library/Caches/com.unity3d.UnityEditor/GiCache` lighting cache. Those native
Unity caches can change. Normal game preference/save directories have no write
permission. No persistent OS/network policy changes.

The sandbox denies Roslyn-style hashed shared-compiler Unix pipes so concurrent
snapshots cannot reuse a compiler server with another run's sandbox permissions.
Roslyn has an existing [local compilation fallback](https://github.com/dotnet/roslyn/blob/main/src/Compilers/Shared/BuildClient.cs)
when server communication fails. No response-file `/shared` override or normal
Editor server shutdown is used. Compilation can be slower. After an editor
upgrade, rerun the OS boundary contracts and a real cold Unity run; a changed
native IPC layout can fail infrastructure validation.

This is not a separate OS account or a complete network prohibition: local
services and existing licensing/OS daemons are outside the child process sandbox.
Use trusted tests, not arbitrary production scenes or cloud/Steam tests. Do not
relay game traffic through a local proxy; inherited HTTP/HTTPS/ALL_PROXY variables
are cleared. This runner does not validate production cloud behavior.

Use `--prepare-only` to inspect a snapshot without starting Unity. Use `--output`
to choose a fresh empty directory outside the source project. The default timeout
is 1,800 seconds per suite including import; `--timeout` changes it. Asset imports
run in process to avoid unnecessary parallel import services. A timeout or
Ctrl-C terminates the Unity process group, including child import workers.
`both` runs suites serially in the same disposable project. Do not run the game's
normal Editor tests against real player data or while a production Play session
is active.

Each run retains `EditMode.xml`/`PlayMode.xml`, corresponding Unity logs,
`sandbox.sb`, input/package manifests and `summary.json`. JSON includes counts, failure
names/messages/stacks, commands, elapsed time and process exit. A prepared run
has no XML or logs. Summarize archived NUnit XML with:

```sh
python3 tools/testing/run.py --summarize /tmp/eov-tests-EXAMPLE/EditMode.xml
```

Inspect logs when there is no XML (compile/import/license errors). Keep failed
evidence when reporting an issue. Delete the disposable run directory once its
evidence is no longer needed; the runner never silently removes failed runs or
reuses stale results.

## Writing and owning tests

Protect an observable contract with a credible failure. Reuse the strongest
existing owner test instead of copying its cases across layers. A bug regression
must fail on the prior behavior for the intended reason and pass after repair.
Avoid source-text snapshots and test-only runtime switches.

* EditMode owns command/result invariants, content contracts, codecs and pure
  calculations. `ResourceDropTests` demonstrates injected `rand` callbacks in
  `DropResolver.RollDrops`; use boundary values or an explicit sequence, not
  statistical assertions or global RNG seeding when a callback is available.
* Farm command tests already use fixed UTC timestamps and supplied elapsed
  seconds. Closed/suspended time grants **no garden growth**. Reuse these
  explicit inputs instead of adding a global clock. Wall-clock policy for
  another subsystem must be established at that subsystem's boundary.
* PlayMode owns real component lifecycle and integration. Existing fixtures use
  `IsolatedPlayModeScene` so the runner owns its empty scene and restores the
  configured normal Play start scene. It does not itself isolate saves or cloud.
* Save-specific fault, migration and import/export tests belong to the save
  hardening work. Reuse its disposable-root fixtures and `SaveManager` override.
  Every fixture must restore the prior override, delete only its own directory,
  and avoid cloud/player banks. Never disable a required isolation check just
  because a type or test hook is absent.

Keep GameObjects/ScriptableObjects local to each fixture; destroy them in
`finally`/TearDown. Preserve and restore global time scale, fixed delta, RNG state
(when global RNG is unavoidable), singleton and event state. Do not parallelize
fixtures that share global state. Tests should not depend on run order. Prefer
existing per-subsystem fixtures; add a shared helper only when it removes actual
duplication. No additional deterministic runtime architecture is needed for the
existing explicit time/RNG seams.

## Audit scope and limits

Initial audit found working assemblies, supplied-delta real-time tests, weighted
drop tests, fixed-time farming command/journal/transaction tests and substantial
save stress/codec/migration coverage. The immediate missing piece was a checked-in
repeatable command with protected data and clear failure reporting.
`OracleBetaNamingTests` currently deletes all prefs in its test namespace; always
use disposable isolation for it. Existing tests were retained, not rewritten or
deleted during this tooling section.

The runner's own contract tests cover missing/empty/skipped/failing results,
process failure, disposable input ownership, stale-result rejection, external
link rejection, exact offline package copies, native fixture cleanup and worker
cleanup. A real OS check verifies local Unix/loopback IPC works while an external
IP connection and protected regular-file write are denied. Passing those proves
tooling behavior; it
does not prove Unity suites, platform builds, device/AOT behavior, UI rendering,
production cloud, or actual suspend/resume integration. Record actual Unity
commands and results separately when the editor is available for coordinated
validation.

## Validated on this Mac

On 2026-10-02, Unity 6000.6.0f1 passed all 158 existing Echoes EditMode cases
and all 51 existing PlayMode cases in disposable copies, with process exit 0.
All 10 Python runner contracts passed. EditMode used the fresh full CLI run;
PlayMode was rerun against that snapshot after resolving native licensing/cache
startup restrictions. Earlier infrastructure failures remain in scratch logs.
These counts exclude the separately authored save-hardening tests and are not a
claim that every future snapshot or editor version will pass.
