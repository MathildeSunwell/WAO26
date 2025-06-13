# ---------- STAGE 1: Base runtime image ----------
    
# This image contains only the ASP.NET Core runtime 
# Used as the final image to run the application in production

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Create a non-root system user for better security
RUN groupadd -g 1001 restricted-group && \
    useradd -r -u 1001 -g restricted-group restricted-user

# -g 1001: creates group with GID 1001
# -u 1001: creates user with UID 1001
# -r: marks the user as a system user (non-login)
# UID 0 = root; 1–999 = reserved for system accounts; 1000+ = normal users

# Set working directory
WORKDIR /app

# Expose port 8080 for incoming HTTP traffic
EXPOSE 8080

# Set environment variables for runtime configuration
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    ASPNETCORE_URLS=http://+:8080

# DOTNET_RUNNING_IN_CONTAINER: optimizes .NET for container environments
# ASPNETCORE_URLS: binds the app to port 8080 on all interfaces


# ---------- STAGE 2: Build stage with Software Development Kit (SDK) ----------

# This image includes the .NET SDK, needed to build and publish the app
# Separated from the final image to reduce size and attack surface

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set working directory for the build context
WORKDIR /src

# Copy only the project file first
COPY *.csproj ./

# Restore dependencies based on the .csproj file
RUN dotnet restore

# Copy the rest of the source code
COPY . ./

# Build and publish the application to a folder for deployment
RUN dotnet publish -c Release -o /app/publish

# -c Release: builds in Release mode
# -o /app/publish: sets output directory for compiled app


# ---------- STAGE 3: Final runtime image ----------
# Use the base runtime image to keep the final image slim

FROM base AS final

# Switch to the non-root user
USER restricted-user

# Set working directory again in this final stage
WORKDIR /app

# Copy the published app from the build stage and set correct file ownership
COPY --from=build --chown=restricted-user:restricted-group /app/publish .

# --from=build: copy from the build stage defined above
# --chown=...: set ownership to match the non-root user

# Set the entry point for the container using exec form
ENTRYPOINT ["dotnet", "PaymentService.dll"]


