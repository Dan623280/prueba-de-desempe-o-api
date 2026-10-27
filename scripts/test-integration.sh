#!/usr/bin/env bash
# Uses its own temporary database and API process; never writes test data into skillhub.
set -euo pipefail
cd "$(dirname "$0")/.."
source scripts/load-env.sh
docker compose up -d --wait
test_database="skillhub_test_$(date +%s)_${RANDOM}"
log_file=$(mktemp /tmp/skillhub-integration.XXXXXX.log)
api_pid=''
cleanup() {
  if [[ -n "$api_pid" ]]; then kill "$api_pid" 2>/dev/null || true; wait "$api_pid" 2>/dev/null || true; fi
  docker compose exec -T postgres sh -c 'dropdb --if-exists -U "$POSTGRES_USER" "$1"' sh "$test_database"
}
trap cleanup EXIT
docker compose exec -T postgres sh -c 'createdb -U "$POSTGRES_USER" "$1"' sh "$test_database"
export ConnectionStrings__DefaultConnection="Host=localhost;Port=${POSTGRES_PORT:-5432};Database=${test_database};Username=${POSTGRES_USER:-skillhub};Password=${POSTGRES_PASSWORD}"
export SKILLHUB_URL=http://localhost:5081
export ASPNETCORE_ENVIRONMENT=Development
dotnet src/SkillHub.Api/bin/Debug/net10.0/SkillHub.Api.dll --urls "$SKILLHUB_URL" --contentRoot "$(pwd)/src/SkillHub.Api" > "$log_file" 2>&1 &
api_pid=$!
for attempt in {1..60}; do
  if curl -fsS "$SKILLHUB_URL/api/v1/workshops" >/dev/null 2>&1; then break; fi
  if ! kill -0 "$api_pid" 2>/dev/null; then cat "$log_file"; exit 1; fi
  sleep 1
done
if ! python3 tests/integration.py; then tail -100 "$log_file"; exit 1; fi
if [[ "${RUN_BROWSER_TESTS:-0}" == "1" ]]; then node tests/browser/participant-flow.cjs; fi
printf 'API integration checks passed. Server log: %s\n' "$log_file"
