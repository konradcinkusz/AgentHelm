# AgentHelm — checklista publikacji (v0.1.0)

## Po Twojej stronie
- [ ] Repo `github.com/konradcinkusz/agenthelm` (public, MIT), push całości
- [ ] **Aspire działa lokalnie**: `dotnet run --project src/AgentHelm.AppHost` — po fixie SDK-element błąd workloadu znika; wymaga .NET SDK ≥ 8.0.303 (opcjonalnie: `dotnet workload uninstall aspire`, stary workload nie jest już potrzebny)
- [ ] Actions `ci` zielone (build + 41 testów na runnerze — pierwszy pełny restore NuGet, w tym AppHost/Npgsql, których sandbox nie mógł zweryfikować)
- [ ] Test zimnego demo: sesja echo → prompt "use the tool" → baner uprawnień → polityka yolo → wpis audytowy → History → Resume
- [ ] Prawdziwy agent: `copilot --acp --stdio` (pin wersji CLI + `--no-auto-update` — patrz README)
- [ ] Tag `v0.1.0` → workflow `release` buduje portable zip i publikuje GitHub Release
- [ ] Test zip-a wydania na czysto: rozpakuj → `./run.sh` → sesja echo działa
- [ ] Post ANNOUNCE-POST.md (dev.to/LinkedIn) po publikacji release'u
- [ ] Cross-link: w README CopilotScope dopisz zdanie o AgentHelm (para Scope/Helm) i odwrotnie — link już jest po stronie Helma

## Świadomie później (Beyond)
SDK finish (pakiet+flaga+4 TODO), tagowanie telemetrii dla dokładnej korelacji
Scope↔Helm, ConPTY, worktrees, pluginy.

## Definicja "opublikowane"
Obcy człowiek → znajduje repo → rozumie parę Scope/Helm → pobiera zip →
`./run.sh` → rozmawia z echo w 2 minuty → wie jak podpiąć prawdziwego agenta.
