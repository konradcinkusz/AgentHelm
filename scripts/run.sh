#!/usr/bin/env bash
# AgentHelm launcher (release layout): Bridge + Web as local processes.
# Requires the .NET 8 ASP.NET Core runtime. Postgres (history) is optional —
# without it AgentHelm runs memory-only and says so in the logs.
set -euo pipefail
cd "$(dirname "$0")"

echo "AgentHelm — Bridge on http://127.0.0.1:5199"
dotnet bridge/AgentHelm.Bridge.dll &
BRIDGE_PID=$!
trap 'kill $BRIDGE_PID 2>/dev/null || true' EXIT

WEB_URLS="${AGENTHELM_WEB_URLS:-http://127.0.0.1:5200}"
echo "AgentHelm — Web on $WEB_URLS"
ASPNETCORE_URLS="$WEB_URLS" dotnet web/AgentHelm.Web.dll
