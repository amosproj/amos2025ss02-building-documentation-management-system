FROM apache/tika:2.9.0-full

# Expose the default Tika port
EXPOSE 9998

# Set the heap size
ENV TIKA_JAVA_OPTS="-Xms512m -Xmx2g"

# Add command line parameters for enhanced functionality
CMD ["--host=0.0.0.0", "--port=9998", "--cors=*", "--includeStack=true"]

# Health check to verify the server is running
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD curl -f http://localhost:9998/tika || exit 1
