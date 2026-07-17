# AgentHelm launcher (release layout): Bridge + Web as local processes.
# Requires the .NET 8 ASP.NET Core runtime.
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

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
