# Existing Unity Localization workflow

These offline tools operate on the project's standard Unity StringTable assets.
Google Sheets remains the collaborative authoring interface for Quests, Wiki and
TownUI (the Interface tab). Standard Unity Google Sheets extensions map A=key,
B=comments, C=en, D=ru, E=fr, F=de, G=es-419, H=pt-BR, I=zh-CN and J=ja.
K is Sheet context only. Nothing in these offline Ruby tools calls Google, imports a Sheet, removes keys, changes
translations, or replaces the runtime.

Run from the repository root with system Ruby (Psych, JSON, Minitest only):

```sh
ruby scripts/localization/catalog.rb inventory --output /tmp/authored-key-inventory.json
ruby scripts/localization/catalog.rb audit --output /tmp/localization-audit.json
ruby scripts/localization/catalog.rb snapshot --output /tmp/localization-before.json
ruby scripts/localization/catalog.rb changes --input /tmp/localization-before.json
ruby scripts/localization/catalog.rb sheet-preflight --input /path/to/live-export.json --output /tmp/sheet-review.json
ruby scripts/localization/catalog_test.rb
```

Audit reports duplicate keys/IDs, orphan locale IDs, missing/blank translations,
argument mismatches and unbalanced rich-text markup. The formatter check supports
positional composite-format and simple named SmartFormat arguments; complex nested
SmartFormat/plural expressions need Unity formatter tests and translator review.
Missing translations are coverage findings rather than fatal errors. Source-change
reports identify changed English/keys and removals; existing translations are flagged
for review, never automatically modified or declared linguistically correct.

The Sheet export schema is `tabs[{title, rows[{key,numericIdNote,english,russian}]}]`;
the all-language export also includes `values` by locale and a key-based sync plan.
Preflight compares exact keys, ID notes and values, including duplicates. Treat old
snapshots as evidence only: re-read affected cells immediately before an authorized
external write and preserve comments, ID notes, unrelated cells and translations.
The current Quests/Wiki importer is configured to remove keys missing from a pull.
**Do not blanket-pull incomplete Sheets or silently delete local-only entries.**

Interface uses `RemoveMissingPulledKeys=false`; Quests and Wiki retain their existing
setting. Include all shared keys, including unused legacy interface keys, before
testing any pull. The service-provider asset and its `.meta` are deliberately
gitignored at `Assets/Localization/Sheets Service Provider.asset`. Fresh checkouts may
lack that local asset; mappings preserve its original GUID reference. Restore
the existing local authoring asset **and original meta** from the configured authoring
machine or backup to retain its GUID. The current Mac authoring checkout has the
original pair restored; Google authorization on this Mac is a separate step. If unavailable, reconnect through Unity's
supported Localization/Google Sheets Service workflow under separately authorized
credential setup. No credentials are created by the mapping tool. Browser-authorized
Sheet editing is independent of that Unity service-provider connection.

## Register missing English entries

A manifest is an array (or `{entries:[...]}`) with `key`, `english`, optional
`collection` (default TownUI), optional `id`, and optional `sources` context.
Existing keys retain their IDs. New IDs allocate from 87000000000003000 upward,
checking loaded IDs; an explicit noncolliding ID is also accepted. Existing English
that differs causes refusal, requiring a separate reviewed source-authority decision.
A missing English entry can be added without changing translations or metadata.

```sh
ruby scripts/localization/catalog.rb coverage --input /path/to/source-inventory.json
ruby scripts/localization/catalog.rb sync-english --input /path/to/source-inventory.json --output /tmp/catalog-plan.json
# Review the plan and coordinate editor/source ownership before this local write:
ruby scripts/localization/catalog.rb sync-english --input /path/to/source-inventory.json --apply --expected /tmp/catalog-plan.json
```

Dry-run is the default. Apply verifies both the reviewed manifest hash and input
asset hashes. It only inserts new list entries, preserving existing asset bytes,
IDs, translation content and metadata. Unsupported Unity YAML layouts fail closed.
Keep Unity and other source writers idle during apply; a filesystem write across
multiple assets is not a database transaction. Review the Git diff before opening
Unity, and include normal compilation/runtime language-switch checks.

The inventory command reads semantic key/fallback pairs from Toolkit definition assets
using the YAML parser and reports conflicting source fallbacks. Coverage accepts authored semantic asset inventories and the dynamic UI/NPC key
inventories emitted by the migration. It checks exact English and key membership;
it is not a compiler or an exhaustive scanner for arbitrary hardcoded prose.
New UI must supply a standard table-backed key and English source in that inventory.

## Validate translated frozen exports

```sh
ruby scripts/localization/catalog.rb validate-translation --source /path/to/frozen-english.json --input /path/to/translation-fr.json --output /tmp/translation-review.json
```

This mode reads only the two JSON inputs, never live Unity assets. It accepts
`translation` or `text` entry values and `sourceHash` or `englishHash` metadata.
It requires the exact collection/ID/key inventory, detects duplicate IDs, preserves
blank entries, and compares balanced format signatures (including nested SmartString
arguments and branch structure), exact markup/sprite tags, numeric tokens, controls
and escaped controls. Mismatches return exit status 1; invalid inputs return 2.
Natural-language SmartString branch text can change while selectors/specifiers stay
fixed. This is structural validation, not a substitute for Unity formatter execution,
locale-specific plural-rule review, visual checks or native-language editing.

### Reviewed numeric wording differences

Some natural translations express spelled-out quantities as digits (for example,
Japanese `Double` → `2倍`). These are review findings, not grounds to rewrite good
translation. An optional `--numeric-review /path/to/review.json` can accept them:

```json
{
  "sourceHash": "the frozen English hash",
  "locale": "ja",
  "rows": [{
    "collection": "TownUI", "id": "12", "key": "example",
    "english": "Double {0}", "translation": "{0}を2倍",
    "reason": "Double is naturally expressed as 2倍 in Japanese."
  }]
}
```

Only numeric token differences are accepted, for byte-identical English/translation
pairs with the same collection, ID, key, source hash and locale, and a nonempty review
reason. Changed pairs require new review. Accepted differences remain visible in
`numericReviewFindings`. This cannot waive placeholder/specifier changes, markup,
sprite IDs, missing entries, controls or any other structural mismatch. No automatic
numeric waiver is provided and no assets are written by validation.
