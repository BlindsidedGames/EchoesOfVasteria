# Reviewed integration baseline, 2 October 2026

This establishes the tested foundations required by the direct Cauldron and adventure fruit slice. It publishes source to the existing development branch; it does not merge or release a Player. Production Main and authored build settings remain unchanged.

The baseline includes the prior Mac diagnostic fixes (MPUIKit initialization, TMP shader pragma and verified Fonts Addressables schemas), corrected test assembly identity/routing and isolated PlayMode fixtures; schema-4 save projection and released-save compatibility; the bounded Radish model, transaction coordinator, receipt retention, native Farm UI and isolated demonstration scene; and the approved seed/timber icon and timber resource mappings.

The Radish demonstration remains `Assets/Development/Farming/Main.unity`. Production Main has no new farm controller wiring. Existing runtime support is available to later development; the full farming unlock/quest expansion, production seed gate and separate town orchard are outside this baseline. Earlier dated review documents describe their original uncommitted checkpoints and remain historical evidence.

Provenance was checked against the final migration review hashes and the later receipt-retention hashes. The later CurrentCollections version deliberately supersedes the earlier migration snapshot. Overlapping Cauldron/UI/atlas files use their saved pre-slice bytes. The SaveImportExport baseline is the tested schema-4 ingress/export version, captured before the separate save-hardening telemetry fix. No unrelated working files or concurrent task changes are included.

Prior validation: 121/121 EditMode cases, 51/51 PlayMode cases, 54/54 focused migration cases, copied historical ES3/binary roundtrips, isolated Player normal/offline/reload/publication-failure/slot-transition phases, and timber completion/icon checks. The current Cauldron integration additionally passes 79 focused/regression cases and four actual Metal Player phases. A fresh compiler check of the exact foundation snapshot confirms its Runtime, Editor and test assemblies compile independently of the later Cauldron/fruit source.

Mac Player builds succeed with zero errors. The two retained configuration warnings are missing iOS localization App Info metadata and absent optional RuntimePipelineConfig. Mobile/AOT, signed device-container upgrades and normal-session balance remain separate validation gates. Publishing this source is not an assertion that those release checks have passed.

Excluded: editor preferences, generated Odin AOT configuration, historical layout preview experiments, planning reports, real save files and backups, temporary guards/helpers, tools/testing and docs/testing.md owned by the harness task, and new save-hardening edits/tests. Existing user work remains on disk.

See [Radish review](../farming-radish-slice-2026-10-01/followup-handoff.md), [timber verification](../timber-resources-2026-10-01/README.md), and [Cauldron/fruit implementation](../cauldron-orchard-2026-10-02/README.md). The Cauldron implementation is the next commit in this sequence.

The staged reference audit checks 1,241 GUID references, resolving 32 through installed packages. Two unresolved material/sprite GUIDs already occur in unchanged production Main (`3c784e62cfa46be4781056917903af84`, `6ed0f6e169197f2428d71c85ece64413`); the isolated duplicate inherits them. No introduced unresolved reference was found. They were retained rather than expanding this integration into unrelated scene repairs.
