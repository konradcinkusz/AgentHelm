// AgentHelm AppHost — Aspire orchestration.
// NOTE: Bridge and Web run as LOCAL PROCESSES on purpose: ACP agents are local
// subprocesses (remote transport is still an RFC), so the Bridge must live on
// the machine that has the repositories and the agent binaries. Only Postgres
// is a container.
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("agenthelm-pgdata")
    .WithPgAdmin();
var helmDb = postgres.AddDatabase("helmdb");

var bridge = builder.AddProject<Projects.AgentHelm_Bridge>("bridge")
    .WithReference(helmDb)
    .WaitFor(helmDb)
    .WithEndpoint("http", e => { e.Port = 5199; e.TargetPort = 5199; e.IsProxied = false; });

builder.AddProject<Projects.AgentHelm_Web>("web")
    .WithReference(bridge)
    .WaitFor(bridge);

builder.Build().Run();
