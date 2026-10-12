#!/bin/bash
# Build script for ElectronCash Client
# Publishes as self-contained standalone executable to ./build folder
# Usage: ./build.sh

set -e

PROJECT_PATH="ElectronCash/src/ElectronCash.Client/ElectronCash.Client.csproj"
BUILD_DIR="./build"
CONFIGURATION="Release"
RUNTIME="win-x64"  # For Windows target

echo "========================================"
echo "ElectronCash Client Build Script"
echo "========================================"
echo ""

# Clean build directory
echo "Cleaning build directory..."
rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR"

# Clean obj/bin
echo "Cleaning project artifacts..."
rm -rf "ElectronCash/src/ElectronCash.Client/bin"
rm -rf "ElectronCash/src/ElectronCash.Client/obj"

# Build solution
echo "Building solution..."
dotnet build ElectronCash.sln -c "$CONFIGURATION"

if [ $? -ne 0 ]; then
    echo "Build failed!"
    exit 1
fi

# Publish as self-contained
echo ""
echo "Publishing self-contained executable for $RUNTIME..."
dotnet publish "$PROJECT_PATH" \
    -c "$CONFIGURATION" \
    --self-contained \
    --runtime "$RUNTIME" \
    -o "$BUILD_DIR/release"

if [ $? -ne 0 ]; then
    echo "Publish failed!"
    exit 1
fi

# Copy executable to root of build
EXE_PATH="$BUILD_DIR/release/ElectronCash.Client.exe"
if [ -f "$EXE_PATH" ]; then
    cp "$EXE_PATH" "$BUILD_DIR/"
    echo ""
    echo "✓ Build complete!"
    echo "  Executable: $BUILD_DIR/ElectronCash.Client.exe"
    echo "  Full build: $BUILD_DIR/release/"
    echo ""
else
    echo "Error: Executable not found at $EXE_PATH"
    exit 1
fi
