FROM mcr.microsoft.com/dotnet/sdk:9.0

# Copy entrypoint script
COPY entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh

# Add dotnet tools to PATH permanently for all shell types
RUN echo 'export PATH="$PATH:/root/.dotnet/tools"' >> /root/.bashrc && \
    echo 'export PATH="$PATH:/root/.dotnet/tools"' >> /root/.profile && \
    echo 'export PATH="$PATH:/root/.dotnet/tools"' >> /etc/profile.d/dotnet-tools.sh

# Set entrypoint
ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]

