# Changelog

## Holochain 0.6.1 Wire Protocol Upgrade

Earlier "version bump" work only updated version strings/comments without changing the actual
wire protocol, leaving HoloNET's real on-the-wire shapes at roughly Holochain 0.2.x-0.3.x while
docs claimed 0.5.6. This upgrade re-verifies and re-implements the wire protocol against the real
Holochain 0.6.1 Rust source (`holochain_conductor_api`, `holochain_types`, `holochain_zome_types`
at tag `holochain-0.6.1` on GitHub) so HoloNET's serialization actually matches a 0.6.1 conductor.

Key changes:

- **Zome call signing payload** (`Data/App/Requests/ZomeCall.cs`, `ZomeCallSigned.cs`): replaced
  the old flattened `cell_id_dna_hash` / `cell_id_agent_pub_key` byte arrays with a proper
  `CellId` tuple type (`Data/App/Requests/CellId.cs`) matching
  `holochain_zome_types::cell::CellId(DnaHash, AgentPubKey)`. Added
  `Data/App/Requests/ZomeCallParamsSigned.cs` mirroring the real 0.6.1 wire shape sent over the
  app websocket interface (`holochain_conductor_api::app_interface::ZomeCallParamsSigned { bytes,
  signature }`), used by `HoloNETClientAppBase` when constructing `call_zome` requests.
- **New Admin/App message types** added to `Enums/HoloNETRequestType.cs` /
  `HoloNETResponseType.cs` with corresponding request/response classes under
  `Data/Admin/Requests`, `Data/Admin/Responses`, `Data/App/Requests`, `Data/App/Responses`:
  RevokeZomeCallCapability, ListCapabilityGrants, PeerMetaInfo, IssueAppAuthenticationToken,
  RevokeAppAuthenticationToken, GetCompatibleCells, CreateCloneCell/EnableCloneCell/
  DisableCloneCell, countersigning, ListWasmHostFunctions, ProvideMemproofs.
- **Typed DumpNetworkStats/DumpFullState responses**: `DumpNetworkStatsResponse` (with nested
  transport stats) and `FullStateDumpedResponse`/`IFullStateDumpedResponse` give typed,
  deserialized access to conductor `dump_network_stats` / `dump_state` data, while the legacy
  raw-JSON string fields (`NetworkStatsDumpJSON`, `DumpedStateJSON`) are kept as an escape hatch
  for callers depending on the old shape.
- **`NetworkConfig`** (`Data/Config/NetworkConfig.cs`) added and wired into `HoloNETDNA` to carry
  `RequestTimeoutS`, matching the move of `request_timeout_s` from `ConductorConfig` directly to
  `ConductorConfig.network` (`NetworkConfig`) in Holochain 0.6.1.
- **`CallZomeOptions`** (`Data/App/Requests/CallZomeOptions.cs`) added as an optional trailing
  parameter on `CallZomeFunctionAsync`, allowing a per-call timeout override that falls back to
  `HoloNETDNA.NetworkConfig.RequestTimeoutS`. Existing overloads remain unchanged/backward
  compatible.
- **Removed dead scaffolding**: `InitializeIntegratedKeystoreAsync` /
  `InitializeCachingLayerAsync` / `InitializeWASMOptimizationAsync` and their `Task.Delay`-only
  helper methods (the "Holochain 0.5.6+ Enhanced Features" block in `HoloNETClientBase.cs`), plus
  the unused `KeystoreConfig`/`CacheConfig`/`WASMConfig` classes and the corresponding
  `HoloNETDNA`/`IHoloNETDNA` properties. None of this mapped to any real Holochain wire protocol
  or conductor config; it was pure unused stub code with no callers outside itself. `Kitsune2Config`
  and `QUICConfig` were removed for the same reason.
- **TestHarness / doc-comment cleanup**: updated hardcoded `holochain-0.1.5` conductor/happ paths
  in `NextGenSoftware.Holochain.HoloNET.Client.TestHarness/HoloNETTestHarness.cs` to `0.6.1`, and
  fixed `docs.rs/holochain_types/0.2.1` doc-comment links (in `InstallAppRequest.cs`,
  `CoordinatorSource.cs`) to point at `docs.rs/holochain_types/0.6.1`, which is the matching crate
  version for the `holochain-0.6.1` release.

### Not verified

- No authoritative Rust struct could be found for a `CallZomeOptions`-equivalent wire type in
  `holochain_zome_types` / `holochain_conductor_api` / `holochain_types` at tag `holochain-0.6.1`;
  `CallZomeOptions` here is a HoloNET-only client-side convenience type (see its doc comment),
  not a verified mirror of a Rust struct.
