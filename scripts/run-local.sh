#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
source scripts/load-env.sh
docker compose up -d --wait
exec dotnet run --project src/SkillHub.Api --no-launch-profile --urls http://localhost:5080
