# AgentHelm launcher (release layout): Bridge + Web as local processes.
# Requires the .NET 8 ASP.NET Core runtime.
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

# OTEL preconfig — routes Copilot SDK telemetry to CopilotScope collector.
$Endpoint = if ($env:OTEL_EXPORTER_OTLP_ENDPOINT) { $env:OTEL_EXPORTER_OTLP_ENDPOINT } else { 'http://localhost:4318' }
$env:COPILOT_OTEL_ENABLED                          = 'true'
$env:COPILOT_OTEL_EXPORTER_TYPE                    = 'otlp-http'
$env:OTEL_EXPORTER_OTLP_ENDPOINT                   = $Endpoint
$env:OTEL_EXPORTER_OTLP_PROTOCOL                   = 'http/protobuf'
$env:OTEL_EXPORTER_OTLP_TRACES_PROTOCOL            = 'http/protobuf'
$env:OTEL_EXPORTER_OTLP_METRICS_PROTOCOL           = 'http/protobuf'
$env:OTEL_EXPORTER_OTLP_LOGS_PROTOCOL              = 'http/protobuf'
if (-not $env:OTEL_EXPORTER_OTLP_HEADERS)          { $env:OTEL_EXPORTER_OTLP_HEADERS = 'x-api-key=dev-secret-123' }
$env:OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT = 'true'

Write-Host "AgentHelm — Bridge on http://127.0.0.1:5199"
$bridge = Start-Process dotnet -ArgumentList "bridge/AgentHelm.Bridge.dll" -PassThru -NoNewWindow
try {
    $webUrls = if ($env:AGENTHELM_WEB_URLS) { $env:AGENTHELM_WEB_URLS } else { "http://127.0.0.1:5200" }
    Write-Host "AgentHelm — Web on $webUrls"
    $env:ASPNETCORE_URLS = $webUrls
    dotnet web/AgentHelm.Web.dll
}
finally {
    if ($bridge -and -not $bridge.HasExited) { Stop-Process -Id $bridge.Id -Force }
}
