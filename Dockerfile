# AgentHelm — Bridge API + built-in echo agent.
# Build:  docker build -t agenthelm-bridge .
# Run:    docker run -p 5199:5199 -e AgentHelm__ApiToken=<token> agenthelm-bridge

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY nuget.config .
COPY src/AgentHelm.Bridge/ src/AgentHelm.Bridge/
COPY tools/AgentHelm.EchoAgent/ tools/AgentHelm.EchoAgent/
RUN dotnet publish src/AgentHelm.Bridge -c Release -o /app/publish \
 && dotnet publish tools/AgentHelm.EchoAgent -c Release -o /app/echo-agent \
 && apt-get update -qq && apt-get install -y -q jq \
 && jq '(.AgentHelm.Agents[] | select(.Id=="echo") | .Args) = ["/app/echo-agent/AgentHelm.EchoAgent.dll"]' \
       /app/publish/appsettings.json > /tmp/appsettings.json \
 && mv /tmp/appsettings.json /app/publish/appsettings.json

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /app/echo-agent /app/echo-agent/
ENV ASPNETCORE_ENVIRONMENT=Production
ENV AgentHelm__Urls=http://0.0.0.0:5199
EXPOSE 5199
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s \
  CMD wget -qO- http://localhost:5199/api/health || exit 1
ENTRYPOINT ["dotnet", "AgentHelm.Bridge.dll"]
