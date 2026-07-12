#! /usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONFIGURATION="${1:-Release}"
ARTIFACTS="${ROOT}/artifacts"

cd "${ROOT}"

dotnet restore p99.NET.sln
dotnet build p99.NET.sln --configuration "${CONFIGURATION}" --no-restore
dotnet test p99.NET.sln --configuration "${CONFIGURATION}" --no-build --verbosity normal

mkdir -p "${ARTIFACTS}/packages"
dotnet pack src/P99/P99.csproj \
  --configuration "${CONFIGURATION}" \
  --no-build \
  --output "${ARTIFACTS}/packages"

echo "Packages written to ${ARTIFACTS}/packages"
