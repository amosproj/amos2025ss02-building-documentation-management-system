<div align="center">
  <h1>Apache Tika + Tesseract OCR Pipeline</h1>
  <p>Document Processing and Text Extraction Service for BitAndBeam</p>
</div>

![Apache Tika](https://img.shields.io/badge/Apache_Tika-2.x-blue.svg)
![Tesseract OCR](https://img.shields.io/badge/Tesseract-5.0.0-green.svg)
![Docker](https://img.shields.io/badge/Docker-Enabled-2496ED.svg)

## Overview

This module implements the document processing pipeline for BitAndBeam using **Apache Tika 2.x** with Tesseract-based Optical Character Recognition (OCR). The service extracts text and metadata from various document formats including scanned PDFs and images.

## Key Components

| File / Dir | Purpose |
|------------|----------|
| `Dockerfile` | Builds a custom image on top of `apache/tika` that installs `tesseract‐ocr` and the language packs **ENG | DEU | FRA**. The image also copies the custom configuration. |
| `tika-config.xml` | Enables `TesseractOCRParser`, configures OCR parameters, and sets performance limits. |

### Configuration Details

The `tika-config.xml` file contains several important configuration parameters:

```xml
<!-- Key configuration settings -->
<properties>
  <parsers>
    <parser class="org.apache.tika.parser.ocr.TesseractOCRParser"/>
  </parsers>
  <service-loader initializableProblemHandler="ignore"/>
  <tika-config pdfocrStrategy="auto" language="eng+deu+fra" maxExtractLength="1000000" ocrTimeoutSeconds="20"/>
</properties>
```

These settings control:

- **OCR Strategy**: Automatically determines when to apply OCR
- **Languages**: English, German, and French support by default
- **Performance Limits**: Timeout of 20 seconds, max extract size of 1MB

## Processing Flow

```mermaid
flowchart TD
    A[Client uploads file] -->|multipart/form-data| B(Backend API)
    B --> C{{/meta request}}
    C -->|extractable text large enough| D[Return text & metadata]
    C -->|little / no text| E[/tika request with OCR headers]
    E --> F[OCR text + metadata]
    F --> D
```

### Step-by-Step Process

1. **Initial Text Extraction**:  
   When a document is uploaded, the backend first attempts to extract text without OCR for performance optimization.

2. **Content Evaluation**:  
   The system checks if sufficient text was extracted (>50 characters).
   - If sufficient text was found, processing is complete.
   - If little or no text was found, the document likely contains scanned content or images.

3. **OCR Application**:  
   For documents with insufficient text, the system sends the file again with specific OCR headers:
   ```
   X-Tika-PDFOcrStrategy: auto
   X-Tika-OCRLanguage: eng+deu+fra
   ```

4. **Performance Protection**:  
   All requests include performance safeguards:
   ```
   X-Tika-OCRTimeoutMillis: 20000
   X-Tika-MaxExtract: 1000000
   ```

5. **Result Handling**:  
   Extracted text and metadata, along with an `ocrApplied` flag, are returned to the application.

## Installation & Usage

### Building & Running

```bash
# Rebuild the custom image if config or Dockerfile changes
docker compose build tika

# (Re)start only Tika + dependent backend service
docker compose up -d tika backend
```

The container exposes **port 9998** by default, matching the upstream Apache Tika image.

### API Endpoints

| Endpoint | Description |
|----------|-------------|
| `/tika` | Main content extraction endpoint |
| `/meta` | Metadata extraction only |
| `/version` | Tika version information |
| `/parsers` | List of available parsers |
| `/detectors` | List of available detectors |
| `/mime-types` | List of supported MIME types |

### Performance Optimization

Tika OCR operations can be resource-intensive. For optimal performance:

- Set appropriate timeout values based on document complexity
- Limit the maximum extract size to prevent memory issues
- Use language-specific OCR only when necessary (improves accuracy and speed)

## Configuration

### Adding New Languages

1. Edit `Dockerfile` to install additional language packages:
   ```dockerfile
   RUN apt-get update && apt-get install -y \
       tesseract-ocr \
       tesseract-ocr-eng \
       tesseract-ocr-deu \
       tesseract-ocr-fra \
       tesseract-ocr-<new-lang>
   ```

2. Update the language configuration in `tika-config.xml`:
   ```xml
   <tika-config pdfocrStrategy="auto" language="eng+deu+fra+<new-lang>" ... />
   ```

3. Rebuild and restart the container:
   ```bash
   docker compose build tika && docker compose up -d tika
   ```

### Health Monitoring

The system includes a health check endpoint at `/healthz/tika` that verifies:

* Tika server responds to `/version` endpoint
* `TesseractOCRParser` is properly registered and available

## Troubleshooting

### Common Issues

| Problem | Possible Solutions |
|---------|--------------------|
| OCR timeout errors | Increase `ocrTimeoutSeconds` in config or request headers |
| Poor text recognition | Add language packs specific to the document content |
| Out of memory errors | Reduce `maxExtractLength` or increase container memory |
| Service unavailable | Check Docker logs with `docker compose logs tika` |

### Debugging OCR Quality

To debug OCR quality issues, you can use the direct Tika API to test specific files:

```bash
curl -T sample.pdf http://localhost:9998/tika --header "X-Tika-OCRLanguage: eng+deu+fra"
```

## Resources

- [Apache Tika Documentation](https://tika.apache.org/2.0.0/index.html)
- [Tesseract OCR Documentation](https://tesseract-ocr.github.io/tessdoc/)
- [OCR Language Data Files](https://github.com/tesseract-ocr/tessdata)

---

> Need help debugging OCR or extending the pipeline? Ping the backend team in `#bitandbeam-backend`. 🚀
