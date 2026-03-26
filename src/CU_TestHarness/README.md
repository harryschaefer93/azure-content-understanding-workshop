# CU Test Harness — Blazor Server

A lightweight local UI for testing Azure AI Content Understanding analyzers.
Upload files (PDF, images, Office docs, audio, video) and see extracted fields, raw JSON, and markdown output.

## How it works

```
Local browser (localhost) → Blazor Server app → Azure Content Understanding endpoint
```

Files are sent to the Azure CU service for analysis — they are **not** processed locally.
Uploaded files exist only in memory during analysis and are discarded after results render.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or whichever version matches the project TFM)
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) — run `az login` before starting
- **Cognitive Services User** role on the CU resource (ask your infra team to assign it)

## Quick Start

```bash
cd src/CU_TestHarness

# Authenticate
az login

# Run the app
dotnet run
```

Open the URL shown in the terminal (typically `http://localhost:5000`).

## Configuration

Edit `appsettings.json` to change the endpoint, default analyzer, or max file size:

```json
{
  "ContentUnderstanding": {
    "Endpoint": "https://your-resource.cognitiveservices.azure.com",
    "DefaultAnalyzerId": "prebuilt-documentSearch",
    "MaxFileSizeMB": 50,
    "ModelsEndpoint": "https://your-models-resource.cognitiveservices.azure.com"
  }
}
```

## Features

- **Upload pane** — drag/drop or browse for files
- **Analyzer selector** — dropdown of available analyzers + free-text input for custom IDs
- **Results tabs:**
  - **Fields** — structured key-value table with confidence scores
  - **Raw JSON** — full API response
  - **Markdown** — extracted content in markdown format
- **Settings page** — independent completion and embedding model pickers with deployment type badges (Standard vs Global Standard) and exact deployment region, CU connection test, endpoint config
- **Completion models** — 4 deployment profiles:
  - GPT-4.1 Global Standard (🌐 inference may leave deployed region)
  - GPT-4.1 Mini Global Standard (🌐 inference may leave deployed region)
  - GPT-4.1 Mini Standard (🔒 region-guaranteed)
  - GPT-4o Standard (🔒 region-guaranteed)
- **Embedding models** — 3 deployment profiles:
  - Ada 002 Standard
  - Embedding 3 Large Standard
  - Embedding 3 Small Standard
- **Per-use-case model override** — Schema Builder and Schema Editor let you choose a different completion + embedding combination for each analyzer
- **Active model badge** — every page shows the current model profile and data residency status
- **Architecture reference** — self-contained dark-themed page (`/architecture.html`) comparing CU vs DI: resource diagrams, analyzer taxonomy, model routing flow, PATCH defaults mapping, capability table, and data residency callout
- **Analyzer templates** — 7 pre-built schema templates in Schema Editor:
  - **Commitment Letter** — 19 fields targeting common DI pain points (Borrowers array with split first/middle/last names, 6 address components, SolicitorConditions array with cross-page hint, Summary via generate method)
  - **Enhanced Title Search** — RegisteredOwners array with name components, ShortLegal, cross-page encumbrances, LegalDescription
  - Field Extraction, Document Classification, RAG Search, CTI Classification, Multi-Province Title Search
- **Confidence color-coding** — field confidence scores are color-coded across all pages (green ≥80%, amber 50–80%, red <50%)
- **Copy JSON buttons** — one-click clipboard copy on all raw JSON panels (Analyze, Compare CU/DI, Validate per-doc)
- **Progress bars** — visual progress indicators on Validate and Test Suite pages during batch analysis
- **Field count badge** — Analyze results header shows extracted field count at a glance
- **Test Suite auto-populate** — when a custom analyzer is selected, field names from its schema are automatically pre-populated on newly uploaded test docs. A "Pre-fill Expected Values" button runs each doc through the analyzer and fills extracted values as editable expectations with confidence scores. Per-doc "Auto-detect" links also available. Existing user-typed values are never overwritten.
- **Semantic matching** — Test Suite supports LLM-powered semantic comparison (toggle on/off). When a Contains check fails, the field pair is sent to Azure OpenAI for semantic equivalence evaluation. Results show match method badge (🧠 Semantic / Abc Contains), expandable LLM reasoning with confidence, and token cost per field. Uses the active completion model (GPT-4.1 Mini by default) via a separate Models endpoint.
- **PDF Viewer with bounding boxes** — Analyze page shows a split-panel layout: PDF/image viewer on the left with bounding box overlays for extracted fields, results on the right. Click a field row to highlight its bounding box on the document; click a bounding box on the document to scroll to and flash the corresponding field row (bidirectional linking). Auto-rotation: page orientation is detected from Azure API response data and applied automatically. Results panel scrolls independently so all field rows are accessible. Multi-page navigation for PDFs. Reusable `DocumentViewer` component backed by PDF.js.

> **Model routing note:** The Settings page model selection applies to **custom analyzers** only — the harness embeds your chosen model names into the analyzer JSON it creates. **Prebuilt analyzers** (e.g. `prebuilt-documentSearch`, `prebuilt-invoice`) use separate server-side defaults (`prebuilt-analyzer-completion/embedding/completion-mini`) set via the `PATCH /contentunderstanding/defaults` API. See the [infrastructure README](../../infra/README.md) for details.

## Authentication

Uses `DefaultAzureCredential`, which chains:
1. **Azure CLI** (`az login`) — primary method for local dev
2. **Interactive Browser** — fallback if CLI isn't available
3. **Managed Identity** — for future server deployment

No API keys are used or stored.

## Supported file types

PDF, JPG, PNG, TIFF, BMP, DOCX, XLSX, PPTX, MP3, MP4, WAV, WebM
