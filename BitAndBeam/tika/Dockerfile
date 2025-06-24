FROM apache/tika:latest
# Switch to root to install extra packages
USER root

# Install tesseract and language packs (English, German, French)
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       tesseract-ocr \
       tesseract-ocr-eng \
       tesseract-ocr-deu \
       tesseract-ocr-fra \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Copy custom Tika configuration enabling OCR
COPY tika-config.xml /tika-config.xml

# Expose the configuration path as env variable (Tika reads this automatically)
ENV TIKA_CONFIG=file:///tika-config.xml

# Run as root (apache/tika:latest may not define a 'tika' user)
# If non-root execution is required, create a user with `useradd` and adjust permissions.
# USER tika

# Default command provided by base image (no change)
CMD ["/docker-entrypoint.sh"]
