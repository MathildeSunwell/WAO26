# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Create non-root user for security
RUN groupadd -g 1001 restricted-group && \
    useradd -r -u 1001 -g restricted-group restricted-user

WORKDIR /app
EXPOSE 8080

# Set environment variables for production
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    ASPNETCORE_URLS=http://+:8080

# Build stage with SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only this service's .csproj
COPY *.csproj ./
RUN dotnet restore

# Copy rest of the source code
COPY . ./

# Build and publish
RUN dotnet publish -c Release -o /app/publish

# Final runtime image
FROM base AS final

# Switch to non-root user
USER restricted-user

WORKDIR /app

# Copy published app with proper ownership
COPY --from=build --chown=restricted-user:restricted-group /app/publish .

# Use exec form and specify the exact DLL name
ENTRYPOINT ["dotnet", "PaymentService.dll"]