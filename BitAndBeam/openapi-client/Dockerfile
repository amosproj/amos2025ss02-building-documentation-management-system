FROM openjdk:17-alpine

# Install dependencies
RUN apk add --no-cache bash curl wget

# Download OpenAPI Generator CLI
RUN wget https://repo1.maven.org/maven2/org/openapitools/openapi-generator-cli/7.6.0/openapi-generator-cli-7.6.0.jar \
  -O /usr/local/bin/openapi-generator-cli.jar

# Create a proper shell wrapper script
RUN echo '#!/bin/sh' > /usr/local/bin/openapi-generator-cli && \
    echo 'exec java -jar /usr/local/bin/openapi-generator-cli.jar "$@"' >> /usr/local/bin/openapi-generator-cli && \
    chmod +x /usr/local/bin/openapi-generator-cli

# Copy SDK generation script
COPY generate-sdk.sh /generate-sdk.sh
RUN chmod +x /generate-sdk.sh

ENTRYPOINT ["/generate-sdk.sh"]
