#!/usr/bin/env sh
# Sugeneruoja src/api/schema.d.ts iš ../Synq.yaml. Paleisti po kiekvieno Synq.yaml pakeitimo, tada `npm run build`.
set -e
cd "$(dirname "$0")/.."
npx openapi-typescript ../Synq.yaml -o src/api/schema.d.ts
