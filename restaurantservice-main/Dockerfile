# Base runtime image
FROM node:22-slim AS base

# Create non-root user for security
RUN groupadd -g 1001 restricted-group && \
    useradd -r -u 1001 -g restricted-group restricted-user

WORKDIR /app
EXPOSE 3000

# Set environment variables for production
ENV NODE_ENV=production

# Build stage
FROM node:22 AS build
WORKDIR /src

# Copy package files for dependency installation
COPY package*.json tsconfig.json ./
RUN npm ci

# Copy source code and build
COPY src/ src/
RUN npm run build

# Final runtime image
FROM base AS final

WORKDIR /app

# Copy package files and install production dependencies AS ROOT
COPY package*.json ./
RUN npm ci --production && npm cache clean --force

# Copy built application
COPY --from=build /src/dist/ ./dist/

# Change ownership of all files to restricted user
RUN chown -R restricted-user:restricted-group /app

# Switch to non-root user
USER restricted-user

# Use exec form for proper signal handling
CMD ["node", "dist/index.js"]