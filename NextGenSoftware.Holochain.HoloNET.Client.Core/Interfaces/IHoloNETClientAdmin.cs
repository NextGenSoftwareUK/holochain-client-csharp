﻿using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects;
using NextGenSoftware.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.Client.Interfaces
{
    public interface IHoloNETClientAdmin : IHoloNETClientBase
    {
        event HoloNETClientAdmin.AdminInterfacesAddedCallBack OnAdminInterfacesAddedCallBack;
        event HoloNETClientAdmin.AgentInfoAddedCallBack OnAgentInfoAddedCallBack;
        event HoloNETClientAdmin.AgentInfoReturnedCallBack OnAgentInfoReturnedCallBack;
        event HoloNETClientAdmin.AgentPubKeyGeneratedCallBack OnAgentPubKeyGeneratedCallBack;
        event HoloNETClientAdmin.AppDisabledCallBack OnAppDisabledCallBack;
        event HoloNETClientAdmin.AppEnabledCallBack OnAppEnabledCallBack;
        event HoloNETClientAdmin.AppInstalledCallBack OnAppInstalledCallBack;
        event HoloNETClientAdmin.AppInterfaceAttachedCallBack OnAppInterfaceAttachedCallBack;
        event HoloNETClientAdmin.AppInterfacesListedCallBack OnAppInterfacesListedCallBack;
        event HoloNETClientAdmin.AppsListedCallBack OnAppsListedCallBack;
        event HoloNETClientAdmin.AppUninstalledCallBack OnAppUninstalledCallBack;
        event HoloNETClientAdmin.CellIdsListedCallBack OnCellIdsListedCallBack;
        event HoloNETClientAdmin.CloneCellDeletedCallBack OnCloneCellDeletedCallBack;
        event HoloNETClientAdmin.CoordinatorsUpdatedCallBack OnCoordinatorsUpdatedCallBack;
        event HoloNETClientAdmin.DnaDefinitionReturnedCallBack OnDnaDefinitionReturnedCallBack;
        event HoloNETClientAdmin.DnaRegisteredCallBack OnDnaRegisteredCallBack;
        event HoloNETClientAdmin.DnasListedCallBack OnDnasListedCallBack;
        event HoloNETClientAdmin.FullStateDumpedCallBack OnFullStateDumpedCallBack;
        event HoloNETClientAdmin.InstallEnableSignAndAttachHappCallBack OnInstallEnableSignAndAttachHappCallBack;
        event HoloNETClientAdmin.InstallEnableSignAttachAndConnectToHappCallBack OnInstallEnableSignAttachAndConnectToHappCallBack;
        event HoloNETClientAdmin.NetworkMetricsDumpedCallBack OnNetworkMetricsDumpedCallBack;
        event HoloNETClientAdmin.NetworkStatsDumpedCallBack OnNetworkStatsDumpedCallBack;
        event HoloNETClientAdmin.RecordsGraftedCallBack OnRecordsGraftedCallBack;
        event HoloNETClientAdmin.StateDumpedCallBack OnStateDumpedCallBack;
        event HoloNETClientAdmin.StorageInfoReturnedCallBack OnStorageInfoReturnedCallBack;
        event HoloNETClientAdmin.ZomeCallCapabilityGrantedCallBack OnZomeCallCapabilityGrantedCallBack;

        AgentInfoAddedCallBackEventArgs AddAgentInfo(AgentInfo[] agentInfos, string id = null);
        Task<AgentInfoAddedCallBackEventArgs> AddAgentInfoAsync(AgentInfo[] agentInfos, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        AppInterfaceAttachedCallBackEventArgs AttachAppInterface(ushort? port = null, string id = null);
        Task<AppInterfaceAttachedCallBackEventArgs> AttachAppInterfaceAsync(ushort? port = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        ZomeCallCapabilityGrantedCallBackEventArgs AuthorizeSigningCredentialsAndGrantZomeCallCapability(byte[] AgentPubKey, byte[] DnaHash, CapGrantAccessType capGrantAccessType, GrantedFunctionsType grantedFunctionsType, List<(string, string)> functions = null, string id = "");
        ZomeCallCapabilityGrantedCallBackEventArgs AuthorizeSigningCredentialsAndGrantZomeCallCapability(byte[][] cellId, CapGrantAccessType capGrantAccessType, GrantedFunctionsType grantedFunctionsType, List<(string, string)> functions = null, string id = "");
        ZomeCallCapabilityGrantedCallBackEventArgs AuthorizeSigningCredentialsAndGrantZomeCallCapability(string AgentPubKey, string DnaHash, CapGrantAccessType capGrantAccessType, GrantedFunctionsType grantedFunctionsType, List<(string, string)> functions = null, string id = "");
        Task<ZomeCallCapabilityGrantedCallBackEventArgs> AuthorizeSigningCredentialsAndGrantZomeCallCapabilityAsync(byte[] AgentPubKey, byte[] DnaHash, CapGrantAccessType capGrantAccessType, GrantedFunctionsType grantedFunctionsType, List<(string, string)> functions = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = "");
        Task<ZomeCallCapabilityGrantedCallBackEventArgs> AuthorizeSigningCredentialsAndGrantZomeCallCapabilityAsync(byte[][] cellId, CapGrantAccessType capGrantAccessType, GrantedFunctionsType grantedFunctionsType, List<(string, string)> functions = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = "");
        Task<ZomeCallCapabilityGrantedCallBackEventArgs> AuthorizeSigningCredentialsAndGrantZomeCallCapabilityAsync(string AgentPubKey, string DnaHash, CapGrantAccessType capGrantAccessType, GrantedFunctionsType grantedFunctionsType, List<(string, string)> functions = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = "");
        HoloNETConnectedEventArgs Connect(string holochainConductorURI = "", bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = false, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateIHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true);
        HoloNETConnectedEventArgs Connect(Uri holochainConductorURI, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateIHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true);
        Task<HoloNETConnectedEventArgs> ConnectAsync(string holochainConductorURI = "", ConnectedCallBackMode connectedCallBackMode = ConnectedCallBackMode.WaitForHolochainConductorToConnect, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateIHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true);
        Task<HoloNETConnectedEventArgs> ConnectAsync(Uri holochainConductorURI, ConnectedCallBackMode connectedCallBackMode = ConnectedCallBackMode.WaitForHolochainConductorToConnect, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateIHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true);
        CloneCellDeletedCallBackEventArgs DeleteCloneCell(string appId, string id = null);
        CloneCellDeletedCallBackEventArgs DeleteCloneCell(string appId, byte[][] cellId, string id = null);
        CloneCellDeletedCallBackEventArgs DeleteCloneCell(string appId, string roleName, string id = null);
        CloneCellDeletedCallBackEventArgs DeleteCloneCell(string appId, string agentPubKey, string dnaHash, string id = null);
        Task<CloneCellDeletedCallBackEventArgs> DeleteCloneCellAsync(string appId, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        Task<CloneCellDeletedCallBackEventArgs> DeleteCloneCellAsync(string appId, byte[][] cellId, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        Task<CloneCellDeletedCallBackEventArgs> DeleteCloneCellAsync(string appId, string roleName, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        Task<CloneCellDeletedCallBackEventArgs> DeleteCloneCellAsync(string appId, string agentPubKey, string dnaHash, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        AppDisabledCallBackEventArgs DisableApp(string installedAppId, string id = null);
        Task<AppDisabledCallBackEventArgs> DisableAppAsync(string installedAppId, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        FullStateDumpedCallBackEventArgs DumpFullState(int? dHTOpsCursor = null, string id = null);
        FullStateDumpedCallBackEventArgs DumpFullState(byte[][] cellId, int? dHTOpsCursor = null, string id = null);
        FullStateDumpedCallBackEventArgs DumpFullState(string agentPubKey, string dnaHash, int? dHTOpsCursor = null, string id = null);
        Task<FullStateDumpedCallBackEventArgs> DumpFullStateAsync(int? dHTOpsCursor = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        Task<FullStateDumpedCallBackEventArgs> DumpFullStateAsync(byte[][] cellId, int? dHTOpsCursor = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        Task<FullStateDumpedCallBackEventArgs> DumpFullStateAsync(string agentPubKey, string dnaHash, int? dHTOpsCursor = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        NetworkMetricsDumpedCallBackEventArgs DumpNetworkMetrics(string id = null);
        Task<NetworkMetricsDumpedCallBackEventArgs> DumpNetworkMetricsAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        NetworkStatsDumpedCallBackEventArgs DumpNetworkStats(string id = null);
        Task<NetworkStatsDumpedCallBackEventArgs> DumpNetworkStatsAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        StateDumpedCallBackEventArgs DumpState(int? dHTOpsCursor = null, string id = null);
        StateDumpedCallBackEventArgs DumpState(byte[][] cellId, int? dHTOpsCursor = null, string id = null);
        StateDumpedCallBackEventArgs DumpState(string agentPubKey, string dnaHash, int? dHTOpsCursor = null, string id = null);
        Task<StateDumpedCallBackEventArgs> DumpStateAsync(int? dHTOpsCursor = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        Task<StateDumpedCallBackEventArgs> DumpStateAsync(byte[][] cellId, int? dHTOpsCursor = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        Task<StateDumpedCallBackEventArgs> DumpStateAsync(string agentPubKey, string dnaHash, int? dHTOpsCursor = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        Task<AppEnabledCallBackEventArgs> EnableAppAsync(string installedAppId, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        AppEnabledCallBackEventArgs EnablelApp(string installedAppId, string id = null);
        void GenerateAgentPubKey(bool updateAgentPubKeyInIHoloNETDNA = true, string id = "");
        Task<AgentPubKeyGeneratedCallBackEventArgs> GenerateAgentPubKeyAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, bool updateAgentPubKeyInIHoloNETDNA = true, string id = "");
        AgentInfoReturnedCallBackEventArgs GetAgentInfo(string id = null);
        AgentInfoReturnedCallBackEventArgs GetAgentInfo(byte[][] cellId, string id = null);
        AgentInfoReturnedCallBackEventArgs GetAgentInfo(string agentPubKey, string dnaHash, string id = null);
        Task<AgentInfoReturnedCallBackEventArgs> GetAgentInfoAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        Task<AgentInfoReturnedCallBackEventArgs> GetAgentInfoAsync(byte[][] cellId, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        Task<AgentInfoReturnedCallBackEventArgs> GetAgentInfoAsync(string agentPubKey, string dnaHash, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        GetAppInfoCallBackEventArgs GetAppInfo(string installedAppId, string id = null);
        Task<GetAppInfoCallBackEventArgs> GetAppInfoAsync(string installedAppId, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        byte[] GetCapGrantSecret(byte[][] cellId);
        byte[] GetCapGrantSecret(string agentPubKey, string dnaHash);
        Task<byte[][]> GetCellIdAsync();
        DnaDefinitionReturnedCallBackEventArgs GetDnaDefinition(byte[] dnaHash, string id = null);
        DnaDefinitionReturnedCallBackEventArgs GetDnaDefinition(string dnaHash, string id = null);
        Task<DnaDefinitionReturnedCallBackEventArgs> GetDnaDefinitionAsync(byte[] dnaHash, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        Task<DnaDefinitionReturnedCallBackEventArgs> GetDnaDefinitionAsync(string dnaHash, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        StorageInfoReturnedCallBackEventArgs GetStorageInfo(string id = null);
        Task<StorageInfoReturnedCallBackEventArgs> GetStorageInfoAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        RecordsGraftedCallBackEventArgs GraftRecords(byte[][] cellId, bool validate, object[] records, string id = null);
        Task<RecordsGraftedCallBackEventArgs> GraftRecordsAsync(byte[][] cellId, bool validate, object[] records, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        AppInstalledCallBackEventArgs InstallApp(string installedAppId, AppBundle appBundle, string agentKey = null, Dictionary<string, byte[]> membraneProofs = null, string network_seed = null, string id = null);
        void InstallApp(string agentKey, string installedAppId, string hAppPath, Dictionary<string, byte[]> membraneProofs = null, string network_seed = null, string id = null);
        Task<AppInstalledCallBackEventArgs> InstallAppAsync(string installedAppId, AppBundle appBundle, string agentKey = null, Dictionary<string, byte[]> membraneProofs = null, string network_seed = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        Task<AppInstalledCallBackEventArgs> InstallAppAsync(string installedAppId, string hAppPath, string agentKey = null, Dictionary<string, byte[]> membraneProofs = null, string network_seed = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        Task<InstallEnableSignAndAttachHappEventArgs> InstallEnableSignAndAttachHappAsync(string hAppId, string hAppInstallPath, string roleName, CapGrantAccessType capGrantAccessType = CapGrantAccessType.Unrestricted, GrantedFunctionsType grantedFunctionsType = GrantedFunctionsType.All, List<(string, string)> grantedFunctions = null, bool uninstallhAppIfAlreadyInstalled = true, bool log = true, Action<string, LogType> loggingFunction = null);
        InstallEnableSignAttachAndConnectToHappEventArgs InstallEnableSignAttachAndConnectToHapp(string hAppId, string hAppInstallPath, string roleName, CapGrantAccessType capGrantAccessType = CapGrantAccessType.Unrestricted, GrantedFunctionsType grantedFunctionsType = GrantedFunctionsType.All, List<(string, string)> grantedFunctions = null, bool uninstallhAppIfAlreadyInstalled = true, bool log = true, Action<string, LogType> loggingFunction = null);
        Task<InstallEnableSignAttachAndConnectToHappEventArgs> InstallEnableSignAttachAndConnectToHappAsync(string hAppId, string hAppInstallPath, string roleName, CapGrantAccessType capGrantAccessType = CapGrantAccessType.Unrestricted, GrantedFunctionsType grantedFunctionsType = GrantedFunctionsType.All, List<(string, string)> grantedFunctions = null, bool uninstallhAppIfAlreadyInstalled = true, bool log = true, Action<string, LogType> loggingFunction = null);
        AppsListedCallBackEventArgs ListApps(AppStatusFilter appStatusFilter, string id = null);
        Task<AppsListedCallBackEventArgs> ListAppsAsync(AppStatusFilter appStatusFilter, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        CellIdsListedCallBackEventArgs ListCellIds(string id = null);
        Task<CellIdsListedCallBackEventArgs> ListCellIdsAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        DnasListedCallBackEventArgs ListDnas(string id = null);
        Task<DnasListedCallBackEventArgs> ListDnasAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        AppInterfacesListedCallBackEventArgs ListInterfaces(string id = null);
        Task<AppInterfacesListedCallBackEventArgs> ListInterfacesAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        DnaRegisteredCallBackEventArgs RegisterDna(byte[] hash, string network_seed = null, object properties = null, string id = null);
        DnaRegisteredCallBackEventArgs RegisterDna(DnaBundle bundle, string network_seed = null, object properties = null, string id = null);
        DnaRegisteredCallBackEventArgs RegisterDna(string path, string network_seed = null, object properties = null, string id = null);
        Task<DnaRegisteredCallBackEventArgs> RegisterDnaAsync(byte[] hash, string network_seed = null, object properties = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        Task<DnaRegisteredCallBackEventArgs> RegisterDnaAsync(DnaBundle bundle, string network_seed = null, object properties = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        Task<DnaRegisteredCallBackEventArgs> RegisterDnaAsync(string path, string network_seed = null, object properties = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        AppUninstalledCallBackEventArgs UninstallApp(string installedAppId, string id = null);
        Task<AppUninstalledCallBackEventArgs> UninstallAppAsync(string installedAppId, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        CoordinatorsUpdatedCallBackEventArgs UpdateCoordinators(byte[] dnaHash, CoordinatorBundle bundle, string id = null);
        CoordinatorsUpdatedCallBackEventArgs UpdateCoordinators(byte[] dnaHash, string path, string id = null);
        CoordinatorsUpdatedCallBackEventArgs UpdateCoordinators(string dnaHash, CoordinatorBundle bundle, string id = null);
        CoordinatorsUpdatedCallBackEventArgs UpdateCoordinators(string dnaHash, string path, string id = null);
        Task<CoordinatorsUpdatedCallBackEventArgs> UpdateCoordinatorsAsync(byte[] dnaHash, CoordinatorBundle bundle, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        Task<CoordinatorsUpdatedCallBackEventArgs> UpdateCoordinatorsAsync(byte[] dnaHash, string path, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        Task<CoordinatorsUpdatedCallBackEventArgs> UpdateCoordinatorsAsync(string dnaHash, CoordinatorBundle bundle, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
        Task<CoordinatorsUpdatedCallBackEventArgs> UpdateCoordinatorsAsync(string dnaHash, string path, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null);
    }
}