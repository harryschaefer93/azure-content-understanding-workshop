# CU Test Harness — Blazor Server

A local UI for testing Azure AI Content Understanding analyzers.
Upload files (PDF, images, Office docs, audio, video) and see extracted fields, raw JSON, and markdown output.

```
Local browser (localhost) → Blazor Server app → Azure CU endpoint
```

Files are sent to the Azure CU service for analysis — **not** processed locally.
Uploaded files exist only in memory during analysis and are discarded after results render.

> For prerequisites, authentication details, and infrastructure setup, see the [parent README](../README.md).

## Quick Start

```bash
cd CU_TestHarness
az login
dotnet run
```

Open the URL shown in the terminal (typically `http://localhost:5000`).

## Configuration

Edit `appsettings.json` to set your endpoints:

```json
{
  "ContentUnderstanding": {
    "Endpoint": "https://{prefix}-cusrv-models-cc.cognitiveservices.azure.com",
    "DefaultAnalyzerId": "prebuilt-documentSearch",
    "MaxFileSizeMB": 50,
    "ModelsEndpoint": "https://{prefix}-cu-models-ce.cognitiveservices.azure.com"
  }
}
```

Endpoint values come from `terraform output` — see the parent README for deployment steps.

## Features

- **Upload pane** — drag/drop or browse for files
- **Analyzer selector** — categorized dropdown of available analyzers + free-text input for custom IDs
- **Results tabs** — Fields (structured key-value with confidence scores), Raw JSON, Markdown
- **PDF Viewer** — split-panel with bounding box overlays, bidirectional field ↔ box linking, multi-page navigation, auto-rotation
- **Settings page** — completion and embedding model pickers with deployment type badges, CU connection test
- **Schema Builder / Editor** — per-use-case model override for custom analyzers
- **Compare CU/DI** — side-by-side Content Understanding vs Document Intelligence comparison
- **Test Suite** — batch analysis with auto-populated field expectations, semantic matching (LLM-powered)
- **Confidence color-coding** — green ≥80%, amber 50–80%, red <50%
- **Architecture reference** — self-contained dark-themed page (`/architecture.html`)

### Completion Models

| Model | SKU | Data Residency |
|-------|-----|----------------|
| GPT-4.1 | Global Standard | Inference may leave Canada |
| GPT-4.1 Mini | Global Standard | Inference may leave Canada |
| GPT-4o | Standard | Canada East guaranteed |

### Embedding Models

| Model | SKU | Region |
|-------|-----|--------|
| text-embedding-ada-002 | Standard | Canada East |
| text-embedding-3-large | Standard | Canada East |
| text-embedding-3-small | Standard | Canada East |

### Analyzer Templates

7 pre-built schema templates in the Schema Editor:
- **Commitment Letter** — 19 fields targeting FCT pain points (borrower name arrays, address components, solicitor conditions, summary)
- **Enhanced Title Search** — registered owners, short legal, cross-page encumbrances, legal description
- Field Extraction, Document Classification, RAG Search, CTI Classification, Multi-Province Title Search

> **Model routing note:** Settings page model selection applies to **custom analyzers** only. **Prebuilt analyzers** use server-side defaults set via the `PATCH /contentunderstanding/defaults` API — see the [parent README](../README.md#cu-defaults-configuration).

## Supported File Types

PDF, JPG, PNG, TIFF, BMP, DOCX, XLSX, PPTX, MP3, MP4, WAV, WebM
