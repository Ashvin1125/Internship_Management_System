# ─────────────────────────────────────────────
# Stage 1: Build
# ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj first (layer cache optimisation)
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the source
COPY . ./

# Publish in Release mode
RUN dotnet publish -c Release -o /app/out --no-restore

# ─────────────────────────────────────────────
# Stage 2: Runtime
# ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install SQLite runtime dependency
RUN apt-get update && apt-get install -y libsqlite3-0 && rm -rf /var/lib/apt/lists/*

# Copy published output from build stage
COPY --from=build /app/out ./

# Ensure the uploads directory exists inside the container
RUN mkdir -p wwwroot/uploads

# Environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# Expose the default port (Render/Railway override via $PORT at runtime)
EXPOSE 8080

# Set entry point
ENTRYPOINT ["dotnet", "InternshipManagementSystem.dll"]
