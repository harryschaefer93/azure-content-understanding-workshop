# Sample Documents

Synthetic sample PDFs for workshop exercises. These documents are AI-generated and contain no real personal or business data.

## Included Samples

| Folder | Count | Description |
|--------|-------|-------------|
| `commitment-letters/` | 5 | Synthetic mortgage commitment letters with borrower names, addresses, loan terms, and solicitor conditions |
| `title-search/` | 2 | Synthetic title search reports with registered owners, legal descriptions, and encumbrances |

## Test Manifests

- `commitment-letter-manifest.json` — skeleton for batch Test Suite runs against commitment letters
- `title-search-manifest.json` — skeleton for batch Test Suite runs against title searches

Fill in `expectedFields` in the manifests after running an initial analysis pass in the harness.

## Bring Your Own Documents

To test with your own documents:

1. Add PDFs (or images, Office docs, audio, video) to a subfolder here.
2. Optionally create a manifest JSON following the same structure as the existing examples.
3. Upload via the harness UI or reference in the Test Suite page.
