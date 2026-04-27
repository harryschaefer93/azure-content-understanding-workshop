# CU_ReproRunner

Console app that automates FCT Canada's multiline-extraction repro workflow against Azure Content Understanding.

It **reuses** `src/CU_TestHarness/Services/ContentUnderstandingService.cs` for all CU calls (Entra auth, analyzer CRUD, file analyze) — no duplicated SDK code.

## Quick start

```pwsh
az login
dotnet run --project src/CU_ReproRunner -- preflight
dotnet run --project src/CU_ReproRunner -- run-repro --allow-replace
dotnet run --project src/CU_ReproRunner -- run-compare
dotnet run --project src/CU_ReproRunner -- run-diff --seed-missing
```

## Commands

| Command | Purpose |
|---|---|
| `preflight` | Pings the CU endpoint, lists current `defaults` + analyzers, flags any GlobalStandard model routing as a residency risk. |
| `run-repro` | Wraps the customer's `fieldSchema` JSON, pins residency-safe Standard-SKU models (`gpt-4.1-mini` + `text-embedding-3-large`), creates/updates the analyzer, then analyzes every PDF in the docs folder. |
| `run-compare` | Analyzes every PDF against a list of analyzers (`prebuilt-read`, `prebuilt-layout`, `prebuilt-documentSearch`, `repro_title_search_v1` by default) and emits `output/summary_table.md`. |
| `run-diff` | Walks `output/<doc>/<analyzer>/result.json`, matches against `expected/<doc>.expected.json`, writes `output/diff_report.md`. Use `--seed-missing` to bootstrap expected files. |

Run `CU_ReproRunner <command> --help` for full options.

## Configuration

Reads `src/CU_ReproRunner/appsettings.json`. Override at runtime with env vars:

```pwsh
$env:ContentUnderstanding__Endpoint       = "https://fcttest-cusrv-models-cc.cognitiveservices.azure.com/"
$env:ContentUnderstanding__ModelsEndpoint = "https://fcttest-cu-models-ce.cognitiveservices.azure.com/"
$env:ModelDeployments__DefaultCompletion  = "gpt-4.1-mini"
$env:ModelDeployments__DefaultEmbedding   = "text-embedding-3-large"
```

## Output layout

```
output/
  <doc>/
    <analyzerId>/
      result.json    Full AnalysisViewModel (deterministic ordering)
      result.md      Human-readable summary + multiline / cross-page flags
  summary_table.md   (run-compare)
  diff_report.md     (run-diff)
expected/
  <doc>.expected.json   Hand-edited (or seeded) ground-truth values
```

See [`TROUBLESHOOTING_MULTILINE.md`](../../TROUBLESHOOTING_MULTILINE.md) at the repo root for the full workflow.
