# Apache Tika (+ Tesseract) OCR Pipeline

This folder contains everything required to run **Apache Tika 2.x** with Tesseract-based Optical Character Recognition (OCR) inside the Bit&Beam stack.

## 1. Components

| File / Dir | Purpose |
|------------|---------|
| `Dockerfile` | Builds a custom image on top of `apache/tika` that installs `tesseract‐ocr` and the language packs **ENG | DEU | FRA**. The image also copies the custom configuration below. |
| `tika-config.xml` | Enables `TesseractOCRParser`, sets `ocrStrategy=auto`, default languages, a 20 s OCR timeout, and a 1 MB max-extract cap. |

## 2. Runtime Flow

```mermaid
flowchart TD
    A[Client uploads file] -->|multipart/form-data| B(Backend API)
    B --> C{{/meta request}}
    C -->|extractable text large enough| D[Return text & metadata]
    C -->|little / no text| E[/tika request with OCR headers]
    E --> F[OCR text + metadata]
    F --> D
```

1. **Initial extraction (no OCR):**  
   `TikaController` posts the file to `/tika` *without* OCR headers. This is fast and skips heavy processing for normal PDFs/Office docs.
2. **Heuristic check:** If the extracted text is empty (or < 50 chars), the file is likely scanned or an image.
3. **OCR retry:** Controller resends the same file with headers:
   * `X-Tika-PDFOcrStrategy: auto`  
   * `X-Tika-OCRLanguage: eng+deu+fra`
4. **Performance guards** are always sent:  
   `X-Tika-OCRTimeoutMillis: 20000`, `X-Tika-MaxExtract: 1000000`.
5. Results (plus `ocrApplied` flag) are returned to the frontend.

## 3. Building & Running

```
# Rebuild the custom image if config or Dockerfile changes
docker compose build tika

# (Re)start only Tika + dependent backend service
docker compose up -d tika backend
```

The container exposes the same **port 9998** as the upstream image, so no other service changes are required.

## 4. Adding More Languages

1. Edit `Dockerfile` — add `tesseract-ocr-<lang>` packages.  
2. Update `tika-config.xml` `language` param and/or backend header.
3. `docker compose build tika && docker compose up -d tika`.

## 5. Health Check

`/healthz/tika` (implemented in `TikaHealthCheck`) now verifies:
* Tika responds to `/version` **and**
* `TesseractOCRParser` appears in the parser list.

---

> Need help debugging OCR or extending the pipeline? Ping the backend team in `#bitandbeam-backend`. 🚀
