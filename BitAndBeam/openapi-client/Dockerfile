FROM bellsoft/liberica-openjdk-alpine:17

# Install dependencies, including dos2unix to fix CRLF
RUN apk add --no-cache bash curl wget dos2unix

# Download OpenAPI Generator CLI
RUN wget https://repo1.maven.org/maven2/org/openapitools/openapi-generator-cli/7.6.0/openapi-generator-cli-7.6.0.jar \
  -O /usr/local/bin/openapi-generator-cli.jar

# Create a shell wrapper script for the generator
RUN echo '#!/bin/sh' > /usr/local/bin/openapi-generator-cli && \
    echo 'exec java -jar /usr/local/bin/openapi-generator-cli.jar "$@"' >> /usr/local/bin/openapi-generator-cli && \
    chmod +x /usr/local/bin/openapi-generator-cli

# Copy and normalize generate-sdk.sh
COPY generate-sdk.sh /generate-sdk.sh
RUN dos2unix /generate-sdk.sh && chmod +x /generate-sdk.sh

ENTRYPOINT ["/generate-sdk.sh"]
