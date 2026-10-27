#!/usr/bin/env bash
# Source from the repository root to configure the API and EF tools.
set -euo pipefail
if [[ ! -f .env ]]; then cp .env.example .env; fi
python3 - <<'GENERATE'
from pathlib import Path
import secrets
p = Path('.env')
s = p.read_text().replace('replace-with-a-strong-local-password', secrets.token_urlsafe(32))
s = s.replace('replace-with-a-random-secret-at-least-32-bytes-long', secrets.token_urlsafe(48))
if not any(line.startswith('Jwt__Key=') for line in s.splitlines()):
    s += '\nJwt__Key=' + secrets.token_urlsafe(48) + '\n'
p.write_text(s)
p.chmod(0o600)
GENERATE
set -a
source .env
set +a
export ConnectionStrings__DefaultConnection="Host=localhost;Port=${POSTGRES_PORT:-5432};Database=${POSTGRES_DB:-skillhub};Username=${POSTGRES_USER:-skillhub};Password=${POSTGRES_PASSWORD}"
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
