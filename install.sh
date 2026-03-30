#!/bin/bash
set -e

APP_NAME="screenctl"
SHARE_DIR="$HOME/.local/share/$APP_NAME"
BIN_DIR="$HOME/.local/bin"

echo "Publishing $APP_NAME..."
dotnet publish src/ScreenDriver -c Release -r linux-x64 --self-contained \
    -p:PublishSingleFile=true -p:PublishTrimmed=true -o ./publish

echo "Installing to $SHARE_DIR..."
mkdir -p "$SHARE_DIR"
rm -rf "$SHARE_DIR"/*
cp ./publish/ScreenDriver "$SHARE_DIR/$APP_NAME"
cp ./publish/libSkiaSharp.so "$SHARE_DIR/"
cp ./publish/libSystem.IO.Ports.Native.so "$SHARE_DIR/"
cp -r ./publish/themes "$SHARE_DIR/"

chmod +x "$SHARE_DIR/$APP_NAME"

echo "Creating symlink in $BIN_DIR..."
mkdir -p "$BIN_DIR"
ln -sf "$SHARE_DIR/$APP_NAME" "$BIN_DIR/$APP_NAME"

if ! echo "$PATH" | grep -q "$BIN_DIR"; then
    echo ""
    echo "WARNING: $BIN_DIR is not in your PATH."
    echo "Add this to your ~/.bashrc or ~/.zshrc:"
    echo "  export PATH=\"\$HOME/.local/bin:\$PATH\""
fi

echo ""
echo "Done. Run '$APP_NAME' from anywhere."
