# Troubleshooting Multiline Extraction in Azure Content Understanding

> Repro CLI for FCT Canada's reported issue: CU mis-extracts fields whose values span multiple lines or cross page boundaries.

## The customer's symptom

Title-search PDFs contain fields (e.g. `Owner_Address`, `Particulars`) whose values commonly:

- wrap across multiple visual lines on a single page, **or**
- continue from the bottom of one page to the top of the next.

CU's `prebuilt-documentSearch` (and custom analyzers built on top of `prebuilt-document`) sometimes returns either the first line only, the last line only, or two separate field instances instead of a single concatenated value.

## How to reproduce

All commands run from repo root.

```pwsh
az login
dotnet build FCT_CU_TestHarness.sln
dotnet run --project src/CU_ReproRunner -- preflight
```

`preflight` will print:

- the endpoint it's actually talking to (so you can confirm env-var overrides),
- the current `defaults` routing table (and **flag** any analyzer routing to `gpt-41` GlobalStandard — that violates Canadian residency),
- the list of analyzers already on the resource.

If `preflight` exits 1, the endpoint or auth is wrong — fix that before going further.

### Single-analyzer repro

```pwsh
dotnet run --project src/CU_ReproRunner -- run-repro --allow-replace
```

This:

1. Loads `test-docs/SyntheticTitleSearchDocswithSchema/Title_Search_Sample_Schema_(996658-996659).json`.
2. Wraps it with `analyzerId=repro_title_search_v1`, `baseAnalyzerId=prebuilt-document`, and **explicitly pins** `models.completion=gpt-4.1-mini` + `models.embedding=text-embedding-3-large`. We don't rely on `defaults` because those have routed to GlobalStandard models in the past.
3. PUTs the analyzer (with `--allow-replace` if specified).
4. Runs both PDFs (`996658_Title_Search.pdf`, `996659_Title_Search.pdf`) through it.
5. Writes `output/<doc>/repro_title_search_v1/result.{json,md}`.

The `result.md` table marks each field as **Multiline?** and **Cross-page?**. That's where you look first when triaging a miss.

### Multi-analyzer comparison

```pwsh
dotnet run --project src/CU_ReproRunner -- run-compare
```

Runs the same PDFs against `prebuilt-read`, `prebuilt-layout`, `prebuilt-documentSearch`, and `repro_title_search_v1`, then emits `output/summary_table.md` with one row per (doc, analyzer) including suspected-multiline and suspected-cross-page counts.

Use this to answer: "Is the multiline issue specific to our custom analyzer, or does it also show up in `prebuilt-layout`?" If `prebuilt-layout` already loses the line wrap, it's an upstream layout issue, not a schema issue.

### Diff against expected values

1. Run `run-repro` (or `run-compare`) once to populate `output/`.
2. Seed the expected files:

   ```pwsh
   dotnet run --project src/CU_ReproRunner -- run-diff --seed-missing
   ```

   This creates `expected/996658_Title_Search.expected.json` and `expected/996659_Title_Search.expected.json` from one of the actual results. **Open them and edit the `value` for any field you care about** — these become the ground truth.
3. Re-run `run-diff` (without `--seed-missing`) after every schema tweak. Exit code 1 ⇒ at least one ❌ miss.

`output/diff_report.md` legend:

| Symbol | Meaning |
|---|---|
| ✅ | Normalized expected value matches actual |
| ⚠ | One is a substring of the other (likely truncation) |
| ❌ | Mismatch, or actual was not extracted |
| ❓ | No expected value supplied for that field |

## Adding new PDFs

1. Drop the PDF into a folder, e.g. `test-docs/title-search/` or a new fixture folder.
2. (Optional) If the schema differs, copy `Title_Search_Sample_Schema_*.json` and modify it.
3. Run with explicit args:

   ```pwsh
   dotnet run --project src/CU_ReproRunner -- run-repro `
     --docs test-docs/title-search `
     --schema test-docs/title-search/my-schema.json `
     --analyzer-id repro_title_search_v2 `
     --allow-replace
   ```
4. Then `run-diff --seed-missing` to bootstrap expected files for the new docs.

## Where to tweak schema hints

Open `test-docs/SyntheticTitleSearchDocswithSchema/Title_Search_Sample_Schema_(996658-996659).json` and edit `description` strings on the fields that are losing data. Things that have helped on similar engagements:

- For `Owner_Address`: explicitly say "may span multiple lines; concatenate all lines into a single string with spaces between line wraps".
- For tabular fields (`Title_Search.Particulars`): "values may wrap to the next visual line within the same row; treat consecutive lines indented under the same Reg_Num as one Particulars value".
- Avoid `method: "classify"` and `method: "generate"` at field level until extraction is solid — those amplify mis-grouping.

After editing, re-run with `--allow-replace` and re-diff.

## Known limitations of the detector

- Multiline detection is heuristic: we flag a field if its value contains `\n` or if its source span has multiple `D(page,…)` tokens. CU may return a single span covering wrapped text, in which case we'll under-report multiline. Inspect `result.json` → `rawJson` → `result.contents[].fields[].source` if a field looks suspicious.
- Cross-page detection requires the source span string to reference more than one page index. Some analyzers don't emit page indices in `source` strings; for those, we fall back to `boundingRegions[].pageNumber` if present, else report `false`.
- The detector does **not** read polygon y-coordinates to estimate line counts — too noisy across analyzers. The `\n` + multi-span heuristic is good enough for triage.
- For array/object fields (e.g. `Registered_Owners`, `Title_Search`), the raw CU JSON uses `valueArray`/`valueObject` nesting. `RawFieldFlattener` recursively walks these into leaf paths like `Title_Search[0].Reg_Num` and `Registered_Owners[1].Owner_Address`, so each leaf gets its own multiline / cross-page assessment. Without flattening, the parent container aggregates all child spans — which falsely flags cross-page.

## Page count caveat

CU's raw response at `result.contents` is an array with one entry **per input file**, not per page. The actual page count lives in `result.contents[0].pages[]` (one element per physical page). Early versions of this tool read `contents.Length` as PageCount, which reported "1 page" for multi-page PDFs and caused the cross-page heuristic to over-fire. The fix sums `contents[].pages[].Length` across all content entries.

## Residency reminder

Never let CU route to `gpt-41` or `gpt-41-mini` (both GlobalStandard — data may leave Canada). The `--completion-model` arg defaults to `gpt-4.1-mini` (Standard, Canada-resident). If `preflight` shows `defaults.<analyzer>-completion = …/gpt-41`, re-PATCH the defaults using `infra/defaults-body.json` before any production-style run.
