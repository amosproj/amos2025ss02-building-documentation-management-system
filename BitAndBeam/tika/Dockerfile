FROM apache/tika:latest

USER root
RUN apt-get update && \
    apt-get install -y tesseract-ocr && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

EXPOSE 9998

ENV TIKA_JAVA_OPTS="-Xms512m -Xmx2g"

CMD ["java", "-jar", "/tika-server.jar", "-h", "0.0.0.0", "-p", "9998"]

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD curl -f http://localhost:9998/healthcheck || exit 1
