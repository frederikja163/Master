#!/bin/bash
set -e
# https://github.com/dotnet/spark/issues/1213#issuecomment-3055708997

# This script fixes the directory structure of the Microsoft.Spark NuGet package
# which can cause issues on non-Windows environments.

# Allow the version to be set via environment variable or first argument, default to 2.3.0
SPARK_VERSION="${SPARK_VERSION:-${1:-2.3.0}}"
PACKAGE_PATH="$HOME/.nuget/packages/microsoft.spark/$SPARK_VERSION"

if [ ! -d "$PACKAGE_PATH" ]; then
  echo "Microsoft Spark package not found at $PACKAGE_PATH"
  exit 1
fi

cd "$PACKAGE_PATH"

echo "Fixing Microsoft.Spark package structure..."

# Create necessary directories
mkdir -p _rels build/netstandard2.0 jars lib/netstandard2.0 lib/netstandard2.1

# find all files/folders in the package and rename them to the correct slash format
# This is necessary because the package may contain files with backslashes in their names
find . -type f -name '*\\*' | while IFS= read -r file; do
    normalized_path="${file//\\//}"
    if [[ "$normalized_path" == */ ]]; then
        mkdir -p "$normalized_path"
        rm -rfv "$file"
        continue
    fi
    mv -v "$file" "$normalized_path"
done

echo "Microsoft.Spark $SPARK_VERSION package fixed."