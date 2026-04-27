#!/bin/bash
# Build script for macOS universal binary
# Run this on a Mac to create the .app bundle

PROJECT_NAME="WeathersnakeAvalonia"
OUTPUT_DIR="./publish"

echo "Building WeathersnakeAvalonia for macOS..."

# Build for x64
echo "Building for osx-x64..."
dotnet publish -r osx-x64 -c Release -p:PublishDirectory="$OUTPUT_DIR/osx-x64/publish"

# Build for arm64
echo "Building for osx-arm64..."
dotnet publish -r osx-arm64 -c Release -p:PublishDirectory="$OUTPUT_DIR/osx-arm64/publish"

# Create universal output directory
mkdir -p "$OUTPUT_DIR/Universal"

# Create universal binary using lipo
echo "Creating universal binary..."
lipo -create \
    "$OUTPUT_DIR/osx-x64/publish/$PROJECT_NAME" \
    "$OUTPUT_DIR/osx-arm64/publish/$PROJECT_NAME" \
    -output "$OUTPUT_DIR/Universal/$PROJECT_NAME"

echo "Universal binary created at $OUTPUT_DIR/Universal/$PROJECT_NAME"