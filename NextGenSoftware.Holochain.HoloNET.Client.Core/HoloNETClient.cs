﻿using System;
using System.Net.WebSockets;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using MessagePack;
using Chaos.NaCl;
using Blake2Fast;
using NextGenSoftware.WebSocket;
using NextGenSoftware.Logging;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.Holochain.HoloNET.Client.Properties;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.AppManifest;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    public class HoloNETClient : IHoloNETClient
    //: IDisposable //, IAsyncDisposable
    {
        private bool _shuttingDownHoloNET = false;
        private bool _getAgentPubKeyAndDnaHashFromConductor;
        private bool _automaticallyAttemptToGetFromSandboxIfConductorFails;
        private bool _updateDnaHashAndAgentPubKey = true;
        private List<string> _pendingRequests = new List<string>();
        private Dictionary<string, string> _zomeLookup = new Dictionary<string, string>();
        private Dictionary<string, string> _funcLookup = new Dictionary<string, string>();
        private Dictionary<string, Type> _entryDataObjectTypeLookup = new Dictionary<string, Type>();
        private Dictionary<string, dynamic> _entryDataObjectLookup = new Dictionary<string, dynamic>();
        private Dictionary<string, ZomeFunctionCallBack> _callbackLookup = new Dictionary<string, ZomeFunctionCallBack>();
        private Dictionary<string, ZomeFunctionCallBackEventArgs> _zomeReturnDataLookup = new Dictionary<string, ZomeFunctionCallBackEventArgs>();
        private Dictionary<string, bool> _cacheZomeReturnDataLookup = new Dictionary<string, bool>();
        private TaskCompletionSource<AgentPubKeyDnaHash> _taskCompletionAgentPubKeyAndDnaHashRetrieved = new TaskCompletionSource<AgentPubKeyDnaHash>();
        private Dictionary<string, TaskCompletionSource<ZomeFunctionCallBackEventArgs>> _taskCompletionZomeCallBack = new Dictionary<string, TaskCompletionSource<ZomeFunctionCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AdminAgentPubKeyGeneratedCallBackEventArgs>> _taskCompletionAdminAgentPubKeyGeneratedCallBack = new Dictionary<string, TaskCompletionSource<AdminAgentPubKeyGeneratedCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AdminAppInstalledCallBackEventArgs>> _taskCompletionAdminAppInstalledCallBack = new Dictionary<string, TaskCompletionSource<AdminAppInstalledCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AdminAppEnabledCallBackEventArgs>> _taskCompletionAdminAppEnabledCallBack = new Dictionary<string, TaskCompletionSource<AdminAppEnabledCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AdminAppDisabledCallBackEventArgs>> _taskCompletionAdminAppDisabledCallBack = new Dictionary<string, TaskCompletionSource<AdminAppDisabledCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AdminSigningCredentialsAuthorizedEventArgs>> _taskCompletionAdminSigningCredentialsAuthorizedCallBack = new Dictionary<string, TaskCompletionSource<AdminSigningCredentialsAuthorizedEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AdminAppInterfaceAttachedCallBackEventArgs>> _taskCompletionAdminAppInterfaceAttachedCallBack = new Dictionary<string, TaskCompletionSource<AdminAppInterfaceAttachedCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AdminRegisterDnaCallBackEventArgs>> _taskCompletionAdminRegisterDnaCallBack = new Dictionary<string, TaskCompletionSource<AdminRegisterDnaCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AdminListAppsCallBackEventArgs>> _taskCompletionAdminListAppsCallBack = new Dictionary<string, TaskCompletionSource<AdminListAppsCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AdminListDnasCallBackEventArgs>> _taskCompletionAdminListDnasCallBack = new Dictionary<string, TaskCompletionSource<AdminListDnasCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AdminListCellIdsCallBackEventArgs>> _taskCompletionAdminListCellIdsCallBack = new Dictionary<string, TaskCompletionSource<AdminListCellIdsCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AdminListAppInterfacesCallBackEventArgs>> _taskCompletionAdminListAppInterfacesCallBack = new Dictionary<string, TaskCompletionSource<AdminListAppInterfacesCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AdminDumpFullStateCallBackEventArgs>> _taskCompletionAdminDumpFullStateCallBack = new Dictionary<string, TaskCompletionSource<AdminDumpFullStateCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AdminDumpStateCallBackEventArgs>> _taskCompletionAdminDumpStateCallBack = new Dictionary<string, TaskCompletionSource<AdminDumpStateCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AdminGetDnaDefinitionCallBackEventArgs>> _taskCompletionAdminGetDnaDefinitionCallBack = new Dictionary<string, TaskCompletionSource<AdminGetDnaDefinitionCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AdminUpdateCoordinatorsCallBackEventArgs>> _taskCompletionAdminUpdateCoordinatorsCallBack = new Dictionary<string, TaskCompletionSource<AdminUpdateCoordinatorsCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AdminGetAgentInfoCallBackEventArgs>> _taskCompletionAdminGetAgentInfoCallBack = new Dictionary<string, TaskCompletionSource<AdminGetAgentInfoCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AdminAddAgentInfoCallBackEventArgs>> _taskCompletionAdminAddAgentInfoCallBack = new Dictionary<string, TaskCompletionSource<AdminAddAgentInfoCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AdminDeleteCloneCellCallBackEventArgs>> _taskCompletionAdminDeleteCloneCellCallBack = new Dictionary<string, TaskCompletionSource<AdminDeleteCloneCellCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AdminGetStorageInfoCallBackEventArgs>> _taskCompletionAdminGetStorageInfoCallBack = new Dictionary<string, TaskCompletionSource<AdminGetStorageInfoCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AdminDumpNetworkStatsCallBackEventArgs>> _taskCompletionAdminDumpNetworkStatsCallBack = new Dictionary<string, TaskCompletionSource<AdminDumpNetworkStatsCallBackEventArgs>>();
        //private Dictionary<string, TaskCompletionSource<ReadyForZomeCallsEventArgs>> _taskCompletionReadyForZomeCalls = new Dictionary<string, TaskCompletionSource<ReadyForZomeCallsEventArgs>>();
        //private Dictionary<string, TaskCompletionSource<DisconnectedEventArgs>> _taskCompletionDisconnected = new Dictionary<string, TaskCompletionSource<DisconnectedEventArgs>>();

        private Dictionary<string, PropertyInfo[]> _dictPropertyInfos = new Dictionary<string, PropertyInfo[]>();
        private Dictionary<byte[][], SigningCredentials> _signingCredentialsForCell = new Dictionary<byte[][], SigningCredentials>();
        private TaskCompletionSource<ReadyForZomeCallsEventArgs> _taskCompletionReadyForZomeCalls = new TaskCompletionSource<ReadyForZomeCallsEventArgs>();
        private TaskCompletionSource<DisconnectedEventArgs> _taskCompletionDisconnected = new TaskCompletionSource<DisconnectedEventArgs>();
        private TaskCompletionSource<HoloNETShutdownEventArgs> _taskCompletionHoloNETShutdown = new TaskCompletionSource<HoloNETShutdownEventArgs>();
        private int _currentId = 0;
        private HoloNETConfig _config = null;
        private Process _conductorProcess = null;
        private ShutdownHolochainConductorsMode _shutdownHolochainConductorsMode = ShutdownHolochainConductorsMode.UseConfigSettings;
        private MessagePackSerializerOptions messagePackSerializerOptions = MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);

        //Events
        public delegate void Connected(object sender, ConnectedEventArgs e);

        /// <summary>
        /// Fired when the client has successfully connected to the Holochain conductor.
        /// </summary>
        public event Connected OnConnected;

        public delegate void Disconnected(object sender, DisconnectedEventArgs e);

        /// <summary>
        /// Fired when the client disconnected from the Holochain conductor.
        /// </summary>
        public event Disconnected OnDisconnected;

        public delegate void HolochainConductorsShutdownComplete(object sender, HolochainConductorsShutdownEventArgs e);

        /// <summary>
        /// Fired when all Holochain Conductors have been shutdown.
        /// </summary>
        public event HolochainConductorsShutdownComplete OnHolochainConductorsShutdownComplete;

        public delegate void HoloNETShutdownComplete(object sender, HoloNETShutdownEventArgs e);

        /// <summary>
        /// Fired when HoloNET has completed shutting down (this includes closing all connections and shutting down all Holochain Conductor).
        /// </summary>
        public event HoloNETShutdownComplete OnHoloNETShutdownComplete;


        public delegate void DataReceived(object sender, HoloNETDataReceivedEventArgs e);

        /// <summary>
        /// Fired when any data is received from the Holochain conductor. This returns the raw data.                                     
        /// </summary>
        public event DataReceived OnDataReceived;


        public delegate void DataSent(object sender, HoloNETDataSentEventArgs e);

        /// <summary>
        /// Fired when any data is sent to the Holochain conductor.
        /// </summary>
        public event DataSent OnDataSent;


        public delegate void ZomeFunctionCallBack(object sender, ZomeFunctionCallBackEventArgs e);

        /// <summary>
        /// Fired when the Holochain conductor returns the response from a zome function call. This returns the raw data as well as the parsed data returned from the zome function. It also returns the id, zome and zome function that made the call.
        /// </summary>
        public event ZomeFunctionCallBack OnZomeFunctionCallBack;

        public delegate void SignalCallBack(object sender, SignalCallBackEventArgs e);

        /// <summary>
        /// Fired when the Holochain conductor sends signals data. 
        /// </summary>
        public event SignalCallBack OnSignalCallBack;

        //public delegate void ConductorDebugCallBack(object sender, ConductorDebugCallBackEventArgs e);
        //public event ConductorDebugCallBack OnConductorDebugCallBack;

        public delegate void AppInfoCallBack(object sender, AppInfoCallBackEventArgs e);

        /// <summary>
        /// Fired when the client receives AppInfo from the conductor containing the cell id for the running hApp (which in itself contains the AgentPubKey & DnaHash). It also contains the AppId and other info.
        /// </summary>
        public event AppInfoCallBack OnAppInfoCallBack;


        public delegate void AdminAgentPubKeyGeneratedCallBack(object sender, AdminAgentPubKeyGeneratedCallBackEventArgs e);

        /// <summary>
        /// Fired when the client receives the generated AgentPubKey from the conductor.
        /// </summary>
        public event AdminAgentPubKeyGeneratedCallBack OnAdminAgentPubKeyGeneratedCallBack;


        public delegate void AdminAppInstalledCallBack(object sender, AdminAppInstalledCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after a hApp has been installed via the AdminInstallAppAsyc/AdminInstallApp method.
        /// </summary>
        public event AdminAppInstalledCallBack OnAdminAppInstalledCallBack;


        public delegate void AdminAppEnabledCallBack(object sender, AdminAppEnabledCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after a hApp has been enabled via the AdminEnableAppAsyc/AdminEnableApp method.
        /// </summary>
        public event AdminAppEnabledCallBack OnAdminAppEnabledCallBack;


        public delegate void AdminAppDisabledCallBack(object sender, AdminAppDisabledCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after a hApp has been disabled via the AdminDisableAppAsyc/AdminDisableApp method.
        /// </summary>
        public event AdminAppDisabledCallBack OnAdminAppDisabledCallBack;


        public delegate void AdminSigningCredentialsAuthorized(object sender, AdminSigningCredentialsAuthorizedEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after a cell has been authorized via the AdminAuthorizeSigningCredentialsAsync/AdminAuthorizeSigningCredentials method.
        /// </summary>
        public event AdminSigningCredentialsAuthorized OnAdminSigningCredentialsAuthorized;


        public delegate void AdminAppInterfaceAttachedCallBack(object sender, AdminAppInterfaceAttachedCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after the app interface has been attached via the AdminAttachAppInterfaceAsyc/AdminAttachAppInterface method.
        /// </summary>
        public event AdminAppInterfaceAttachedCallBack OnAdminAppInterfaceAttachedCallBack;


        public delegate void AdminRegisterDnaCallBack(object sender, AdminRegisterDnaCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after the dna has been registered via the AdminRegisterDnaAsync/AdminRegisterDna method.
        /// </summary>
        public event AdminRegisterDnaCallBack OnAdminRegisterDnaCallBack;


        public delegate void AdminListAppsCallBack(object sender, AdminListAppsCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after the AdminListAppsAsync/AdminListApps method is called.
        /// </summary>
        public event AdminListAppsCallBack OnAdminListAppsCallBack;


        public delegate void AdminListDnasCallBack(object sender, AdminListDnasCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after the AdminListDnasAsync/AdminListDnas method is called.
        /// </summary>
        public event AdminListDnasCallBack OnAdminListDnasCallBack;


        public delegate void AdminListCellIdsCallBack(object sender, AdminListCellIdsCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after the AdminListCellIdsAsync/AdminListCellIds method is called.
        /// </summary>
        public event AdminListCellIdsCallBack OnAdminListCellIdsCallBack;


        public delegate void AdminListAppInterfacesCallBack(object sender, AdminListAppInterfacesCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after the AdminListAppInterfacesAsync/AdminListAppInterfaces method is called.
        /// </summary>
        public event AdminListAppInterfacesCallBack OnAdminListAppInterfacesCallBack;


        public delegate void AdminDumpFullStateCallBack(object sender, AdminDumpFullStateCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after the AdminDumpFullStateAsync/AdminDumpFullState method is called.
        /// </summary>
        public event AdminDumpFullStateCallBack OnAdminDumpFullStateCallBack;


        public delegate void AdminDumpStateCallBack(object sender, AdminDumpStateCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after the AdminDumpStateAsync/AdminDumpState method is called.
        /// </summary>
        public event AdminDumpStateCallBack OnAdminDumpStateCallBack;


        public delegate void AdminGetDnaDefinitionCallBack(object sender, AdminGetDnaDefinitionCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor containing the DNA Definition for a DNA Hash after the AdminGetDnaDefinitionAsync/AdminGetDnaDefinition method is called.
        /// </summary>
        public event AdminGetDnaDefinitionCallBack OnAdminGetDnaDefinitionCallBack;


        public delegate void AdminUpdateCoordinatorsCallBack(object sender, AdminUpdateCoordinatorsCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after the AdminUpdateCoordinatorsAsync/AdminUpdateCoordinators method is called.
        /// </summary>
        public event AdminUpdateCoordinatorsCallBack OnAdminUpdateCoordinatorsCallBack;


        public delegate void AdminGetAgentInfoCallBack(object sender, AdminGetAgentInfoCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor containing the requested Agent Info after the AdminGetAgentInfoAsync/GetAgentInfo method is called.
        /// </summary>
        public event AdminGetAgentInfoCallBack OnAdminGetAgentInfoCallBack;


        public delegate void AdminAddAgentInfoCallBack(object sender, AdminAddAgentInfoCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor containing the requested Agent Info after the AdminGetAgentInfoAsync/GetAgentInfo method is called.
        /// </summary>
        public event AdminAddAgentInfoCallBack OnAdminAddAgentInfoCallBack;


        public delegate void AdminDeleteCloneCellCallBack(object sender, AdminDeleteCloneCellCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor containing the requested Agent Info after the AdminGetAgentInfoAsync/GetAgentInfo method is called.
        /// </summary>
        public event AdminDeleteCloneCellCallBack OnAdminDeleteCloneCellCallBack;


        public delegate void AdminGetStorageInfoCallBack(object sender, AdminGetStorageInfoCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor containing the requested Agent Info after the AdminGetAgentInfoAsync/GetAgentInfo method is called.
        /// </summary>
        public event AdminGetStorageInfoCallBack OnAdminGetStorageInfoCallBack;


        public delegate void AdminDumpNetworkStatsCallBack(object sender, AdminDumpNetworkStatsCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor containing the requested Agent Info after the AdminGetAgentInfoAsync/GetAgentInfo method is called.
        /// </summary>
        public event AdminDumpNetworkStatsCallBack OnAdminDumpNetworkStatsCallBack;



        public delegate void ReadyForZomeCalls(object sender, ReadyForZomeCallsEventArgs e);

        /// <summary>
        /// Fired when the client has successfully connected and reteived the AgentPubKey & DnaHash, meaning it is ready to make zome calls to the Holochain conductor.
        /// </summary>
        public event ReadyForZomeCalls OnReadyForZomeCalls;

        public delegate void Error(object sender, HoloNETErrorEventArgs e);

        /// <summary>
        /// Fired when an error occurs, check the params for the cause of the error.
        /// </summary>
        public event Error OnError;

        // Properties

        /// <summary>
        /// This property contains the internal NextGenSoftware.WebSocket (https://www.nuget.org/packages/NextGenSoftware.WebSocket) that HoloNET uses. You can use this property to access the current state of the WebSocket as well as configure more options.
        /// </summary>
        public WebSocket.WebSocket WebSocket { get; set; }

        /// <summary>
        /// This property contains a struct called HoloNETConfig containing the sub-properties: AgentPubKey, DnaHash, FullPathToRootHappFolder, FullPathToCompiledHappFolder, HolochainConductorMode, FullPathToExternalHolochainConductorBinary, FullPathToExternalHCToolBinary, SecondsToWaitForHolochainConductorToStart, AutoStartHolochainConductor, ShowHolochainConductorWindow, AutoShutdownHolochainConductor, ShutDownALLHolochainConductors, HolochainConductorToUse, OnlyAllowOneHolochainConductorToRunAtATime, LoggingMode & ErrorHandlingBehaviour.
        /// </summary>
        public HoloNETConfig Config
        {
            get
            {
                if (_config == null)
                    _config = new HoloNETConfig();

                return _config;
            }
            set
            {
                _config = value;
            }
        }

        /// <summary>
        /// This property is a shortcut to the State property on the WebSocket property.
        /// </summary>
        public WebSocketState State
        {
            get
            {
                return WebSocket.State;
            }
        }

        /// <summary>
        /// This property is a shortcut to the EndPoint property on the WebSocket property.
        /// </summary>
        public string EndPoint
        {
            get
            {
                if (WebSocket != null)
                    return WebSocket.EndPoint;

                return "";
            }
        }

        /// <summary>
        /// This property is a boolean and will return true when HoloNET is ready for zome calls after the OnReadyForZomeCalls event is raised.
        /// </summary>
        public bool IsReadyForZomesCalls { get; set; }

        /// <summary>
        /// This property is a boolean and will return true when HoloNET is retrieving the AgentPubKey & DnaHash using one of the RetrieveAgentPubKeyAndDnaHash, RetrieveAgentPubKeyAndDnaHashFromSandbox or RetrieveAgentPubKeyAndDnaHashFromConductor methods (by default this will occur automatically after it has connected to the Holochain Conductor).
        /// </summary>
        public bool RetrievingAgentPubKeyAndDnaHash { get; private set; }

        public bool IsAdmin { get; private set; }

        /// <summary>
        /// This constructor uses the built-in DefaultLogger and allows various options to be configured for it.
        /// </summary>
        /// <param name="holochainConductorURI">The URI of the Holochain Conductor to connect to. Will default to 'ws://localhost:8888'.</param>
        /// <param name="logToConsole">Set this to true (default) if you wish HoloNET to log to the console. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="logToFile">Set this to true (default) if you wish HoloNET to log a log file. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="releativePathToLogFolder">The relative path to the log folder to log to. Will default to a sub-directory called `Logs` within the current working directory. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="logFileName">The name of the file to log to. Will default to `HoloNET.log`. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="addAdditionalSpaceAfterEachLogEntry">Set this to true to add additional space after each log entry. The default is false. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="showColouredLogs">Set this to true to enable coloured logs in the console. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="debugColour">The colour to use for `Debug` log entries to the console NOTE: This is only relevant if the built-in DefaultLogger is used..</param>
        /// <param name="infoColour">The colour to use for `Info` log entries to the console. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="warningColour">The colour to use for `Warning` log entries to the console. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="errorColour">The colour to use for `Error` log entries to the console. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        public HoloNETClient(string holochainConductorURI = "ws://localhost:8888", bool logToConsole = true, bool logToFile = true, string releativePathToLogFolder = "Logs", string logFileName = "HoloNET.log", bool addAdditionalSpaceAfterEachLogEntry = false, bool showColouredLogs = true, ConsoleColor debugColour = ConsoleColor.White, ConsoleColor infoColour = ConsoleColor.Green, ConsoleColor warningColour = ConsoleColor.Yellow, ConsoleColor errorColour = ConsoleColor.Red)
        {
            Logger.Loggers.Add(new DefaultLogger(logToConsole, logToFile, releativePathToLogFolder, logFileName, addAdditionalSpaceAfterEachLogEntry, showColouredLogs, debugColour, infoColour, warningColour, errorColour));
            Init(holochainConductorURI);
        }

        /// <summary>
        /// This constructor allows you to inject in (DI) your own implementation (logger) of the ILogger interface.
        /// </summary>
        /// <param name="logger">The implementation of the ILogger interface (custom logger).</param>
        /// <param name="alsoUseDefaultLogger">Set this to true if you wish HoloNET to also log to the DefaultLogger as well as any custom logger injected in.</param>
        /// <param name="holochainConductorURI">The URI of the Holochain Conductor to connect to. Will default to 'ws://localhost:8888'.</param>
        public HoloNETClient(ILogger logger, bool alsoUseDefaultLogger = false, string holochainConductorURI = "ws://localhost:8888")
        {
            Logger.Loggers.Add(logger);

            if (alsoUseDefaultLogger)
                Logger.Loggers.Add(new DefaultLogger());

            Init(holochainConductorURI);
        }

        /// <summary>
        /// This constructor allows you to inject in (DI) your own implementation (logger) of the ILogger interface.
        /// </summary>
        /// <param name="logger">The implementation of the ILogger interface (custom logger).</param>
        /// <param name="alsoUseDefaultLogger">Set this to true if you wish HoloNET to also log to the DefaultLogger as well as any custom logger injected in.</param>
        /// <param name="holochainConductorURI">The URI of the Holochain Conductor to connect to. Will default to 'ws://localhost:8888'.</param>
        /// <param name="logToConsole">Set this to true (default) if you wish HoloNET to log to the console. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="logToFile">Set this to true (default) if you wish HoloNET to log a log file. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="releativePathToLogFolder">The relative path to the log folder to log to. Will default to a sub-directory called `Logs` within the current working directory. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="logFileName">The name of the file to log to. Will default to `HoloNET.log`. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="addAdditionalSpaceAfterEachLogEntry">Set this to true to add additional space after each log entry. The default is false. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="showColouredLogs">Set this to true to enable coloured logs in the console. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="debugColour">The colour to use for `Debug` log entries to the console NOTE: This is only relevant if the built-in DefaultLogger is used..</param>
        /// <param name="infoColour">The colour to use for `Info` log entries to the console. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="warningColour">The colour to use for `Warning` log entries to the console. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="errorColour">The colour to use for `Error` log entries to the console. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        public HoloNETClient(ILogger logger, bool alsoUseDefaultLogger = false, string holochainConductorURI = "ws://localhost:8888", bool logToConsole = true, bool logToFile = true, string releativePathToLogFolder = "Logs", string logFileName = "HoloNET.log", bool addAdditionalSpaceAfterEachLogEntry = false, bool showColouredLogs = true, ConsoleColor debugColour = ConsoleColor.White, ConsoleColor infoColour = ConsoleColor.Green, ConsoleColor warningColour = ConsoleColor.Yellow, ConsoleColor errorColour = ConsoleColor.Red)
        {
            Logger.Loggers.Add(logger);

            if (alsoUseDefaultLogger)
                Logger.Loggers.Add(new DefaultLogger(logToConsole, logToFile, releativePathToLogFolder, logFileName, addAdditionalSpaceAfterEachLogEntry, showColouredLogs, debugColour, infoColour, warningColour, errorColour));

            Init(holochainConductorURI);
        }

        /// <summary>
        /// This constructor allows you to inject in (DI) multiple implementations (logger) of the ILogger interface. HoloNET will then log to each of these loggers.
        /// </summary>
        /// <param name="loggers">The implementations of the ILogger interface (custom loggers).</param>
        /// <param name="alsoUseDefaultLogger">Set this to true if you wish HoloNET to also log to the DefaultLogger as well as any custom loggers injected in.</param>
        /// <param name="holochainConductorURI">The URI of the Holochain Conductor to connect to. Will default to 'ws://localhost:8888'.</param>
        public HoloNETClient(IEnumerable<ILogger> loggers, bool alsoUseDefaultLogger = false, string holochainConductorURI = "ws://localhost:8888")
        {
            Logger.Loggers = new List<ILogger>(loggers);

            if (alsoUseDefaultLogger)
                Logger.Loggers.Add(new DefaultLogger());

            Init(holochainConductorURI);
        }

        /// <summary>
        /// This constructor allows you to inject in (DI) multiple implementations (logger) of the ILogger interface. HoloNET will then log to each of these loggers.
        /// </summary>
        /// <param name="loggers">The implementations of the ILogger interface (custom loggers).</param>
        /// <param name="alsoUseDefaultLogger">Set this to true if you wish HoloNET to also log to the DefaultLogger as well as any custom loggers injected in.</param>
        /// <param name="holochainConductorURI">The URI of the Holochain Conductor to connect to. Will default to 'ws://localhost:8888'.</param>
        /// <param name="logToConsole">Set this to true (default) if you wish HoloNET to log to the console. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="logToFile">Set this to true (default) if you wish HoloNET to log a log file. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="releativePathToLogFolder">The relative path to the log folder to log to. Will default to a sub-directory called `Logs` within the current working directory. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="logFileName">The name of the file to log to. Will default to `HoloNET.log`. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="addAdditionalSpaceAfterEachLogEntry">Set this to true to add additional space after each log entry. The default is false. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="showColouredLogs">Set this to true to enable coloured logs in the console. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="debugColour">The colour to use for `Debug` log entries to the console NOTE: This is only relevant if the built-in DefaultLogger is used..</param>
        /// <param name="infoColour">The colour to use for `Info` log entries to the console. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="warningColour">The colour to use for `Warning` log entries to the console. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        /// <param name="errorColour">The colour to use for `Error` log entries to the console. NOTE: This is only relevant if the built-in DefaultLogger is used.</param>
        public HoloNETClient(IEnumerable<ILogger> loggers, bool alsoUseDefaultLogger = false, string holochainConductorURI = "ws://localhost:8888", bool logToConsole = true, bool logToFile = true, string releativePathToLogFolder = "Logs", string logFileName = "HoloNET.log", bool addAdditionalSpaceAfterEachLogEntry = false, bool showColouredLogs = true, ConsoleColor debugColour = ConsoleColor.White, ConsoleColor infoColour = ConsoleColor.Green, ConsoleColor warningColour = ConsoleColor.Yellow, ConsoleColor errorColour = ConsoleColor.Red)
        {
            Logger.Loggers = new List<ILogger>(loggers);

            if (alsoUseDefaultLogger)
                Logger.Loggers.Add(new DefaultLogger(logToConsole, logToFile, releativePathToLogFolder, logFileName, addAdditionalSpaceAfterEachLogEntry, showColouredLogs, debugColour, infoColour, warningColour, errorColour));

            Init(holochainConductorURI);
        }

        /// <summary>
        /// This method will wait (non blocking) until HoloNET is ready to make zome calls after it has connected to the Holochain Conductor and retrived the AgentPubKey & DnaHash. It will then return to the caller with the AgentPubKey & DnaHash. This method will return the same time the OnReadyForZomeCalls event is raised. Unlike all the other methods, this one only contains an async version because the non async version would block all other threads including any UI ones etc.
        /// </summary>
        /// <returns></returns>
        public async Task<ReadyForZomeCallsEventArgs> WaitTillReadyForZomeCallsAsync()
        {
            return await _taskCompletionReadyForZomeCalls.Task;
        }

        /// <summary>
        /// This method simply connects to the Holochain conductor. It raises the OnConnected event once it is has successfully established a connection. It then calls the RetrieveAgentPubKeyAndDnaHash method to retrieve the AgentPubKey & DnaHash. If the `connectedCallBackMode` flag is set to `WaitForHolochainConductorToConnect` (default) it will await until it is connected before returning, otherwise it will return immediately and then call the OnConnected event once it has finished connecting.
        /// </summary>
        /// <param name="connectedCallBackMode">If set to `WaitForHolochainConductorToConnect` (default) it will await until it is connected before returning, otherwise it will return immediately and then call the OnConnected event once it has finished connecting.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashMode">If set to `Wait` (default) it will await until it has finished retrieving the AgentPubKey & DnaHash before returning, otherwise it will return immediately and then call the OnReadyForZomeCalls event once it has finished retrieving the DnaHash & AgentPubKey.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromConductor">Set this to true for HoloNET to automatically retrieve the AgentPubKey & DnaHash from the Holochain Conductor after it has connected. This defaults to true.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromSandbox">Set this to true if you wish HoloNET to automatically retrieve the AgentPubKey & DnaHash from the hc sandbox after it has connected. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromConductorIfSandBoxFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the Holochain Conductor if it fails to get them from the HC Sandbox command. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromSandBoxIfConductorFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <param name="updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the [HoloNETConfig](#holonetconfig) once it has retrieved the DnaHash & AgentPubKey.</param>
        /// <returns></returns>
        public async Task ConnectAsync(ConnectedCallBackMode connectedCallBackMode = ConnectedCallBackMode.WaitForHolochainConductorToConnect, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved = true)
        {
            try
            {
                _getAgentPubKeyAndDnaHashFromConductor = retrieveAgentPubKeyAndDnaHashFromConductor;

                if (Logger.Loggers.Count == 0)
                    throw new HoloNETException("ERROR: No Logger Has Been Specified! Please set a Logger with the Logger.Loggers Property.");

                if (WebSocket.State != WebSocketState.Connecting && WebSocket.State != WebSocketState.Open && WebSocket.State != WebSocketState.Aborted)
                {
                    if (Config.AutoStartHolochainConductor)
                        await StartHolochainConductorAsync();

                    if (connectedCallBackMode == ConnectedCallBackMode.WaitForHolochainConductorToConnect)
                        await WebSocket.ConnectAsync();
                    else
                    {
                        WebSocket.ConnectAsync();

                        //if (retrieveAgentPubKeyAndDnaHashMode != RetrieveAgentPubKeyAndDnaHashMode.DoNotRetreive)
                        //    await RetrieveAgentPubKeyAndDnaHashAsync(retrieveAgentPubKeyAndDnaHashMode, retrieveAgentPubKeyAndDnaHashFromConductor, retrieveAgentPubKeyAndDnaHashFromSandbox, automaticallyAttemptToRetrieveFromConductorIfSandBoxFails, automaticallyAttemptToRetrieveFromSandBoxIfConductorFails, updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved);
                    }
                }
            }
            catch (Exception e)
            {
                HandleError(string.Concat("Error occurred in HoloNETClient.Connect method connecting to ", WebSocket.EndPoint), e);
            }
        }

        /// <summary>
        /// This method simply connects to the Holochain conductor. It raises the OnConnected event once it is has successfully established a connection. It then calls the RetrieveAgentPubKeyAndDnaHash method to retrieve the AgentPubKey & DnaHash.
        /// </summary>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromConductor">Set this to true for HoloNET to automatically retrieve the AgentPubKey & DnaHash from the Holochain Conductor after it has connected. This defaults to true.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromSandbox">Set this to true if you wish HoloNET to automatically retrieve the AgentPubKey & DnaHash from the hc sandbox after it has connected. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromConductorIfSandBoxFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the Holochain Conductor if it fails to get them from the HC Sandbox command. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromSandBoxIfConductorFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <param name="updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the [HoloNETConfig](#holonetconfig) once it has retrieved the DnaHash & AgentPubKey.</param>
        public void Connect(bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = false, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved = true)
        {
            ConnectAsync(ConnectedCallBackMode.UseCallBackEvents, RetrieveAgentPubKeyAndDnaHashMode.UseCallBackEvents, retrieveAgentPubKeyAndDnaHashFromConductor, retrieveAgentPubKeyAndDnaHashFromSandbox, automaticallyAttemptToRetrieveFromConductorIfSandBoxFails, automaticallyAttemptToRetrieveFromSandBoxIfConductorFails, updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved);
        }

        /// <summary>
        /// This method simply connects to the Holochain conductor. It raises the OnConnected event once it is has successfully established a connection. If the `connectedCallBackMode` flag is set to `WaitForHolochainConductorToConnect` (default) it will await until it is connected before returning, otherwise it will return immediately and then call the OnConnected event once it has finished connecting.
        /// </summary>
        /// <param name="connectedCallBackMode">If set to `WaitForHolochainConductorToConnect` (default) it will await until it is connected before returning, otherwise it will return immediately and then call the OnConnected event once it has finished connecting.</param>
        /// <returns></returns>
        public async Task ConnectAdminAsync(ConnectedCallBackMode connectedCallBackMode = ConnectedCallBackMode.WaitForHolochainConductorToConnect)
        {
            IsAdmin = true;
            await ConnectAsync(connectedCallBackMode, RetrieveAgentPubKeyAndDnaHashMode.DoNotRetreive, false, false, false, false);
        }

        /// <summary>
        /// This method simply connects to the Holochain conductor. It raises the OnConnected event once it is has successfully established a connection. If the `connectedCallBackMode` flag is set to `WaitForHolochainConductorToConnect` (default) it will await until it is connected before returning, otherwise it will return immediately and then call the OnConnected event once it has finished connecting.
        /// </summary>
        /// <param name="connectedCallBackMode">If set to `WaitForHolochainConductorToConnect` (default) it will await until it is connected before returning, otherwise it will return immediately and then call the OnConnected event once it has finished connecting.</param>
        /// <returns></returns>
        public void ConnectAdmin(bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = false, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved = true)
        {
            ConnectAdminAsync(ConnectedCallBackMode.UseCallBackEvents);
        }

        /// <summary>
        /// This method will start the Holochain Conducutor using the appropriate settings defined in the Config property.
        /// </summary>
        /// <returns></returns>
        public async Task StartHolochainConductorAsync()
        {
            try
            {
                // Was used when they were set to Content rather than Embedded.
                //string fullPathToEmbeddedHolochainConductorBinary = string.Concat(Directory.GetCurrentDirectory(), "\\HolochainBinaries\\holochain.exe");
                //string fullPathToEmbeddedHCToolBinary = string.Concat(Directory.GetCurrentDirectory(), "\\HolochainBinaries\\hc.exe");

                _conductorProcess = new Process();

                if (string.IsNullOrEmpty(Config.FullPathToExternalHolochainConductorBinary) && Config.HolochainConductorMode == HolochainConductorModeEnum.UseExternal && Config.HolochainConductorToUse == HolochainConductorEnum.HolochainProductionConductor)
                    throw new ArgumentNullException("FullPathToExternalHolochainConductorBinary", "When HolochainConductorMode is set to 'UseExternal' and HolochainConductorToUse is set to 'HolochainProductionConductor', FullPathToExternalHolochainConductorBinary cannot be empty.");

                if (string.IsNullOrEmpty(Config.FullPathToExternalHCToolBinary) && Config.HolochainConductorMode == HolochainConductorModeEnum.UseExternal && Config.HolochainConductorToUse == HolochainConductorEnum.HcDevTool)
                    throw new ArgumentNullException("FullPathToExternalHCToolBinary", "When HolochainConductorMode is set to 'UseExternal' and HolochainConductorToUse is set to 'HcDevTool', FullPathToExternalHCToolBinary cannot be empty.");

                if (!File.Exists(Config.FullPathToExternalHolochainConductorBinary) && Config.HolochainConductorMode == HolochainConductorModeEnum.UseExternal && Config.HolochainConductorToUse == HolochainConductorEnum.HolochainProductionConductor)
                    throw new FileNotFoundException($"When HolochainConductorMode is set to 'UseExternal' and HolochainConductorToUse is set to 'HolochainProductionConductor', FullPathToExternalHolochainConductorBinary ({Config.FullPathToExternalHolochainConductorBinary}) must point to a valid file.");

                if (!File.Exists(Config.FullPathToExternalHCToolBinary) && Config.HolochainConductorMode == HolochainConductorModeEnum.UseExternal && Config.HolochainConductorToUse == HolochainConductorEnum.HcDevTool)
                    throw new FileNotFoundException($"When HolochainConductorMode is set to 'UseExternal' and HolochainConductorToUse is set to 'HcDevTool', FullPathToExternalHCToolBinary ({Config.FullPathToExternalHCToolBinary}) must point to a valid file.");

                //if (!File.Exists(fullPathToEmbeddedHolochainConductorBinary) && Config.HolochainConductorMode == HolochainConductorModeEnum.UseEmbedded && Config.HolochainConductorToUse == HolochainConductorEnum.HolochainProductionConductor)
                //    throw new FileNotFoundException($"When HolochainConductorMode is set to 'UseEmbedded' and HolochainConductorToUse is set to 'HolochainProductionConductor', you must ensure the holochain.exe is found here: {fullPathToEmbeddedHolochainConductorBinary}.");

                //if (!File.Exists(fullPathToEmbeddedHCToolBinary) && Config.HolochainConductorMode == HolochainConductorModeEnum.UseEmbedded && Config.HolochainConductorToUse == HolochainConductorEnum.HcDevTool)
                //    throw new FileNotFoundException($"When HolochainConductorMode is set to 'UseEmbedded' and HolochainConductorToUse is set to 'HcDevTool', you must ensure the hc.exe is found here: {fullPathToEmbeddedHCToolBinary}.");

                if (!Directory.Exists(Config.FullPathToRootHappFolder))
                    throw new DirectoryNotFoundException($"The path for Config.FullPathToRootHappFolder ({Config.FullPathToRootHappFolder}) was not found.");

                if (!Directory.Exists(Config.FullPathToCompiledHappFolder))
                    throw new DirectoryNotFoundException($"The path for Config.FullPathToCompiledHappFolder ({Config.FullPathToCompiledHappFolder}) was not found.");


                if (Config.HolochainConductorToUse == HolochainConductorEnum.HcDevTool)
                {
                    switch (Config.HolochainConductorMode)
                    {
                        case HolochainConductorModeEnum.UseExternal:
                            _conductorProcess.StartInfo.FileName = Config.FullPathToExternalHCToolBinary;
                            break;

                        case HolochainConductorModeEnum.UseEmbedded:
                            {
                                //throw new InvalidOperationException("You must install the Embedded version if you wish to use HolochainConductorMode.UseEmbedded.");

                                //_conductorProcess.StartInfo.FileName = fullPathToEmbeddedHCToolBinary;

                                string hcPath = Path.Combine(Directory.GetCurrentDirectory(), "hc.exe");

                                if (!File.Exists(hcPath))
                                {
                                    using (FileStream fsDst = new FileStream(hcPath, FileMode.CreateNew, FileAccess.Write))
                                    {
                                        byte[] bytes = Resources.hc;
                                        fsDst.Write(bytes, 0, bytes.Length);
                                    }
                                }

                                _conductorProcess.StartInfo.FileName = hcPath;
                            }
                            break;

                        case HolochainConductorModeEnum.UseSystemGlobal:
                            _conductorProcess.StartInfo.FileName = "hc.exe";
                            break;
                    }
                }
                else
                {
                    switch (Config.HolochainConductorMode)
                    {
                        case HolochainConductorModeEnum.UseExternal:
                            _conductorProcess.StartInfo.FileName = Config.FullPathToExternalHolochainConductorBinary;
                            break;

                        case HolochainConductorModeEnum.UseEmbedded:
                            {
                                //throw new InvalidOperationException("You must install the Embedded version if you wish to use HolochainConductorMode.UseEmbedded.");

                                //_conductorProcess.StartInfo.FileName = fullPathToEmbeddedHolochainConductorBinary;

                                string holochainPath = Path.Combine(Directory.GetCurrentDirectory(), "holochain.exe");

                                if (!File.Exists(holochainPath))
                                {
                                    using (FileStream fsDst = new FileStream(holochainPath, FileMode.CreateNew, FileAccess.Write))
                                    {
                                        byte[] bytes = Resources.holochain;
                                        fsDst.Write(bytes, 0, bytes.Length);
                                    }
                                }

                                _conductorProcess.StartInfo.FileName = holochainPath;
                            }
                            break;

                        case HolochainConductorModeEnum.UseSystemGlobal:
                            _conductorProcess.StartInfo.FileName = "holochain.exe";
                            break;
                    }
                }

                //Make sure the condctor is not already running
                if (!Config.OnlyAllowOneHolochainConductorToRunAtATime || (Config.OnlyAllowOneHolochainConductorToRunAtATime && !Process.GetProcesses().Any(x => x.ProcessName == _conductorProcess.StartInfo.FileName)))
                {
                    Logger.Log("Starting Holochain Conductor...", LogType.Info, true);
                    _conductorProcess.StartInfo.WorkingDirectory = Config.FullPathToRootHappFolder;

                    if (Config.HolochainConductorToUse == HolochainConductorEnum.HcDevTool)
                    {
                        //_conductorProcess.StartInfo.Arguments = "sandbox run 0";
                        _conductorProcess.StartInfo.Arguments = $"sandbox generate {Config.FullPathToCompiledHappFolder}";
                    }

                    _conductorProcess.StartInfo.UseShellExecute = true;
                    _conductorProcess.StartInfo.RedirectStandardOutput = false;

                    if (Config.ShowHolochainConductorWindow)
                    {
                        _conductorProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                        _conductorProcess.StartInfo.CreateNoWindow = false;
                    }
                    else
                    {
                        _conductorProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        _conductorProcess.StartInfo.CreateNoWindow = true;
                    }

                    _conductorProcess.Start();

                    if (Config.HolochainConductorToUse == HolochainConductorEnum.HcDevTool)
                    {
                        await Task.Delay(3000);
                        _conductorProcess.Close();
                        _conductorProcess.StartInfo.Arguments = "sandbox run 0";
                        _conductorProcess.Start();
                    }

                    await Task.Delay(Config.SecondsToWaitForHolochainConductorToStart * 1000); // Give the conductor 7 (default) seconds to start up...
                }
            }
            catch (Exception ex)
            {
                HandleError("Error in HoloNETClient.StartConductor method.", ex);
            }
        }

        /// <summary>
        /// This method will start the Holochain Conducutor using the appropriate settings defined in the Config property.
        /// </summary>
        /// <returns></returns>
        public void StartHolochainConductor()
        {
            StartHolochainConductorAsync();
        }

        /// <summary>
        /// This method will retrieve the AgentPubKey & DnaHash from either the Holochain Conductor or HC Sandbox depending on what params are passed in. It will default to retrieving from the Conductor first. It will call RetrieveAgentPubKeyAndDnaHashFromConductor and RetrieveAgentPubKeyAndDnaHashFromSandboxAsync internally.
        /// </summary>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromConductor">Set this to true for HoloNET to automatically retrieve the AgentPubKey & DnaHash from the Holochain Conductor after it has connected. This defaults to true.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromSandbox">Set this to true if you wish HoloNET to automatically retrieve the AgentPubKey & DnaHash from the hc sandbox after it has connected. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromConductorIfSandBoxFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the Holochain Conductor if it fails to get them from the HC Sandbox command. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromSandBoxIfConductorFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <param name="updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the Config property once it has retrieved the DnaHash & AgentPubKey.</param>
        /// <returns>The AgentPubKey and DnaHash</returns>
        public AgentPubKeyDnaHash RetrieveAgentPubKeyAndDnaHash(bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = false, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved = true)
        {
            return RetrieveAgentPubKeyAndDnaHashAsync(RetrieveAgentPubKeyAndDnaHashMode.UseCallBackEvents, retrieveAgentPubKeyAndDnaHashFromConductor, retrieveAgentPubKeyAndDnaHashFromSandbox, automaticallyAttemptToRetrieveFromConductorIfSandBoxFails, automaticallyAttemptToRetrieveFromSandBoxIfConductorFails, updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved).Result;
        }

        /// <summary>
        /// This method will retrieve the AgentPubKey & DnaHash from either the Holochain Conductor or HC Sandbox depending on what params are passed in. It will default to retrieving from the Conductor first. It will call RetrieveAgentPubKeyAndDnaHashFromConductor and RetrieveAgentPubKeyAndDnaHashFromSandboxAsync internally.
        /// </summary>
        /// <param name="retrieveAgentPubKeyAndDnaHashMode">If set to `Wait` (default) it will await until it has finished retrieving the AgentPubKey & DnaHash before returning, otherwise it will return immediately and then raise the OnReadyForZomeCalls event once it has finished retrieving the DnaHash & AgentPubKey.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromConductor">Set this to true for HoloNET to automatically retrieve the AgentPubKey & DnaHash from the Holochain Conductor after it has connected. This defaults to true.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromSandbox">Set this to true if you wish HoloNET to automatically retrieve the AgentPubKey & DnaHash from the hc sandbox after it has connected. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromConductorIfSandBoxFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the Holochain Conductor if it fails to get them from the HC Sandbox command. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromSandBoxIfConductorFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <param name="updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the Config property once it has retrieved the DnaHash & AgentPubKey.</param>
        /// <returns>The AgentPubKey and DnaHash</returns>
        public async Task<AgentPubKeyDnaHash> RetrieveAgentPubKeyAndDnaHashAsync(RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved = true)
        {
            if (RetrievingAgentPubKeyAndDnaHash)
                return null;

            RetrievingAgentPubKeyAndDnaHash = true;

            //Try to first get from the conductor.
            if (retrieveAgentPubKeyAndDnaHashFromConductor)
                return await RetrieveAgentPubKeyAndDnaHashFromConductorAsync(null, retrieveAgentPubKeyAndDnaHashMode, updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved, automaticallyAttemptToRetrieveFromSandBoxIfConductorFails);

            else if (retrieveAgentPubKeyAndDnaHashFromSandbox)
            {
                AgentPubKeyDnaHash agentPubKeyDnaHash = await RetrieveAgentPubKeyAndDnaHashFromSandboxAsync(updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved, automaticallyAttemptToRetrieveFromConductorIfSandBoxFails);

                if (agentPubKeyDnaHash != null)
                    RetrievingAgentPubKeyAndDnaHash = false;
            }

            return new AgentPubKeyDnaHash() { AgentPubKey = Config.AgentPubKey, DnaHash = Config.DnaHash };
        }

        /// <summary>
        /// This method gets the AgentPubKey & DnaHash from the HC Sandbox command. It will raise the [OnReadyForZomeCalls](#onreadyforzomecalls) event once it successfully retrieves them and the WebSocket has connected to the Holochain Conductor. If it fails to retrieve the AgentPubKey and DnaHash from the HC Sandbox and the optional `automaticallyAttemptToRetrieveFromConductorIfSandBoxFails` flag is true (defaults to true), it will call the RetrieveAgentPubKeyAndDnaHashFromConductor method to attempt to retrieve them directly from the conductor (default).
        /// </summary>
        /// <param name="updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the Config property once it has retrieved the DnaHash & AgentPubKey.</param>
        /// <param name="automaticallyAttemptToRetrieveFromConductorIfSandBoxFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <returns>The AgentPubKey and DnaHash</returns>
        public async Task<AgentPubKeyDnaHash> RetrieveAgentPubKeyAndDnaHashFromSandboxAsync(bool updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true)
        {
            try
            {
                if (RetrievingAgentPubKeyAndDnaHash)
                    return null;

                RetrievingAgentPubKeyAndDnaHash = true;
                Logger.Log("Attempting To Retrieve AgentPubKey & DnaHash From hc sandbox...", LogType.Info, true);

                if (string.IsNullOrEmpty(Config.FullPathToExternalHCToolBinary))
                    Config.FullPathToExternalHCToolBinary = string.Concat(Directory.GetCurrentDirectory(), "\\HolochainBinaries\\hc.exe"); //default to the current path

                Process pProcess = new Process();
                pProcess.StartInfo.WorkingDirectory = Config.FullPathToRootHappFolder;
                pProcess.StartInfo.FileName = "hc";
                //pProcess.StartInfo.FileName = Config.FullPathToExternalHCToolBinary; //TODO: Need to get this working later (think currently has a version conflict with keylairstone? But not urgent because AgentPubKey & DnaHash are retrieved from Conductor anyway.
                pProcess.StartInfo.Arguments = "sandbox call list-cells";
                pProcess.StartInfo.UseShellExecute = false;
                pProcess.StartInfo.RedirectStandardOutput = true;
                pProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                pProcess.StartInfo.CreateNoWindow = false;
                pProcess.Start();

                string output = pProcess.StandardOutput.ReadToEnd();
                int dnaStart = output.IndexOf("DnaHash") + 8;
                int dnaEnd = output.IndexOf(")", dnaStart);
                int agentStart = output.IndexOf("AgentPubKey") + 12;
                int agentEnd = output.IndexOf(")", agentStart);

                string dnaHash = output.Substring(dnaStart, dnaEnd - dnaStart);
                string agentPubKey = output.Substring(agentStart, agentEnd - agentStart);

                if (!string.IsNullOrEmpty(dnaHash) && !string.IsNullOrEmpty(agentPubKey))
                {
                    if (updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved)
                    {
                        Config.AgentPubKey = agentPubKey;
                        Config.DnaHash = dnaHash;
                    }

                    Logger.Log("AgentPubKey & DnaHash successfully retrieved from hc sandbox.", LogType.Info, false);

                    if (WebSocket.State == WebSocketState.Open && !string.IsNullOrEmpty(Config.AgentPubKey) && !string.IsNullOrEmpty(Config.DnaHash))
                        SetReadyForZomeCalls();

                    return new AgentPubKeyDnaHash() { DnaHash = dnaHash, AgentPubKey = agentPubKey };
                }
                else if (automaticallyAttemptToRetrieveFromConductorIfSandBoxFails)
                    await RetrieveAgentPubKeyAndDnaHashFromConductorAsync();
            }
            catch (Exception ex)
            {
                HandleError("Error in HoloNETClient.retrieveAgentPubKeyAndDnaHashFromSandbox method getting DnaHash & AgentPubKey from hApp.", ex);
            }

            return null;
        }

        /// <summary>
        /// This method gets the AgentPubKey & DnaHash from the HC Sandbox command. It will raise the [OnReadyForZomeCalls](#onreadyforzomecalls) event once it successfully retrieves them and the WebSocket has connected to the Holochain Conductor. If it fails to retrieve the AgentPubKey and DnaHash from the HC Sandbox and the optional `automaticallyAttemptToRetrieveFromConductorIfSandBoxFails` flag is true (defaults to true), it will call the RetrieveAgentPubKeyAndDnaHashFromConductor method to attempt to retrieve them directly from the conductor (default).
        /// </summary>
        /// <param name="updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the Config property once it has retrieved the DnaHash & AgentPubKey.</param>
        /// <param name="automaticallyAttemptToRetrieveFromConductorIfSandBoxFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <returns>The AgentPubKey and DnaHash</returns>
        public AgentPubKeyDnaHash RetrieveAgentPubKeyAndDnaHashFromSandbox(bool updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true)
        {
            return RetrieveAgentPubKeyAndDnaHashFromSandboxAsync(updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved, automaticallyAttemptToRetrieveFromConductorIfSandBoxFails).Result;
        }

        /// <summary>
        /// This method gets the AgentPubKey & DnaHash from the Holochain Conductor (the Connect method will automatically call this by default). Once it has retrieved them and the WebSocket has connceted to the Holochain Conductor it will raise the OnReadyForZomeCalls event. If it fails to retrieve the AgentPubKey and DnaHash from the Conductor and the optional `automaticallyAttemptToRetrieveFromSandBoxIfConductorFails` flag is true (defaults to true), it will call the RetrieveAgentPubKeyAndDnaHashFromSandbox method. 
        /// </summary>
        /// <param name="retrieveAgentPubKeyAndDnaHashMode">If set to `Wait` (default) it will await until it has finished retrieving the AgentPubKey & DnaHash before returning, otherwise it will return immediately and then raise the OnReadyForZomeCalls event once it has finished retrieving the DnaHash & AgentPubKey.</param>
        /// <param name="updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the Config property once it has retrieved the DnaHash & AgentPubKey.</param>
        /// <param name="automaticallyAttemptToRetrieveFromSandBoxIfConductorFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <returns>The AgentPubKey and DnaHash</returns>
        public async Task<AgentPubKeyDnaHash> RetrieveAgentPubKeyAndDnaHashFromConductorAsync(string installedAppId = null, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true)
        {
            try
            {
                //if (RetrievingAgentPubKeyAndDnaHash)
                //    return null;

                if (string.IsNullOrEmpty(installedAppId))
                {
                    if (!string.IsNullOrEmpty(Config.InstalledAppId))
                        installedAppId = Config.InstalledAppId;
                    else
                        installedAppId = "test-app";
                }

                _automaticallyAttemptToGetFromSandboxIfConductorFails = automaticallyAttemptToRetrieveFromSandBoxIfConductorFails;
                RetrievingAgentPubKeyAndDnaHash = true;
                _updateDnaHashAndAgentPubKey = updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved;
                Logger.Log("Attempting To Retrieve AgentPubKey & DnaHash from Holochain Conductor...", LogType.Info, true);

                HoloNETData holoNETData = new HoloNETData()
                {
                    type = "app_info",
                    data = new AppInfoRequest()
                    {
                        installed_app_id = installedAppId
                    }
                };

                await SendHoloNETRequestAsync(holoNETData);

                if (retrieveAgentPubKeyAndDnaHashMode == RetrieveAgentPubKeyAndDnaHashMode.Wait)
                    return await _taskCompletionAgentPubKeyAndDnaHashRetrieved.Task;
            }
            catch (Exception ex)
            {
                HandleError("Error occurred in HoloNETClient.retrieveAgentPubKeyAndDnaHashFromConductor method getting DnaHash & AgentPubKey from hApp.", ex);
            }

            return null;
        }

        /// <summary>
        /// This method gets the AgentPubKey & DnaHash from the Holochain Conductor (the Connect method will automatically call this by default). Once it has retrieved them and the WebSocket has connceted to the Holochain Conductor it will raise the OnReadyForZomeCalls event. If it fails to retrieve the AgentPubKey and DnaHash from the Conductor and the optional `automaticallyAttemptToRetrieveFromSandBoxIfConductorFails` flag is true (defaults to true), it will call the RetrieveAgentPubKeyAndDnaHashFromSandbox method. 
        /// </summary>
        /// <param name="updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the Config property once it has retrieved the DnaHash & AgentPubKey.</param>
        /// <param name="automaticallyAttemptToRetrieveFromSandBoxIfConductorFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <returns>The AgentPubKey and DnaHash</returns>
        public AgentPubKeyDnaHash RetrieveAgentPubKeyAndDnaHashFromConductor(string installedAppId = null, bool updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true)
        {
            return RetrieveAgentPubKeyAndDnaHashFromConductorAsync(installedAppId, RetrieveAgentPubKeyAndDnaHashMode.UseCallBackEvents, updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved, automaticallyAttemptToRetrieveFromSandBoxIfConductorFails).Result;
        }

        /// <summary>
        /// This method allows you to send your own raw request to holochain. This method raises the OnDataRecived event once it has received a response from the Holochain conductor.
        /// </summary>
        /// <param name="id">The id of the request to send to the Holochain Conductor. This will be matched to the id in the response received from the Holochain Conductor.</param>
        /// <param name="holoNETData">The raw data packet you wish to send to the Holochain conductor.</param>
        /// <returns></returns>
        public async Task SendHoloNETRequestAsync(HoloNETData holoNETData, string id = "")
        {
            await SendHoloNETRequestAsync(MessagePackSerializer.Serialize(holoNETData), id);
        }

        /// <summary>
        /// This method allows you to send your own raw request to holochain. This method raises the OnDataRecived event once it has received a response from the Holochain conductor.
        /// </summary>
        /// <param name="id">The id of the request to send to the Holochain Conductor. This will be matched to the id in the response received from the Holochain Conductor.</param>
        /// <param name="holoNETData">The raw data packet you wish to send to the Holochain conductor.</param>
        public void SendHoloNETRequest(HoloNETData holoNETData, string id = "")
        {
            SendHoloNETRequestAsync(holoNETData, id);
        }

        /// <summary>
        /// This method allows you to send your own raw request to holochain. This method raises the OnDataRecived event once it has received a response from the Holochain conductor.
        /// </summary>
        /// <param name="id">The id of the request to send to the Holochain Conductor. This will be matched to the id in the response received from the Holochain Conductor.</param>
        /// <param name="holoNETData">The raw data packet you wish to send to the Holochain conductor.</param>
        public async Task SendHoloNETRequestAsync(byte[] data, string id = "")
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    id = GetRequestId();

                HoloNETRequest request = new HoloNETRequest()
                {
                    id = Convert.ToUInt64(id),
                    type = "request",
                    data = data
                };

                if (Config.EnforceRequestToResponseIdMatchingBehaviour != EnforceRequestToResponseIdMatchingBehaviour.Ignore)
                    _pendingRequests.Add(id);

                if (WebSocket.State == WebSocketState.Open)
                {
                    Logger.Log("Sending HoloNET Request to Holochain Conductor...", LogType.Info, true);
                    await WebSocket.SendRawDataAsync(MessagePackSerializer.Serialize(request)); //This is the fastest and most popular .NET MessagePack Serializer.
                    //await WebSocket.UnityWebSocket.Send(MessagePackSerializer.Serialize(request));
                    Logger.Log("HoloNET Request Successfully Sent To Holochain Conductor.", LogType.Info, false);
                }
            }
            catch (Exception ex)
            {
                HandleError("Error occurred in HoloNETClient.SendHoloNETRequest method.", ex);
            }
        }

        /// <summary>
        /// This method allows you to send your own raw request to holochain. This method raises the OnDataRecived event once it has received a response from the Holochain conductor.
        /// </summary>
        /// <param name="id">The id of the request to send to the Holochain Conductor. This will be matched to the id in the response received from the Holochain Conductor.</param>
        /// <param name="holoNETData">The raw data packet you wish to send to the Holochain conductor.</param>
        public void SendHoloNETRequest(byte[] data, string id = "")
        {
            SendHoloNETRequestAsync(data, id);
        }

        /// <summary>
        /// This method disconnects the client from the Holochain conductor. It raises the OnDisconnected event once it is has successfully disconnected. It will then automatically call the ShutDownAllHolochainConductors method (if the `shutdownHolochainConductorsMode` flag (defaults to `UseConfigSettings`) is not set to `DoNotShutdownAnyConductors`). If the `disconnectedCallBackMode` flag is set to `WaitForHolochainConductorToDisconnect` (default) then it will await until it has disconnected before returning to the caller, otherwise it will return immediately and then raise the OnDisconnected event once it is disconnected.
        /// </summary>
        /// <param name="disconnectedCallBackMode">If this is set to `WaitForHolochainConductorToDisconnect` (default) then it will await until it has disconnected before returning to the caller, otherwise (it is set to `UseCallBackEvents`) it will return immediately and then raise the [OnDisconnected](#ondisconnected) once it is disconnected.</param>
        /// <param name="shutdownHolochainConductorsMode">Once it has successfully disconnected it will automatically call the ShutDownAllHolochainConductors method if the `shutdownHolochainConductorsMode` flag (defaults to `UseConfigSettings`) is not set to `DoNotShutdownAnyConductors`. Other values it can be are 'ShutdownCurrentConductorOnly' or 'ShutdownAllConductors'. Please see the ShutDownConductors method below for more detail.</param>
        /// <returns></returns>
        public async Task DisconnectAsync(DisconnectedCallBackMode disconnectedCallBackMode = DisconnectedCallBackMode.WaitForHolochainConductorToDisconnect, ShutdownHolochainConductorsMode shutdownHolochainConductorsMode = ShutdownHolochainConductorsMode.UseConfigSettings)
        {
            _shutdownHolochainConductorsMode = shutdownHolochainConductorsMode;
            await WebSocket.DisconnectAsync();

            if (disconnectedCallBackMode == DisconnectedCallBackMode.WaitForHolochainConductorToDisconnect)
                await _taskCompletionDisconnected.Task;
        }

        /// <summary>
        /// This method disconnects the client from the Holochain conductor. It raises the OnDisconnected event once it is has successfully disconnected. It will then automatically call the ShutDownAllHolochainConductors method (if the `shutdownHolochainConductorsMode` flag (defaults to `UseConfigSettings`) is not set to `DoNotShutdownAnyConductors`). If the `disconnectedCallBackMode` flag is set to `WaitForHolochainConductorToDisconnect` (default) then it will await until it has disconnected before returning to the caller, otherwise it will return immediately and then raise the OnDisconnected event once it is disconnected.
        /// </summary>
        /// <param name="disconnectedCallBackMode">If this is set to `WaitForHolochainConductorToDisconnect` (default) then it will await until it has disconnected before returning to the caller, otherwise (it is set to `UseCallBackEvents`) it will return immediately and then raise the [OnDisconnected](#ondisconnected) once it is disconnected.</param>
        /// <param name="shutdownHolochainConductorsMode">Once it has successfully disconnected it will automatically call the ShutDownAllHolochainConductors method if the `shutdownHolochainConductorsMode` flag (defaults to `UseConfigSettings`) is not set to `DoNotShutdownAnyConductors`. Other values it can be are 'ShutdownCurrentConductorOnly' or 'ShutdownAllConductors'. Please see the ShutDownConductors method below for more detail.</param>
        /// <returns></returns>
        public void Disconnect(DisconnectedCallBackMode disconnectedCallBackMode = DisconnectedCallBackMode.WaitForHolochainConductorToDisconnect, ShutdownHolochainConductorsMode shutdownHolochainConductorsMode = ShutdownHolochainConductorsMode.UseConfigSettings)
        {
            DisconnectAsync(disconnectedCallBackMode, shutdownHolochainConductorsMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor. 
        /// </summary>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting.</param>
        /// <returns></returns>
        public async Task<ZomeFunctionCallBackEventArgs> CallZomeFunctionAsync(string zome, string function, object paramsObject)
        {
            _currentId++;
            return await CallZomeFunctionAsync(_currentId.ToString(), zome, function, null, paramsObject, true, false, ConductorResponseCallBackMode.WaitForHolochainConductorResponse);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor. 
        /// </summary>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public async Task<ZomeFunctionCallBackEventArgs> CallZomeFunctionAsync(string zome, string function, object paramsObject, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _currentId++;
            return await CallZomeFunctionAsync(_currentId.ToString(), zome, function, null, paramsObject, true, false, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor. 
        /// </summary>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="cachReturnData">This is an optional param, which defaults to false. Set this to true if you wish HoloNET to cache the response retrieved from holochain. Subsequent calls will return this cached data rather than calling the Holochain conductor again. Use this for static data that is not going to change for performance gains.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public async Task<ZomeFunctionCallBackEventArgs> CallZomeFunctionAsync(string zome, string function, object paramsObject, bool cachReturnData = false, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _currentId++;
            return await CallZomeFunctionAsync(_currentId.ToString(), zome, function, null, paramsObject, true, cachReturnData, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor. 
        /// </summary>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="entryDataObjectTypeReturnedFromZome">This is an optional param, where the caller can pass in the type of the dynamic data object they wish the entry data returned to be mapped to. This newly created data object will then be returned in the ZomeFunctionCallBackEventArgs.Entry.EntryDataObject property.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public async Task<ZomeFunctionCallBackEventArgs> CallZomeFunctionAsync(string zome, string function, object paramsObject, Type entryDataObjectTypeReturnedFromZome = null, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _currentId++;
            return await CallZomeFunctionAsync(_currentId.ToString(), zome, function, paramsObject, entryDataObjectTypeReturnedFromZome, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor. 
        /// </summary>
        /// <param name="id">The id of the HoloNET request sent to the Holochain Conductor. This id is then returned in OnDataReceived, OnZomeFunctionCallBack and OnSignalCallBack events. If this id is not passed used by calling one of the alternative overloads then HoloNET will automatically generate a unique id.</param>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="entryDataObjectTypeReturnedFromZome">This is an optional param, where the caller can pass in the type of the dynamic data object they wish the entry data returned to be mapped to. This newly created data object will then be returned in the ZomeFunctionCallBackEventArgs.Entry.EntryDataObject property.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public async Task<ZomeFunctionCallBackEventArgs> CallZomeFunctionAsync(string id, string zome, string function, object paramsObject, Type entryDataObjectTypeReturnedFromZome = null, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            return await CallZomeFunctionAsync(id, zome, function, paramsObject, true, false, entryDataObjectTypeReturnedFromZome, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor. 
        /// </summary>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="entryDataObjectReturnedFromZome">This is an optional param, where the caller can pass in the type of the dynamic data object they wish the entry data returned to be mapped to.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public async Task<ZomeFunctionCallBackEventArgs> CallZomeFunctionAsync(string zome, string function, object paramsObject, dynamic entryDataObjectReturnedFromZome = null, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _currentId++;
            return await CallZomeFunctionAsync(_currentId.ToString(), zome, function, paramsObject, entryDataObjectReturnedFromZome, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor. 
        /// </summary>
        /// <param name="id">The unique id you wish to assign for this call. This id is then returned in OnDataReceived, OnZomeFunctionCallBack and OnSignalCallBack events. There are overloads that omit this param, use these overloads if you wish HoloNET to auto-generate and manage the id's for you.</param>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="entryDataObjectReturnedFromZome">This is an optional param, where the caller can pass in an instance of the dynamic data object they wish the entry data returned to be mapped to. This data object will then be returned in the ZomeFunctionCallBackEventArgs.Entry.EntryDataObject property.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public async Task<ZomeFunctionCallBackEventArgs> CallZomeFunctionAsync(string id, string zome, string function, object paramsObject, dynamic entryDataObjectReturnedFromZome = null, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            return await CallZomeFunctionAsync(id, zome, function, paramsObject, true, false, entryDataObjectReturnedFromZome, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor. 
        /// </summary>
        /// <param name="id">The unique id you wish to assign for this call. This id is then returned in OnDataReceived, OnZomeFunctionCallBack and OnSignalCallBack events. There are overloads that omit this param, use these overloads if you wish HoloNET to auto-generate and manage the id's for you.</param>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="matchIdToZomeFuncInCallback">This is an optional param, which defaults to true. Set this to true if you wish HoloNET to give the zome and zome function that made the call in the callback/event. If this is false then only the id will be given in the callback. This uses a small internal cache to match up the id to the given zome/function. Set this to false if you wish to save a tiny amount of memory by not utilizing this cache. If it is false then the `Zome` and `ZomeFunction` params will be missing in the ZomeCallBack, you will need to manually match the `id` to the call yourself. NOTE: If Config.EnforceRequestToResponseIdMatchingBehaviour is set to Warn or Error then this param will be ignored and the matching will always occur.</param>
        /// <param name="cachReturnData">This is an optional param, which defaults to false. Set this to true if you wish HoloNET to cache the response retrieved from holochain. Subsequent calls will return this cached data rather than calling the Holochain conductor again. Use this for static data that is not going to change for performance gains.</param>
        /// <param name="entryDataObjectTypeReturnedFromZome">This is an optional param, where the caller can pass in the type of the dynamic data object they wish the entry data returned to be mapped to. This newly created data object will then be returned in the ZomeFunctionCallBackEventArgs.Entry.EntryDataObject property.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public async Task<ZomeFunctionCallBackEventArgs> CallZomeFunctionAsync(string id, string zome, string function, object paramsObject, bool matchIdToZomeFuncInCallback = true, bool cachReturnData = false, Type entryDataObjectTypeReturnedFromZome = null, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _entryDataObjectTypeLookup[id] = entryDataObjectTypeReturnedFromZome;
            return await CallZomeFunctionAsync(id, zome, function, null, paramsObject, matchIdToZomeFuncInCallback, cachReturnData, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor. 
        /// </summary>
        /// <param name="id">The unique id you wish to assign for this call. This id is then returned in OnDataReceived, OnZomeFunctionCallBack and OnSignalCallBack events. There are overloads that omit this param, use these overloads if you wish HoloNET to auto-generate and manage the id's for you.</param>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="matchIdToZomeFuncInCallback">This is an optional param, which defaults to true. Set this to true if you wish HoloNET to give the zome and zome function that made the call in the callback/event. If this is false then only the id will be given in the callback. This uses a small internal cache to match up the id to the given zome/function. Set this to false if you wish to save a tiny amount of memory by not utilizing this cache. If it is false then the `Zome` and `ZomeFunction` params will be missing in the ZomeCallBack, you will need to manually match the `id` to the call yourself. NOTE: If Config.EnforceRequestToResponseIdMatchingBehaviour is set to Warn or Error then this param will be ignored and the matching will always occur.</param>
        /// <param name="cachReturnData">This is an optional param, which defaults to false. Set this to true if you wish HoloNET to cache the response retrieved from holochain. Subsequent calls will return this cached data rather than calling the Holochain conductor again. Use this for static data that is not going to change for performance gains.</param>
        /// <param name="entryDataObjectReturnedFromZome">This is an optional param, where the caller can pass in an instance of the dynamic data object they wish the entry data returned to be mapped to. This data object will then be returned in the ZomeFunctionCallBackEventArgs.Entry.EntryDataObject property.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public async Task<ZomeFunctionCallBackEventArgs> CallZomeFunctionAsync(string id, string zome, string function, object paramsObject, bool matchIdToZomeFuncInCallback = true, bool cachReturnData = false, dynamic entryDataObjectReturnedFromZome = null, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _entryDataObjectLookup[id] = entryDataObjectReturnedFromZome;
            return await CallZomeFunctionAsync(id, zome, function, null, paramsObject, matchIdToZomeFuncInCallback, cachReturnData, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor.  
        /// </summary>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="callback">A delegate to call once the zome function returns. This delegate contains the same signature as the one used for the OnZomeFunctionCallBack event. (optional param).</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <returns></returns>
        public async Task<ZomeFunctionCallBackEventArgs> CallZomeFunctionAsync(string zome, string function, ZomeFunctionCallBack callback, object paramsObject)
        {
            _currentId++;
            return await CallZomeFunctionAsync(_currentId.ToString(), zome, function, callback, paramsObject, true, false, ConductorResponseCallBackMode.WaitForHolochainConductorResponse);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor.   
        /// </summary>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="callback">A delegate to call once the zome function returns. This delegate contains the same signature as the one used for the OnZomeFunctionCallBack event. (optional param).</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public async Task<ZomeFunctionCallBackEventArgs> CallZomeFunctionAsync(string zome, string function, ZomeFunctionCallBack callback, object paramsObject, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _currentId++;
            return await CallZomeFunctionAsync(_currentId.ToString(), zome, function, callback, paramsObject, true, false, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor.    
        /// </summary>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="callback">A delegate to call once the zome function returns. This delegate contains the same signature as the one used for the OnZomeFunctionCallBack event. (optional param).</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="cachReturnData">This is an optional param, which defaults to false. Set this to true if you wish HoloNET to cache the response retrieved from holochain. Subsequent calls will return this cached data rather than calling the Holochain conductor again. Use this for static data that is not going to change for performance gains.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public async Task<ZomeFunctionCallBackEventArgs> CallZomeFunctionAsync(string zome, string function, ZomeFunctionCallBack callback, object paramsObject, bool cachReturnData = false, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _currentId++;
            return await CallZomeFunctionAsync(_currentId.ToString(), zome, function, callback, paramsObject, true, cachReturnData, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor.    
        /// </summary>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="callback">A delegate to call once the zome function returns. This delegate contains the same signature as the one used for the OnZomeFunctionCallBack event. (optional param).</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="entryDataObjectTypeReturnedFromZome">This is an optional param, where the caller can pass in the type of the dynamic data object they wish the entry data returned to be mapped to. This newly created data object will then be returned in the ZomeFunctionCallBackEventArgs.Entry.EntryDataObject property.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public async Task<ZomeFunctionCallBackEventArgs> CallZomeFunctionAsync(string zome, string function, ZomeFunctionCallBack callback, object paramsObject, Type entryDataObjectTypeReturnedFromZome = null, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _currentId++;
            return await CallZomeFunctionAsync(_currentId.ToString(), zome, function, callback, paramsObject, true, false, entryDataObjectTypeReturnedFromZome, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor.     
        /// </summary>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="callback">A delegate to call once the zome function returns. This delegate contains the same signature as the one used for the OnZomeFunctionCallBack event. (optional param).</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="entryDataObjectReturnedFromZome">This is an optional param, where the caller can pass in an instance of the dynamic data object they wish the entry data returned to be mapped to. This data object will then be returned in the ZomeFunctionCallBackEventArgs.Entry.EntryDataObject property.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public async Task<ZomeFunctionCallBackEventArgs> CallZomeFunctionAsync(string zome, string function, ZomeFunctionCallBack callback, object paramsObject, dynamic entryDataObjectReturnedFromZome = null, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _currentId++;
            return await CallZomeFunctionAsync(_currentId.ToString(), zome, function, callback, paramsObject, true, false, entryDataObjectReturnedFromZome, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor.      
        /// </summary>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="callback">A delegate to call once the zome function returns. This delegate contains the same signature as the one used for the OnZomeFunctionCallBack event. (optional param).</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="cachReturnData">This is an optional param, which defaults to false. Set this to true if you wish HoloNET to cache the response retrieved from holochain. Subsequent calls will return this cached data rather than calling the Holochain conductor again. Use this for static data that is not going to change for performance gains.</param>
        /// <param name="entryDataObjectTypeReturnedFromZome">This is an optional param, where the caller can pass in the type of the dynamic data object they wish the entry data returned to be mapped to. This newly created data object will then be returned in the ZomeFunctionCallBackEventArgs.Entry.EntryDataObject property.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public async Task<ZomeFunctionCallBackEventArgs> CallZomeFunctionAsync(string zome, string function, ZomeFunctionCallBack callback, object paramsObject, bool cachReturnData = false, Type entryDataObjectTypeReturnedFromZome = null, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _currentId++;
            return await CallZomeFunctionAsync(_currentId.ToString(), zome, function, callback, paramsObject, true, cachReturnData, entryDataObjectTypeReturnedFromZome, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor.       
        /// </summary>
        /// <param name="id">The unique id you wish to assign for this call. This id is then returned in OnDataReceived, OnZomeFunctionCallBack and OnSignalCallBack events. There are overloads that omit this param, use these overloads if you wish HoloNET to auto-generate and manage the id's for you.</param>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="callback">A delegate to call once the zome function returns. This delegate contains the same signature as the one used for the OnZomeFunctionCallBack event. (optional param).</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="matchIdToZomeFuncInCallback">This is an optional param, which defaults to true. Set this to true if you wish HoloNET to give the zome and zome function that made the call in the callback/event. If this is false then only the id will be given in the callback. This uses a small internal cache to match up the id to the given zome/function. Set this to false if you wish to save a tiny amount of memory by not utilizing this cache. If it is false then the `Zome` and `ZomeFunction` params will be missing in the ZomeCallBack, you will need to manually match the `id` to the call yourself. NOTE: If Config.EnforceRequestToResponseIdMatchingBehaviour is set to Warn or Error then this param will be ignored and the matching will always occur.</param>
        /// <param name="cachReturnData">This is an optional param, which defaults to false. Set this to true if you wish HoloNET to cache the response retrieved from holochain. Subsequent calls will return this cached data rather than calling the Holochain conductor again. Use this for static data that is not going to change for performance gains.</param>
        /// <param name="entryDataObjectReturnedFromZome">This is an optional param, where the caller can pass in an instance of the dynamic data object they wish the entry data returned to be mapped to. This data object will then be returned in the ZomeFunctionCallBackEventArgs.Entry.EntryDataObject property.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public async Task<ZomeFunctionCallBackEventArgs> CallZomeFunctionAsync(string id, string zome, string function, ZomeFunctionCallBack callback, object paramsObject, bool matchIdToZomeFuncInCallback = true, bool cachReturnData = false, dynamic entryDataObjectReturnedFromZome = null, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _entryDataObjectLookup[id] = entryDataObjectReturnedFromZome;
            return await CallZomeFunctionAsync(id, zome, function, callback, paramsObject, matchIdToZomeFuncInCallback, cachReturnData, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor.       
        /// </summary>
        /// <param name="id">The unique id you wish to assign for this call. This id is then returned in OnDataReceived, OnZomeFunctionCallBack and OnSignalCallBack events. There are overloads that omit this param, use these overloads if you wish HoloNET to auto-generate and manage the id's for you.</param>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="callback">A delegate to call once the zome function returns. This delegate contains the same signature as the one used for the OnZomeFunctionCallBack event. (optional param).</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="matchIdToZomeFuncInCallback">This is an optional param, which defaults to true. Set this to true if you wish HoloNET to give the zome and zome function that made the call in the callback/event. If this is false then only the id will be given in the callback. This uses a small internal cache to match up the id to the given zome/function. Set this to false if you wish to save a tiny amount of memory by not utilizing this cache. If it is false then the `Zome` and `ZomeFunction` params will be missing in the ZomeCallBack, you will need to manually match the `id` to the call yourself. NOTE: If Config.EnforceRequestToResponseIdMatchingBehaviour is set to Warn or Error then this param will be ignored and the matching will always occur.</param>
        /// <param name="cachReturnData">This is an optional param, which defaults to false. Set this to true if you wish HoloNET to cache the response retrieved from holochain. Subsequent calls will return this cached data rather than calling the Holochain conductor again. Use this for static data that is not going to change for performance gains.</param>
        /// <param name="entryDataObjectTypeReturnedFromZome">This is an optional param, where the caller can pass in the type of the dynamic data object they wish the entry data returned to be mapped to. This newly created data object will then be returned in the ZomeFunctionCallBackEventArgs.Entry.EntryDataObject property.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public async Task<ZomeFunctionCallBackEventArgs> CallZomeFunctionAsync(string id, string zome, string function, ZomeFunctionCallBack callback, object paramsObject, bool matchIdToZomeFuncInCallback = true, bool cachReturnData = false, Type entryDataObjectTypeReturnedFromZome = null, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _entryDataObjectTypeLookup[id] = entryDataObjectTypeReturnedFromZome;
            return await CallZomeFunctionAsync(id, zome, function, callback, paramsObject, matchIdToZomeFuncInCallback, cachReturnData, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor.       
        /// </summary>
        /// <param name="id">The unique id you wish to assign for this call. This id is then returned in OnDataReceived, OnZomeFunctionCallBack and OnSignalCallBack events. There are overloads that omit this param, use these overloads if you wish HoloNET to auto-generate and manage the id's for you.</param>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="callback">A delegate to call once the zome function returns. This delegate contains the same signature as the one used for the OnZomeFunctionCallBack event. (optional param).</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="matchIdToZomeFuncInCallback">This is an optional param, which defaults to true. Set this to true if you wish HoloNET to give the zome and zome function that made the call in the callback/event. If this is false then only the id will be given in the callback. This uses a small internal cache to match up the id to the given zome/function. Set this to false if you wish to save a tiny amount of memory by not utilizing this cache. If it is false then the `Zome` and `ZomeFunction` params will be missing in the ZomeCallBack, you will need to manually match the `id` to the call yourself. NOTE: If Config.EnforceRequestToResponseIdMatchingBehaviour is set to Warn or Error then this param will be ignored and the matching will always occur.</param>
        /// <param name="cachReturnData">This is an optional param, which defaults to false. Set this to true if you wish HoloNET to cache the response retrieved from holochain. Subsequent calls will return this cached data rather than calling the Holochain conductor again. Use this for static data that is not going to change for performance gains.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public async Task<ZomeFunctionCallBackEventArgs> CallZomeFunctionAsync(string id, string zome, string function, ZomeFunctionCallBack callback, object paramsObject, bool matchIdToZomeFuncInCallback = true, bool cachReturnData = false, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            try
            {
                _taskCompletionZomeCallBack[id] = new TaskCompletionSource<ZomeFunctionCallBackEventArgs>();

                if (WebSocket.State == WebSocketState.Closed || WebSocket.State == WebSocketState.None)
                    await ConnectAsync();

                await _taskCompletionReadyForZomeCalls.Task;

                if (string.IsNullOrEmpty(Config.DnaHash))
                    return new ZomeFunctionCallBackEventArgs() { EndPoint = EndPoint, Id = id, Zome = zome, ZomeFunction = function, IsError = true, Message = "The DnaHash cannot be empty, please either set manually in the Config.DnaHash property or wait till the ReadyForZomeCalls event is fired before attempting to make a zome call." };
                    //throw new InvalidOperationException("The DnaHash cannot be empty, please either set manually in the Config.DnaHash property or wait till the ReadyForZomeCalls event is fired before attempting to make a zome call.");

                if (string.IsNullOrEmpty(Config.AgentPubKey))
                    return new ZomeFunctionCallBackEventArgs() { EndPoint = EndPoint, Id = id, Zome = zome, ZomeFunction = function, IsError = true, Message = "The AgentPubKey cannot be empty, please either set manually in the Config.DnaHash property or wait till the ReadyForZomeCalls event is fired before attempting to make a zome call." };
                    //throw new InvalidOperationException("The AgentPubKey cannot be empty, please either set manually in the Config.AgentPubKey property or wait till the ReadyForZomeCalls event is fired before attempting to make a zome call.");

                Logger.Log($"Calling Zome Function {function} on Zome {zome} with Id {id} On Holochain Conductor...", LogType.Info, true);

                _cacheZomeReturnDataLookup[id] = cachReturnData;

                if (cachReturnData)
                {
                    if (_zomeReturnDataLookup.ContainsKey(id))
                    {
                        Logger.Log("Caching Enabled so returning data from cache...", LogType.Info);
                        Logger.Log(string.Concat("Id: ", _zomeReturnDataLookup[id].Id, ", Zome: ", _zomeReturnDataLookup[id].Zome, ", Zome Function: ", _zomeReturnDataLookup[id].ZomeFunction, ", Is Zome Call Successful: ", _zomeReturnDataLookup[id].IsCallSuccessful ? "True" : "False", ", Raw Zome Return Data: ", _zomeReturnDataLookup[id].RawZomeReturnData, ", Zome Return Data: ", _zomeReturnDataLookup[id].ZomeReturnData, ", JSON Raw Data: ", _zomeReturnDataLookup[id].RawJSONData), LogType.Info);

                        if (callback != null)
                            callback.DynamicInvoke(this, _zomeReturnDataLookup[id]);

                        OnZomeFunctionCallBack?.Invoke(this, _zomeReturnDataLookup[id]);
                        _taskCompletionZomeCallBack[id].SetResult(_zomeReturnDataLookup[id]);

                        Task<ZomeFunctionCallBackEventArgs> returnValue = _taskCompletionZomeCallBack[id].Task;
                        _taskCompletionZomeCallBack.Remove(id);

                        return await returnValue;
                    }
                }

                if (matchIdToZomeFuncInCallback || Config.EnforceRequestToResponseIdMatchingBehaviour != EnforceRequestToResponseIdMatchingBehaviour.Ignore)
                {
                    _zomeLookup[id] = zome;
                    _funcLookup[id] = function;
                }

                if (callback != null)
                    _callbackLookup[id] = callback;

                try
                {
                    if (paramsObject != null && paramsObject.GetType() == typeof(string))
                    {
                        byte[] holoHash = null;
                        holoHash = ConvertHoloHashToBytes(paramsObject.ToString());

                        if (holoHash != null)
                            paramsObject = holoHash;
                    }
                }
                catch (Exception ex)
                {

                }

                //TODO: Also be good to implement same functionality in js client where it will error if there is no matching request to a response (id etc). I think we can make this optional param for these method overloads like responseMustHaveMatchingRequest which defaults to true.

                byte[][] cellId = await GetCellIdAsync();

                if (_signingCredentialsForCell.ContainsKey(cellId) && _signingCredentialsForCell[cellId] != null)
                {
                    ZomeCall payload = new ZomeCall()
                    {
                        cap_secret = _signingCredentialsForCell[cellId].CapSecret,
                        cell_id = new byte[2][] { ConvertHoloHashToBytes(Config.DnaHash), ConvertHoloHashToBytes(Config.AgentPubKey) },
                        fn_name = function,
                        zome_name = zome,
                        payload = MessagePackSerializer.Serialize(paramsObject),
                        //provenance = ConvertHoloHashToBytes(Config.AgentPubKey),
                        provenance = _signingCredentialsForCell[cellId].SigningKey,
                        nonce = RandomNumberGenerator.GetBytes(32),
                        expires_at = DateTime.Now.AddMinutes(5).Ticks / 10 //DateTime.Now.AddMinutes(5).ToBinary(), //Conductor expects it in microseconds.
                    };

                    byte[] hash = Blake2b.ComputeHash(MessagePackSerializer.Serialize(payload));
                    var sig = Ed25519.Sign(hash, _signingCredentialsForCell[cellId].KeyPair.PrivateKey);

                    ZomeCallSigned signedPayload = new ZomeCallSigned()
                    {
                        cap_secret = payload.cap_secret,
                        cell_id = payload.cell_id,
                        fn_name = payload.fn_name,
                        zome_name = payload.zome_name,
                        payload = payload.payload,
                        provenance = payload.provenance,
                        nonce = payload.nonce,
                        expires_at = payload.expires_at,
                        signature = sig
                    };

                    HoloNETData holoNETData = new HoloNETData()
                    {
                        type = "zome_call",
                        data = signedPayload
                    };

                    await SendHoloNETRequestAsync(holoNETData, id);

                    if (zomeResultCallBackMode == ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
                    {
                        Task<ZomeFunctionCallBackEventArgs> returnValue = _taskCompletionZomeCallBack[id].Task;
                        return await returnValue;
                    }
                    else
                        return new ZomeFunctionCallBackEventArgs() { EndPoint = EndPoint, Id = id, Zome = zome, ZomeFunction = function, Message = "ZomeResultCallBackMode is set to UseCallBackEvents so please wait for OnZomeFunctionCallBack event for zome result." };
                }
                else
                {
                    string msg = $"Error occurred in HoloNETClient.CallZomeFunctionAsync method: Cannot sign zome call when no signing credentials have been authorized for cell {cellId} (AgentPubKey: {Config.AgentPubKey}, DnaHash: {Config.DnaHash}).";
                    HandleError(msg, null);
                    return new ZomeFunctionCallBackEventArgs() { EndPoint = EndPoint, Id = id, Zome = zome, ZomeFunction = function, Message = msg, IsError = true };
                }
            }
            catch (Exception ex)
            {
                HandleError("Error occurred in HoloNETClient.CallZomeFunctionAsync method.", ex);
                return new ZomeFunctionCallBackEventArgs() { EndPoint = EndPoint, Id = id, Zome = zome, ZomeFunction = function, IsError = true, Message = $"Error occurred in HoloNETClient.CallZomeFunctionAsync method. Details: {ex}", Excception = ex };
            }
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor. 
        /// </summary>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting.</param>
        /// <returns></returns>
        public ZomeFunctionCallBackEventArgs CallZomeFunction(string zome, string function, object paramsObject)
        {
            _currentId++;
            return CallZomeFunction(_currentId.ToString(), zome, function, null, paramsObject, true, false, ConductorResponseCallBackMode.WaitForHolochainConductorResponse);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor. 
        /// </summary>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public ZomeFunctionCallBackEventArgs CallZomeFunction(string zome, string function, object paramsObject, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _currentId++;
            return CallZomeFunction(_currentId.ToString(), zome, function, null, paramsObject, true, false, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor. 
        /// </summary>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="cachReturnData">This is an optional param, which defaults to false. Set this to true if you wish HoloNET to cache the response retrieved from holochain. Subsequent calls will return this cached data rather than calling the Holochain conductor again. Use this for static data that is not going to change for performance gains.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public ZomeFunctionCallBackEventArgs CallZomeFunction(string zome, string function, object paramsObject, bool cachReturnData = false, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _currentId++;
            return CallZomeFunction(_currentId.ToString(), zome, function, null, paramsObject, true, cachReturnData, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor. 
        /// </summary>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="entryDataObjectTypeReturnedFromZome">This is an optional param, where the caller can pass in the type of the dynamic data object they wish the entry data returned to be mapped to. This newly created data object will then be returned in the ZomeFunctionCallBackEventArgs.Entry.EntryDataObject property.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public ZomeFunctionCallBackEventArgs CallZomeFunction(string zome, string function, object paramsObject, Type entryDataObjectTypeReturnedFromZome = null, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _currentId++;
            return CallZomeFunction(_currentId.ToString(), zome, function, paramsObject, entryDataObjectTypeReturnedFromZome, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor. 
        /// </summary>
        /// <param name="id">The id of the HoloNET request sent to the Holochain Conductor. This id is then returned in OnDataReceived, OnZomeFunctionCallBack and OnSignalCallBack events. If this id is not passed used by calling one of the alternative overloads then HoloNET will automatically generate a unique id.</param>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="entryDataObjectTypeReturnedFromZome">This is an optional param, where the caller can pass in the type of the dynamic data object they wish the entry data returned to be mapped to. This newly created data object will then be returned in the ZomeFunctionCallBackEventArgs.Entry.EntryDataObject property.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public ZomeFunctionCallBackEventArgs CallZomeFunction(string id, string zome, string function, object paramsObject, Type entryDataObjectTypeReturnedFromZome = null, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            return CallZomeFunction(id, zome, function, paramsObject, true, false, entryDataObjectTypeReturnedFromZome, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor. 
        /// </summary>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="entryDataObjectReturnedFromZome">This is an optional param, where the caller can pass in the type of the dynamic data object they wish the entry data returned to be mapped to.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public ZomeFunctionCallBackEventArgs CallZomeFunction(string zome, string function, object paramsObject, dynamic entryDataObjectReturnedFromZome = null, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _currentId++;
            return CallZomeFunctionAsync(_currentId.ToString(), zome, function, paramsObject, entryDataObjectReturnedFromZome, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor. 
        /// </summary>
        /// <param name="id">The unique id you wish to assign for this call. This id is then returned in OnDataReceived, OnZomeFunctionCallBack and OnSignalCallBack events. There are overloads that omit this param, use these overloads if you wish HoloNET to auto-generate and manage the id's for you.</param>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="entryDataObjectReturnedFromZome">This is an optional param, where the caller can pass in an instance of the dynamic data object they wish the entry data returned to be mapped to. This data object will then be returned in the ZomeFunctionCallBackEventArgs.Entry.EntryDataObject property.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public ZomeFunctionCallBackEventArgs CallZomeFunction(string id, string zome, string function, object paramsObject, dynamic entryDataObjectReturnedFromZome = null, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            return CallZomeFunction(id, zome, function, paramsObject, true, false, entryDataObjectReturnedFromZome, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor. 
        /// </summary>
        /// <param name="id">The unique id you wish to assign for this call. This id is then returned in OnDataReceived, OnZomeFunctionCallBack and OnSignalCallBack events. There are overloads that omit this param, use these overloads if you wish HoloNET to auto-generate and manage the id's for you.</param>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="matchIdToZomeFuncInCallback">This is an optional param, which defaults to true. Set this to true if you wish HoloNET to give the zome and zome function that made the call in the callback/event. If this is false then only the id will be given in the callback. This uses a small internal cache to match up the id to the given zome/function. Set this to false if you wish to save a tiny amount of memory by not utilizing this cache. If it is false then the `Zome` and `ZomeFunction` params will be missing in the ZomeCallBack, you will need to manually match the `id` to the call yourself. NOTE: If Config.EnforceRequestToResponseIdMatchingBehaviour is set to Warn or Error then this param will be ignored and the matching will always occur.</param>
        /// <param name="cachReturnData">This is an optional param, which defaults to false. Set this to true if you wish HoloNET to cache the response retrieved from holochain. Subsequent calls will return this cached data rather than calling the Holochain conductor again. Use this for static data that is not going to change for performance gains.</param>
        /// <param name="entryDataObjectTypeReturnedFromZome">This is an optional param, where the caller can pass in the type of the dynamic data object they wish the entry data returned to be mapped to. This newly created data object will then be returned in the ZomeFunctionCallBackEventArgs.Entry.EntryDataObject property.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public ZomeFunctionCallBackEventArgs CallZomeFunction(string id, string zome, string function, object paramsObject, bool matchIdToZomeFuncInCallback = true, bool cachReturnData = false, Type entryDataObjectTypeReturnedFromZome = null, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _entryDataObjectTypeLookup[id] = entryDataObjectTypeReturnedFromZome;
            return CallZomeFunction(id, zome, function, null, paramsObject, matchIdToZomeFuncInCallback, cachReturnData, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor. 
        /// </summary>
        /// <param name="id">The unique id you wish to assign for this call. This id is then returned in OnDataReceived, OnZomeFunctionCallBack and OnSignalCallBack events. There are overloads that omit this param, use these overloads if you wish HoloNET to auto-generate and manage the id's for you.</param>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="matchIdToZomeFuncInCallback">This is an optional param, which defaults to true. Set this to true if you wish HoloNET to give the zome and zome function that made the call in the callback/event. If this is false then only the id will be given in the callback. This uses a small internal cache to match up the id to the given zome/function. Set this to false if you wish to save a tiny amount of memory by not utilizing this cache. If it is false then the `Zome` and `ZomeFunction` params will be missing in the ZomeCallBack, you will need to manually match the `id` to the call yourself. NOTE: If Config.EnforceRequestToResponseIdMatchingBehaviour is set to Warn or Error then this param will be ignored and the matching will always occur.</param>
        /// <param name="cachReturnData">This is an optional param, which defaults to false. Set this to true if you wish HoloNET to cache the response retrieved from holochain. Subsequent calls will return this cached data rather than calling the Holochain conductor again. Use this for static data that is not going to change for performance gains.</param>
        /// <param name="entryDataObjectReturnedFromZome">This is an optional param, where the caller can pass in an instance of the dynamic data object they wish the entry data returned to be mapped to. This data object will then be returned in the ZomeFunctionCallBackEventArgs.Entry.EntryDataObject property.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public ZomeFunctionCallBackEventArgs CallZomeFunction(string id, string zome, string function, object paramsObject, bool matchIdToZomeFuncInCallback = true, bool cachReturnData = false, dynamic entryDataObjectReturnedFromZome = null, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _entryDataObjectLookup[id] = entryDataObjectReturnedFromZome;
            return CallZomeFunction(id, zome, function, null, paramsObject, matchIdToZomeFuncInCallback, cachReturnData, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor.  
        /// </summary>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="callback">A delegate to call once the zome function returns. This delegate contains the same signature as the one used for the OnZomeFunctionCallBack event. (optional param).</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <returns></returns>
        public ZomeFunctionCallBackEventArgs CallZomeFunction(string zome, string function, ZomeFunctionCallBack callback, object paramsObject)
        {
            _currentId++;
            return CallZomeFunction(_currentId.ToString(), zome, function, callback, paramsObject, true, false, ConductorResponseCallBackMode.WaitForHolochainConductorResponse);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor.   
        /// </summary>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="callback">A delegate to call once the zome function returns. This delegate contains the same signature as the one used for the OnZomeFunctionCallBack event. (optional param).</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public ZomeFunctionCallBackEventArgs CallZomeFunction(string zome, string function, ZomeFunctionCallBack callback, object paramsObject, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _currentId++;
            return CallZomeFunction(_currentId.ToString(), zome, function, callback, paramsObject, true, false, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor.    
        /// </summary>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="callback">A delegate to call once the zome function returns. This delegate contains the same signature as the one used for the OnZomeFunctionCallBack event. (optional param).</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="cachReturnData">This is an optional param, which defaults to false. Set this to true if you wish HoloNET to cache the response retrieved from holochain. Subsequent calls will return this cached data rather than calling the Holochain conductor again. Use this for static data that is not going to change for performance gains.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public ZomeFunctionCallBackEventArgs CallZomeFunction(string zome, string function, ZomeFunctionCallBack callback, object paramsObject, bool cachReturnData = false, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _currentId++;
            return CallZomeFunction(_currentId.ToString(), zome, function, callback, paramsObject, true, cachReturnData, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor.    
        /// </summary>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="callback">A delegate to call once the zome function returns. This delegate contains the same signature as the one used for the OnZomeFunctionCallBack event. (optional param).</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="entryDataObjectTypeReturnedFromZome">This is an optional param, where the caller can pass in the type of the dynamic data object they wish the entry data returned to be mapped to. This newly created data object will then be returned in the ZomeFunctionCallBackEventArgs.Entry.EntryDataObject property.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public ZomeFunctionCallBackEventArgs CallZomeFunction(string zome, string function, ZomeFunctionCallBack callback, object paramsObject, Type entryDataObjectTypeReturnedFromZome = null, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _currentId++;
            return CallZomeFunction(_currentId.ToString(), zome, function, callback, paramsObject, true, false, entryDataObjectTypeReturnedFromZome, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor.     
        /// </summary>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="callback">A delegate to call once the zome function returns. This delegate contains the same signature as the one used for the OnZomeFunctionCallBack event. (optional param).</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="entryDataObjectReturnedFromZome">This is an optional param, where the caller can pass in an instance of the dynamic data object they wish the entry data returned to be mapped to. This data object will then be returned in the ZomeFunctionCallBackEventArgs.Entry.EntryDataObject property.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public ZomeFunctionCallBackEventArgs CallZomeFunction(string zome, string function, ZomeFunctionCallBack callback, object paramsObject, dynamic entryDataObjectReturnedFromZome = null, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _currentId++;
            return CallZomeFunctionAsync(_currentId.ToString(), zome, function, callback, paramsObject, true, false, entryDataObjectReturnedFromZome, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor.      
        /// </summary>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="callback">A delegate to call once the zome function returns. This delegate contains the same signature as the one used for the OnZomeFunctionCallBack event. (optional param).</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="cachReturnData">This is an optional param, which defaults to false. Set this to true if you wish HoloNET to cache the response retrieved from holochain. Subsequent calls will return this cached data rather than calling the Holochain conductor again. Use this for static data that is not going to change for performance gains.</param>
        /// <param name="entryDataObjectTypeReturnedFromZome">This is an optional param, where the caller can pass in the type of the dynamic data object they wish the entry data returned to be mapped to. This newly created data object will then be returned in the ZomeFunctionCallBackEventArgs.Entry.EntryDataObject property.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public ZomeFunctionCallBackEventArgs CallZomeFunction(string zome, string function, ZomeFunctionCallBack callback, object paramsObject, bool cachReturnData = false, Type entryDataObjectTypeReturnedFromZome = null, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _currentId++;
            return CallZomeFunction(_currentId.ToString(), zome, function, callback, paramsObject, true, cachReturnData, entryDataObjectTypeReturnedFromZome, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor.       
        /// </summary>
        /// <param name="id">The unique id you wish to assign for this call. This id is then returned in OnDataReceived, OnZomeFunctionCallBack and OnSignalCallBack events. There are overloads that omit this param, use these overloads if you wish HoloNET to auto-generate and manage the id's for you.</param>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="callback">A delegate to call once the zome function returns. This delegate contains the same signature as the one used for the OnZomeFunctionCallBack event. (optional param).</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="matchIdToZomeFuncInCallback">This is an optional param, which defaults to true. Set this to true if you wish HoloNET to give the zome and zome function that made the call in the callback/event. If this is false then only the id will be given in the callback. This uses a small internal cache to match up the id to the given zome/function. Set this to false if you wish to save a tiny amount of memory by not utilizing this cache. If it is false then the `Zome` and `ZomeFunction` params will be missing in the ZomeCallBack, you will need to manually match the `id` to the call yourself. NOTE: If Config.EnforceRequestToResponseIdMatchingBehaviour is set to Warn or Error then this param will be ignored and the matching will always occur.</param>
        /// <param name="cachReturnData">This is an optional param, which defaults to false. Set this to true if you wish HoloNET to cache the response retrieved from holochain. Subsequent calls will return this cached data rather than calling the Holochain conductor again. Use this for static data that is not going to change for performance gains.</param>
        /// <param name="entryDataObjectReturnedFromZome">This is an optional param, where the caller can pass in an instance of the dynamic data object they wish the entry data returned to be mapped to. This data object will then be returned in the ZomeFunctionCallBackEventArgs.Entry.EntryDataObject property.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public ZomeFunctionCallBackEventArgs CallZomeFunction(string id, string zome, string function, ZomeFunctionCallBack callback, object paramsObject, bool matchIdToZomeFuncInCallback = true, bool cachReturnData = false, dynamic entryDataObjectReturnedFromZome = null, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _entryDataObjectLookup[id] = entryDataObjectReturnedFromZome;
            return CallZomeFunction(id, zome, function, callback, paramsObject, matchIdToZomeFuncInCallback, cachReturnData, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor.       
        /// </summary>
        /// <param name="id">The unique id you wish to assign for this call. This id is then returned in OnDataReceived, OnZomeFunctionCallBack and OnSignalCallBack events. There are overloads that omit this param, use these overloads if you wish HoloNET to auto-generate and manage the id's for you.</param>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="callback">A delegate to call once the zome function returns. This delegate contains the same signature as the one used for the OnZomeFunctionCallBack event. (optional param).</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="matchIdToZomeFuncInCallback">This is an optional param, which defaults to true. Set this to true if you wish HoloNET to give the zome and zome function that made the call in the callback/event. If this is false then only the id will be given in the callback. This uses a small internal cache to match up the id to the given zome/function. Set this to false if you wish to save a tiny amount of memory by not utilizing this cache. If it is false then the `Zome` and `ZomeFunction` params will be missing in the ZomeCallBack, you will need to manually match the `id` to the call yourself. NOTE: If Config.EnforceRequestToResponseIdMatchingBehaviour is set to Warn or Error then this param will be ignored and the matching will always occur.</param>
        /// <param name="cachReturnData">This is an optional param, which defaults to false. Set this to true if you wish HoloNET to cache the response retrieved from holochain. Subsequent calls will return this cached data rather than calling the Holochain conductor again. Use this for static data that is not going to change for performance gains.</param>
        /// <param name="entryDataObjectTypeReturnedFromZome">This is an optional param, where the caller can pass in the type of the dynamic data object they wish the entry data returned to be mapped to. This newly created data object will then be returned in the ZomeFunctionCallBackEventArgs.Entry.EntryDataObject property.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public ZomeFunctionCallBackEventArgs CallZomeFunction(string id, string zome, string function, ZomeFunctionCallBack callback, object paramsObject, bool matchIdToZomeFuncInCallback = true, bool cachReturnData = false, Type entryDataObjectTypeReturnedFromZome = null, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            _entryDataObjectTypeLookup[id] = entryDataObjectTypeReturnedFromZome;
            return CallZomeFunction(id, zome, function, callback, paramsObject, matchIdToZomeFuncInCallback, cachReturnData, zomeResultCallBackMode);
        }

        /// <summary>
        /// This is the main method you will be using to invoke zome functions on your given zome. It has a number of handy overloads making it easier and more powerful to call your zome functions and manage the returned data. This method raises the OnDataReceived event and then either the OnZomeFunctionCallBack or OnSignalCallBack event once it has received a response from the Holochain conductor.       
        /// </summary>
        /// <param name="id">The unique id you wish to assign for this call. This id is then returned in OnDataReceived, OnZomeFunctionCallBack and OnSignalCallBack events. There are overloads that omit this param, use these overloads if you wish HoloNET to auto-generate and manage the id's for you.</param>
        /// <param name="zome">The name of the zome you wish to target.</param>
        /// <param name="function">The name of the zome function you wish to call.</param>
        /// <param name="callback">A delegate to call once the zome function returns. This delegate contains the same signature as the one used for the OnZomeFunctionCallBack event. (optional param).</param>
        /// <param name="paramsObject">A basic CLR object containing the params the zome function is expecting (optional param).</param>
        /// <param name="matchIdToZomeFuncInCallback">This is an optional param, which defaults to true. Set this to true if you wish HoloNET to give the zome and zome function that made the call in the callback/event. If this is false then only the id will be given in the callback. This uses a small internal cache to match up the id to the given zome/function. Set this to false if you wish to save a tiny amount of memory by not utilizing this cache. If it is false then the `Zome` and `ZomeFunction` params will be missing in the ZomeCallBack, you will need to manually match the `id` to the call yourself. NOTE: If Config.EnforceRequestToResponseIdMatchingBehaviour is set to Warn or Error then this param will be ignored and the matching will always occur.</param>
        /// <param name="cachReturnData">This is an optional param, which defaults to false. Set this to true if you wish HoloNET to cache the response retrieved from holochain. Subsequent calls will return this cached data rather than calling the Holochain conductor again. Use this for static data that is not going to change for performance gains.</param>
        /// <param name="zomeResultCallBackMode">This is an optional param, where the caller can choose whether to wait for the Holochain Conductor response before returning to the caller or to return immediately once the request has been sent to the Holochain Conductor and then raise the OnDataReceived and then the OnZomeFunctionCallBack or OnSignalsCallBack events depending on the type of request sent to the Holochain Conductor.</param>
        /// <returns></returns>
        public ZomeFunctionCallBackEventArgs CallZomeFunction(string id, string zome, string function, ZomeFunctionCallBack callback, object paramsObject, bool matchIdToZomeFuncInCallback = true, bool cachReturnData = false, ConductorResponseCallBackMode zomeResultCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
        {
            return CallZomeFunctionAsync(id, zome, function, callback, paramsObject, matchIdToZomeFuncInCallback, cachReturnData, zomeResultCallBackMode).Result;
        }

        public async Task<AdminSigningCredentialsAuthorizedEventArgs> AdminAuthorizeSigningCredentialsAsync(GrantedFunctionsType grantedFunctionsType, List<(string, string)> functions = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = "")
        {
            return await AdminAuthorizeSigningCredentialsAsync(await GetCellIdAsync(), grantedFunctionsType, functions, conductorResponseCallBackMode, id);
        }

        public AdminSigningCredentialsAuthorizedEventArgs AdminAuthorizeSigningCredentials(GrantedFunctionsType grantedFunctionsType, List<(string, string)> functions = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = "")
        {
            return AdminAuthorizeSigningCredentials(GetCellId(), grantedFunctionsType, functions, id);
        }

        public async Task<AdminSigningCredentialsAuthorizedEventArgs> AdminAuthorizeSigningCredentialsAsync(string AgentPubKey, string DnaHash, GrantedFunctionsType grantedFunctionsType, List<(string, string)> functions= null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = "")
        {
            return await AdminAuthorizeSigningCredentialsAsync(GetCellId(DnaHash, AgentPubKey), grantedFunctionsType, functions, conductorResponseCallBackMode, id);
        }

        public AdminSigningCredentialsAuthorizedEventArgs AdminAuthorizeSigningCredentials(string AgentPubKey, string DnaHash, GrantedFunctionsType grantedFunctionsType, List<(string, string)> functions = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = "")
        {
            return AdminAuthorizeSigningCredentials(GetCellId(DnaHash, AgentPubKey), grantedFunctionsType, functions, id);
        }

        public async Task<AdminSigningCredentialsAuthorizedEventArgs> AdminAuthorizeSigningCredentialsAsync(byte[][] cellId, GrantedFunctionsType grantedFunctionsType, List<(string, string)> functions = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = "")
        {
            (AdminSigningCredentialsAuthorizedEventArgs args, Dictionary<GrantedFunctionsType, List<(string, string)>> grantedFunctions, byte[] signingKey) = AuthorizeSigningCredentials(cellId, grantedFunctionsType, functions, id);

            if (!args.IsError)
            {
                return await CallAdminFunctionAsync("grant_zome_call_capability", new GrantZomeCallCapabilityRequest()
                {
                    cell_id = cellId,
                    cap_grant = new ZomeCallCapGrant()
                    {
                        tag = "zome-call-signing-key",
                        functions = grantedFunctions,
                        access = new CapGrantAccess()
                        {
                            Assigned = new CapGrantAccessAssigned()
                            {
                                secret = _signingCredentialsForCell[cellId].CapSecret,
                                assignees = signingKey
                            }
                        }
                    }
                },
                _taskCompletionAdminSigningCredentialsAuthorizedCallBack, "OnAdminSigningCredentialsAuthorized", conductorResponseCallBackMode, id);
            }
            else
                return args;
        }

        public AdminSigningCredentialsAuthorizedEventArgs AdminAuthorizeSigningCredentials(byte[][] cellId, GrantedFunctionsType grantedFunctionsType, List<(string, string)> functions = null, string id = "")
        {
            (AdminSigningCredentialsAuthorizedEventArgs args, Dictionary<GrantedFunctionsType, List<(string, string)>> grantedFunctions, byte[] signingKey) = AuthorizeSigningCredentials(cellId, grantedFunctionsType, functions, id);

            if (!args.IsError)
            {
                CallAdminFunction("grant_zome_call_capability", new GrantZomeCallCapabilityRequest()
                {
                    cell_id = cellId,
                    cap_grant = new ZomeCallCapGrant()
                    {
                        tag = "zome-call-signing-key",
                        functions = grantedFunctions,
                        access = new CapGrantAccess()
                        {
                            Assigned = new CapGrantAccessAssigned()
                            {
                                secret = _signingCredentialsForCell[cellId].CapSecret,
                                assignees = signingKey
                            }
                        }
                    }
                }, id);

                return new AdminSigningCredentialsAuthorizedEventArgs() { IsCallSuccessful = true, EndPoint = EndPoint, Id = id, Message = "The call has been sent to the conductor.  Please wait for the event 'OnAdminAgentPubKeyGeneratedCallBack' to view the response." };
            }
            else
                return args;
        }

        private (AdminSigningCredentialsAuthorizedEventArgs, Dictionary<GrantedFunctionsType, List<(string, string)>>, byte[]) AuthorizeSigningCredentials(byte[][] cellId, GrantedFunctionsType grantedFunctionsType, List<(string, string)> functions = null, string id = "")
        {
            var seed = RandomNumberGenerator.GetBytes(32);
            byte[] privateKey;
            byte[] publicKey;

            if (cellId == null)
            {
                string msg = "Error occured in AuthorizeSigningCredentialsAsync function. cellId is null.";
                HandleError(msg, null);
                return (new AdminSigningCredentialsAuthorizedEventArgs() { IsError = true, EndPoint = EndPoint, Id = id, Message = msg }, null, null);
            }

            if (string.IsNullOrEmpty(Config.AgentPubKey))
            {
                string msg = "Error occured in AuthorizeSigningCredentialsAsync function. Config.AgentPubKey is null. Please set or call AdminGenerateAgentPubKey method.";
                HandleError(msg, null);
                return (new AdminSigningCredentialsAuthorizedEventArgs() { IsError = true, EndPoint = EndPoint, Id = id, Message = msg }, null, null);
            }

            if (grantedFunctionsType == GrantedFunctionsType.Listed && functions == null)
            {
                string msg = "Error occured in AuthorizeSigningCredentialsAsync function. GrantedFunctionsType was set to Listed but no functions were passed in.";
                HandleError(msg, null);
                return (new AdminSigningCredentialsAuthorizedEventArgs() { IsError = true, EndPoint = EndPoint, Id = id, Message = msg }, null, null);
            }

            //Ed25519.KeyPairFromSeed(out publicKey, out privateKey, seed);

            Sodium.KeyPair pair = Sodium.PublicKeyAuth.GenerateKeyPair(seed);

            //NSec.Cryptography.Ed25519

            byte[] DHTLocation = ConvertHoloHashToBytes(Config.AgentPubKey).TakeLast(4).ToArray();
            byte[] signingKey = new byte[] { 132, 32, 36 }.Concat(pair.PublicKey).Concat(DHTLocation).ToArray();

            //GrantedFunctions grantedFunction = new GrantedFunctions();
            //grantedFunction.Functions.Add(grantedFunctionsType, functions);

            Dictionary<GrantedFunctionsType, List<(string, string)>>  grantedFunctions = new Dictionary<GrantedFunctionsType, List<(string, string)>>();

            if (grantedFunctionsType == GrantedFunctionsType.All)
                grantedFunctions[GrantedFunctionsType.All] = null;
            else
                grantedFunctions[GrantedFunctionsType.Listed] = functions;

            _signingCredentialsForCell[cellId] = new SigningCredentials()
            {
                CapSecret = RandomNumberGenerator.GetBytes(64),
                KeyPair = new KeyPair() { PrivateKey = pair.PrivateKey, PublicKey = pair.PublicKey },
                SigningKey = signingKey
            };

            return (new AdminSigningCredentialsAuthorizedEventArgs(), grantedFunctions, signingKey);
        }


        public async Task<AdminAgentPubKeyGeneratedCallBackEventArgs> AdminGenerateAgentPubKeyAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, bool updateAgentPubKeyInConfig = true, string id = "")
        {
            _updateDnaHashAndAgentPubKey = updateAgentPubKeyInConfig;
            return await CallAdminFunctionAsync("generate_agent_pub_key", null, _taskCompletionAdminAgentPubKeyGeneratedCallBack, "OnAdminAgentPubKeyGeneratedCallBack", conductorResponseCallBackMode, id);
        }

        public AdminAgentPubKeyGeneratedCallBackEventArgs AdminGenerateAgentPubKey(bool updateAgentPubKeyInConfig = true, string id = "")
        {
            return AdminGenerateAgentPubKeyAsync(ConductorResponseCallBackMode.UseCallBackEvents, updateAgentPubKeyInConfig, id).Result;
        }

        public async Task<AdminAppInstalledCallBackEventArgs> AdminInstallAppAsync(string installedAppId, string hAppPath, string agentKey = null, Dictionary<string, byte[]> membraneProofs = null, string network_seed = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await AdminInstallAppAsync(installedAppId, hAppPath, null, agentKey, membraneProofs, network_seed, conductorResponseCallBackMode, id);
        }

        public void AdminInstallApp(string agentKey, string installedAppId, string hAppPath, Dictionary<string, byte[]> membraneProofs = null, string network_seed = null, string id = null)
        {
            AdminInstallApp(agentKey, installedAppId, hAppPath, null, membraneProofs, network_seed, id);
        }

        public async Task<AdminAppInstalledCallBackEventArgs> AdminInstallAppAsync(string installedAppId, AppBundle appBundle, string agentKey = null, Dictionary<string, byte[]> membraneProofs = null, string network_seed = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await AdminInstallAppAsync(installedAppId, null, appBundle, agentKey, membraneProofs, network_seed, conductorResponseCallBackMode, id);
        }

        public AdminAppInstalledCallBackEventArgs AdminInstallApp(string installedAppId, AppBundle appBundle, string agentKey = null, Dictionary<string, byte[]> membraneProofs = null, string network_seed = null, string id = null)
        {
            return AdminInstallAppAsync(installedAppId, null, appBundle, agentKey, membraneProofs, network_seed, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public async Task<AdminAppEnabledCallBackEventArgs> AdminEnableAppAsync(string installedAppId, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallAdminFunctionAsync("enable_app", new EnableAppRequest()
            {
                installed_app_id = installedAppId
            }, _taskCompletionAdminAppEnabledCallBack, "OnAdminAppEnabledCallBack", conductorResponseCallBackMode, id);
        }

        public AdminAppEnabledCallBackEventArgs AdminEnablelApp(string installedAppId, string id = null)
        {
            return AdminEnableAppAsync(installedAppId, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public async Task<AdminAppDisabledCallBackEventArgs> AdminDisableAppAsync(string installedAppId, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallAdminFunctionAsync("disable_app", new EnableAppRequest()
            {
                installed_app_id = installedAppId
            }, _taskCompletionAdminAppDisabledCallBack, "OnAdminAppDisabledCallBack", conductorResponseCallBackMode, id);
        }

        public AdminAppDisabledCallBackEventArgs AdminDisableApp(string installedAppId, string id = null)
        {
            return AdminDisableAppAsync(installedAppId, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public async Task<AdminAppInterfaceAttachedCallBackEventArgs> AdminAttachAppInterfaceAsync(int port, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallAdminFunctionAsync("register_dna", new AttachAppInterfaceRequest()
            {
                port = port
            }, _taskCompletionAdminAppInterfaceAttachedCallBack, "OnAdminAppInterfaceAttachedCallBack", conductorResponseCallBackMode, id);
        }

        public AdminAppInterfaceAttachedCallBackEventArgs AdminAttachAppInterface(int port, string id = null)
        {
            return AdminAttachAppInterfaceAsync(port, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public async Task<AdminRegisterDnaCallBackEventArgs> AdminRegisterDnaAsync(string path, string network_seed = null, object properties = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await AdminRegisterDnaAsync(path, null, null, network_seed, properties, conductorResponseCallBackMode, id);
        }

        public AdminRegisterDnaCallBackEventArgs AdminRegisterDna(string path, string network_seed = null, object properties = null, string id = null)
        {
            return AdminRegisterDnaAsync(path, network_seed, properties, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public async Task<AdminRegisterDnaCallBackEventArgs> AdminRegisterDnaAsync(byte[] hash, string network_seed = null, object properties = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await AdminRegisterDnaAsync(null, null, hash, network_seed, properties, conductorResponseCallBackMode, id);
        }

        public AdminRegisterDnaCallBackEventArgs AdminRegisterDna(byte[] hash, string network_seed = null, object properties = null, string id = null)
        {
            return AdminRegisterDnaAsync(hash, network_seed, properties, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public async Task<AdminRegisterDnaCallBackEventArgs> AdminRegisterDnaAsync(DnaBundle bundle, string network_seed = null, object properties = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await AdminRegisterDnaAsync(null, bundle, null, network_seed, properties, conductorResponseCallBackMode, id);
        }

        public AdminRegisterDnaCallBackEventArgs AdminRegisterDna(DnaBundle bundle, string network_seed = null, object properties = null, string id = null)
        {
            return AdminRegisterDnaAsync(bundle, network_seed, properties, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public async Task<AdminListAppsCallBackEventArgs> AdminListAppsAsync(AppStatusFilter appStatusFilter, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallAdminFunctionAsync("list_apps", new ListAppsRequest()
            {
                status_filter = appStatusFilter
            }, _taskCompletionAdminListAppsCallBack, "OnAdminListAppsCallBack", conductorResponseCallBackMode, id);
        }

        public AdminListAppsCallBackEventArgs AdminListApps(AppStatusFilter appStatusFilter, string id = null)
        {
            return AdminListAppsAsync(appStatusFilter, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public async Task<AdminListDnasCallBackEventArgs> AdminListDnasAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallAdminFunctionAsync("list_dnas", null, _taskCompletionAdminListDnasCallBack, "OnAdminListDnasCallBack", conductorResponseCallBackMode, id);
        }

        public AdminListDnasCallBackEventArgs AdminListDnas(string id = null)
        {
            return AdminListDnasAsync(ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public async Task<AdminListCellIdsCallBackEventArgs> AdminListCellIdsAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallAdminFunctionAsync("list_cell_ids", null, _taskCompletionAdminListCellIdsCallBack, "OnAdminListCellIds", conductorResponseCallBackMode, id);
        }

        public AdminListCellIdsCallBackEventArgs AdminListCellIds(string id = null)
        {
            return AdminListCellIdsAsync(ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public async Task<AdminListAppInterfacesCallBackEventArgs> AdminListInterfacesAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallAdminFunctionAsync("list_app_interfaces", null, _taskCompletionAdminListAppInterfacesCallBack, "OnAdminListAppInterfacesCallBack", conductorResponseCallBackMode, id);
        }

        public AdminListAppInterfacesCallBackEventArgs AdminListInterfaces(string id = null)
        {
            return AdminListInterfacesAsync(ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Dump the full state of the specified cell, including its chain and DHT shard, as JSON.
        /// </summary>
        /// <param name="cellId">The cell id to dump the full state for.</param>
        /// <param name="dHTOpsCursor">The last seen DhtOp RowId, returned in the full dump state. Only DhtOps with RowId greater than the cursor will be returned.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AdminDumpFullStateCallBackEventArgs> AdminDumpFullStateAsync(byte[][] cellId, int? dHTOpsCursor = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallAdminFunctionAsync("dump_full_state", new DumpFullStateRequest()
            {
                cell_id = cellId,
                dht_ops_cursor = dHTOpsCursor
            }, _taskCompletionAdminDumpFullStateCallBack, "OnAdminDumpFullStateCallBack", conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Dump the full state of the specified cell, including its chain and DHT shard, as JSON.
        /// </summary>
        /// <param name="cellId">The cell id to dump the full state for.</param>
        /// <param name="dHTOpsCursor">The last seen DhtOp RowId, returned in the full dump state. Only DhtOps with RowId greater than the cursor will be returned.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AdminDumpFullStateCallBackEventArgs AdminDumpFullState(byte[][] cellId, int? dHTOpsCursor = null, string id = null)
        {
            return AdminDumpFullStateAsync(cellId, dHTOpsCursor, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Dump the full state of the specified cell, including its chain and DHT shard, as JSON.
        /// </summary>
        /// <param name="agentPubKey">The AgentPubKey for the cell to dump the full state for.</param>
        /// <param name="dnaHash">The DnaHash for the cell to dump the full state for.</param>
        /// <param name="dHTOpsCursor">The last seen DhtOp RowId, returned in the full dump state. Only DhtOps with RowId greater than the cursor will be returned.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AdminDumpFullStateCallBackEventArgs> AdminDumpFullStateAsync(string agentPubKey, string dnaHash, int? dHTOpsCursor = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await AdminDumpFullStateAsync(GetCellId(dnaHash, agentPubKey), dHTOpsCursor, conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Dump the full state of the specified cell, including its chain and DHT shard, as JSON.
        /// </summary>
        /// <param name="agentPubKey">The AgentPubKey for the cell to dump the full state for.</param>
        /// <param name="dnaHash">The DnaHash for the cell to dump the full state for.</param>
        /// <param name="dHTOpsCursor">The last seen DhtOp RowId, returned in the full dump state. Only DhtOps with RowId greater than the cursor will be returned.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AdminDumpFullStateCallBackEventArgs AdminDumpFullState(string agentPubKey, string dnaHash, int? dHTOpsCursor = null, string id = null)
        {
            return AdminDumpFullStateAsync(agentPubKey, dnaHash, dHTOpsCursor, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Dump the full state of the specified cell, including its chain and DHT shard, as JSON. This will dump the state for the current AgentPubKey/DnaHash stored in Config.AgentPubKey & Config.DnaHash. If there it is not stored in the Config it will automatically generate one for you and retrieve from the conductor.
        /// </summary>
        /// <param name="dHTOpsCursor">The last seen DhtOp RowId, returned in the full dump state. Only DhtOps with RowId greater than the cursor will be returned.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AdminDumpFullStateCallBackEventArgs> AdminDumpFullStateAsync(int? dHTOpsCursor = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await AdminDumpFullStateAsync(await GetCellIdAsync(), dHTOpsCursor, conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Dump the full state of the specified cell, including its chain and DHT shard, as JSON. This will dump the state for the current AgentPubKey/DnaHash stored in Config.AgentPubKey & Config.DnaHash.
        /// </summary>
        /// <param name="dHTOpsCursor">The last seen DhtOp RowId, returned in the full dump state. Only DhtOps with RowId greater than the cursor will be returned.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AdminDumpFullStateCallBackEventArgs AdminDumpFullState(int? dHTOpsCursor = null, string id = null)
        {
            return AdminDumpFullStateAsync(dHTOpsCursor, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Dump the state of the specified cell, including its source chain, as JSON.
        /// </summary>
        /// <param name="cellId">The cell id to dump the full state for.</param>
        /// <param name="dHTOpsCursor">The last seen DhtOp RowId, returned in the full dump state. Only DhtOps with RowId greater than the cursor will be returned.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AdminDumpStateCallBackEventArgs> AdminDumpStateAsync(byte[][] cellId, int? dHTOpsCursor = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallAdminFunctionAsync("dump_state", new DumpStateRequest()
            {
                cell_id = cellId
            }, _taskCompletionAdminDumpStateCallBack, "OnAdminDumpStateCallBack", conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Dump the state of the specified cell, including its source chain, as JSON.
        /// </summary>
        /// <param name="cellId">The cell id to dump the full state for.</param>
        /// <param name="dHTOpsCursor">The last seen DhtOp RowId, returned in the full dump state. Only DhtOps with RowId greater than the cursor will be returned.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AdminDumpStateCallBackEventArgs AdminDumpState(byte[][] cellId, int? dHTOpsCursor = null, string id = null)
        {
            return AdminDumpStateAsync(cellId, dHTOpsCursor, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Dump the state of the specified cell, including its source chain, as JSON.
        /// </summary>
        /// <param name="agentPubKey">The AgentPubKey for the cell to dump the full state for.</param>
        /// <param name="dnaHash">The DnaHash for the cell to dump the full state for.</param>
        /// <param name="dHTOpsCursor"></param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AdminDumpStateCallBackEventArgs> AdminDumpStateAsync(string agentPubKey, string dnaHash, int? dHTOpsCursor = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await AdminDumpStateAsync(GetCellId(dnaHash, agentPubKey), dHTOpsCursor, conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Dump the state of the specified cell, including its source chain, as JSON.
        /// </summary>
        /// <param name="agentPubKey">The AgentPubKey for the cell to dump the full state for.</param>
        /// <param name="dnaHash">The DnaHash for the cell to dump the full state for.</param>
        /// <param name="dHTOpsCursor"></param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AdminDumpStateCallBackEventArgs AdminDumpState(string agentPubKey, string dnaHash, int? dHTOpsCursor = null, string id = null)
        {
            return AdminDumpStateAsync(agentPubKey, dnaHash, dHTOpsCursor, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Dump the state of the specified cell, including its source chain, as JSON. This will dump the state for the current AgentPubKey/DnaHash stored in Config.AgentPubKey & Config.DnaHash.
        /// </summary>
        /// <param name="dHTOpsCursor"></param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AdminDumpStateCallBackEventArgs> AdminDumpStateAsync(int? dHTOpsCursor = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await AdminDumpStateAsync(await GetCellIdAsync(), dHTOpsCursor, conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Dump the state of the specified cell, including its source chain, as JSON. This will dump the state for the current AgentPubKey/DnaHash stored in Config.AgentPubKey & Config.DnaHash.
        /// </summary>
        /// <param name="dHTOpsCursor"></param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AdminDumpStateCallBackEventArgs AdminDumpState(int? dHTOpsCursor = null, string id = null)
        {
            return AdminDumpStateAsync(dHTOpsCursor, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }


        /// <summary>
        /// Get the DNA definition for the specified DNA hash.
        /// </summary>
        /// <param name="dnaHash">The hash of the dna.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AdminGetDnaDefinitionCallBackEventArgs> AdminGetDnaDefinitionAsync(byte[] dnaHash, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallAdminFunctionAsync("get_dna_definition", new UpdateCoordinatorsRequest()
            {
                dnaHash = dnaHash
            }, _taskCompletionAdminGetDnaDefinitionCallBack, "OnAdminGetDnaDefinitionCallBack", conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Get the DNA definition for the specified DNA hash.
        /// </summary>
        /// <param name="dnaHash">The hash of the dna.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AdminGetDnaDefinitionCallBackEventArgs AdminGetDnaDefinition(byte[] dnaHash, string id = null)
        {
            return AdminGetDnaDefinitionAsync(dnaHash, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Get the DNA definition for the specified DNA hash.
        /// </summary>
        /// <param name="dnaHash">The hash of the dna.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AdminGetDnaDefinitionCallBackEventArgs> AdminGetDnaDefinitionAsync(string dnaHash, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await AdminGetDnaDefinitionAsync(ConvertHoloHashToBytes(dnaHash), conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Get the DNA definition for the specified DNA hash.
        /// </summary>
        /// <param name="dnaHash">The hash of the dna.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AdminGetDnaDefinitionCallBackEventArgs AdminGetDnaDefinition(string dnaHash, string id = null)
        {
            return AdminGetDnaDefinitionAsync(dnaHash, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Update the coordinataor zomes.
        /// </summary>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AdminUpdateCoordinatorsCallBackEventArgs> AdminUpdateCoordinatorsAsync(byte[] dnaHash, string path, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
           return await AdminUpdateCoordinatorsAsync(dnaHash, path, null, conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Update the coordinataor zomes.
        /// </summary>
        /// <param name="dnaHash"></param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AdminUpdateCoordinatorsCallBackEventArgs AdminUpdateCoordinators(byte[] dnaHash, string path, string id = null)
        {
            return AdminUpdateCoordinatorsAsync(dnaHash, path, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Update the coordinataor zomes.
        /// </summary>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AdminUpdateCoordinatorsCallBackEventArgs> AdminUpdateCoordinatorsAsync(string dnaHash, string path, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await AdminUpdateCoordinatorsAsync(ConvertHoloHashToBytes(dnaHash), path, null, conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Update the coordinataor zomes.
        /// </summary>
        /// <param name="dnaHash"></param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AdminUpdateCoordinatorsCallBackEventArgs AdminUpdateCoordinators(string dnaHash, string path, string id = null)
        {
            return AdminUpdateCoordinatorsAsync(dnaHash, path, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Update the coordinataor zomes.
        /// </summary>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AdminUpdateCoordinatorsCallBackEventArgs> AdminUpdateCoordinatorsAsync(byte[] dnaHash, CoordinatorBundle bundle, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await AdminUpdateCoordinatorsAsync(dnaHash, null, bundle, conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Update the coordinataor zomes.
        /// </summary>
        /// <param name="dnaHash"></param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AdminUpdateCoordinatorsCallBackEventArgs AdminUpdateCoordinators(byte[] dnaHash, CoordinatorBundle bundle, string id = null)
        {
            return AdminUpdateCoordinatorsAsync(dnaHash, bundle, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// Update the coordinataor zomes.
        /// </summary>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AdminUpdateCoordinatorsCallBackEventArgs> AdminUpdateCoordinatorsAsync(string dnaHash, CoordinatorBundle bundle, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await AdminUpdateCoordinatorsAsync(ConvertHoloHashToBytes(dnaHash), null, bundle, conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Update the coordinataor zomes.
        /// </summary>
        /// <param name="dnaHash"></param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AdminUpdateCoordinatorsCallBackEventArgs AdminUpdateCoordinators(string dnaHash, CoordinatorBundle bundle, string id = null)
        {
            return AdminUpdateCoordinatorsAsync(dnaHash, bundle, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }


        /// <summary>
        /// Request all available info about an agent.
        /// </summary>
        /// <param name="cellId">The cell id to retrive the angent info for.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AdminGetAgentInfoCallBackEventArgs> AdminGetAgentInfoAsync(byte[][] cellId, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallAdminFunctionAsync("agent_info", new GetAgentInfoRequest()
            {
                cell_id = cellId
            }, _taskCompletionAdminGetAgentInfoCallBack, "OnAdminGetAgentInfoCallBack", conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Request all available info about an agent.
        /// </summary>
        /// <param name="cellId">The cell id to retrive the angent info for.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AdminGetAgentInfoCallBackEventArgs AdminGetAgentInfo(byte[][] cellId, string id = null)
        {
            return AdminGetAgentInfoAsync(cellId, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Request all available info about an agent.
        /// </summary>
        /// <param name="agentPubKey">The AgentPubKey for the cell to dump the full state for.</param>
        /// <param name="dnaHash">The DnaHash for the cell to dump the full state for.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AdminGetAgentInfoCallBackEventArgs> AdminGetAgentInfoAsync(string agentPubKey, string dnaHash, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await AdminGetAgentInfoAsync(GetCellId(dnaHash, agentPubKey), conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Request all available info about an agent.
        /// </summary>
        /// <param name="agentPubKey">The AgentPubKey for the cell to dump the full state for.</param>
        /// <param name="dnaHash">The DnaHash for the cell to dump the full state for.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AdminGetAgentInfoCallBackEventArgs AdminGetAgentInfo(string agentPubKey, string dnaHash, string id = null)
        {
            return AdminGetAgentInfoAsync(agentPubKey, dnaHash, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Request all available info about an agent. This will retreive info for the current AgentPubKey/DnaHash stored in Config.AgentPubKey & Config.DnaHash.
        /// </summary>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AdminGetAgentInfoCallBackEventArgs> AdminGetAgentInfoAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await AdminGetAgentInfoAsync(await GetCellIdAsync(), conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Request all available info about an agent. This will retreive info for the current AgentPubKey/DnaHash stored in Config.AgentPubKey & Config.DnaHash.
        /// </summary>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AdminGetAgentInfoCallBackEventArgs AdminGetAgentInfo(string id = null)
        {
            return AdminGetAgentInfoAsync(ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        ///  Add existing agent(s) to Holochain.
        /// </summary>
        /// <param name="agentInfos">The agentInfo's to add.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AdminAddAgentInfoCallBackEventArgs> AdminAddAgentInfoAsync(AgentInfo[] agentInfos, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallAdminFunctionAsync("add_agent_info", new AddAgentInfoRequest()
            {
                agent_infos = agentInfos
            }, _taskCompletionAdminAddAgentInfoCallBack, "OnAdminAddAgentInfoCallBack", conductorResponseCallBackMode, id);
        }

        /// <summary>
        ///  Add existing agent(s) to Holochain.
        /// </summary>
        /// <param name="agentInfos">The agentInfo's to add.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AdminAddAgentInfoCallBackEventArgs AdminAddAgentInfo(AgentInfo[] agentInfos, string id = null)
        {
            return AdminAddAgentInfoAsync(agentInfos, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        ///// <summary>
        ///// Delete a clone cell that was previously disabled.
        ///// </summary>
        ///// <param name="appId">The app id that the clone cell belongs to.</param>
        ///// <param name="cloneCellId"> The clone id (string/rolename) or cell id (byte[][]) of the clone cell.</param>
        ///// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        ///// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        ///// <returns></returns>
        //public async Task<AdminDeleteCloneCellCallBackEventArgs> AdminDeleteCloneCellAsync(string appId, dynamic cloneCellId, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        //{
        //    return await CallAdminFunctionAsync("delete_clone_cell", new HoloNETAdminDeleteCloneCellRequest()
        //    {
        //         app_id = appId,
        //         clone_cell_id = cloneCellId
        //    }, _taskCompletionAdminDeleteCloneCellCallBack, "OnAdminDeleteCloneCellCallBack", conductorResponseCallBackMode, id);
        //}

        ///// <summary>
        /////  Delete a clone cell that was previously disabled.
        ///// </summary>
        ///// <param name="appId">The app id that the clone cell belongs to.</param>
        ///// <param name="cloneCellId"> The clone id (string/rolename) or cell id (byte[][]) of the clone cell.</param>
        ///// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        ///// <returns></returns>
        //public AdminGetAgentInfoCallBackEventArgs AdminDeleteCloneCell(string appId, dynamic cloneCellId, string id = null)
        //{
        //    return AdminDeleteCloneCellAsync(appId, cloneCellId, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        //}


        /// <summary>
        /// Delete a clone cell that was previously disabled.
        /// </summary>
        /// <param name="appId">The app id that the clone cell belongs to.</param>
        /// <param name="roleName">The clone id (string/rolename).</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AdminDeleteCloneCellCallBackEventArgs> AdminDeleteCloneCellAsync(string appId, string roleName, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallAdminFunctionAsync("delete_clone_cell", new DeleteCloneCellRequest()
            {
                app_id = appId,
                clone_cell_id = roleName
            }, _taskCompletionAdminDeleteCloneCellCallBack, "OnAdminDeleteCloneCellCallBack", conductorResponseCallBackMode, id);
        }

        /// <summary>
        ///  Delete a clone cell that was previously disabled.
        /// </summary>
        /// <param name="appId">The app id that the clone cell belongs to.</param>
        /// <param name="cellId"> The cell id of the cloned cell.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AdminDeleteCloneCellCallBackEventArgs AdminDeleteCloneCell(string appId, string roleName, string id = null)
        {
            return AdminDeleteCloneCellAsync(appId, roleName, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Delete a clone cell that was previously disabled.
        /// </summary>
        /// <param name="appId">The app id that the clone cell belongs to.</param>
        /// <param name="cellId"> The cell id of the cloned cell.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AdminDeleteCloneCellCallBackEventArgs> AdminDeleteCloneCellAsync(string appId, byte[][] cellId, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallAdminFunctionAsync("delete_clone_cell", new DeleteCloneCellRequest()
            {
                app_id = appId,
                clone_cell_id = cellId
            }, _taskCompletionAdminDeleteCloneCellCallBack, "OnAdminDeleteCloneCellCallBack", conductorResponseCallBackMode, id);
        }

        /// <summary>
        ///  Delete a clone cell that was previously disabled.
        /// </summary>
        /// <param name="appId">The app id that the clone cell belongs to.</param>
        /// <param name="cellId"> The cell id of the cloned cell.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AdminDeleteCloneCellCallBackEventArgs AdminDeleteCloneCell(string appId, byte[][] cellId, string id = null)
        {
            return AdminDeleteCloneCellAsync(appId, cellId, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Delete a clone cell that was previously disabled.
        /// </summary>
        /// <param name="appId">The app id that the clone cell belongs to.</param>
        /// <param name="agentPubKey">The AgentPubKey for the cell.</param>
        /// <param name="dnaHash">The DnaHash for the cell.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AdminDeleteCloneCellCallBackEventArgs> AdminDeleteCloneCellAsync(string appId, string agentPubKey, string dnaHash, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await AdminDeleteCloneCellAsync(appId, GetCellId(dnaHash, agentPubKey), conductorResponseCallBackMode, id);
        }

        /// <summary>
        ///  Delete a clone cell that was previously disabled.
        /// </summary>
        /// <param name="appId">The app id that the clone cell belongs to.</param>
        /// <param name="agentPubKey">The AgentPubKey for the cell.</param>
        /// <param name="dnaHash">The DnaHash for the cell.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AdminDeleteCloneCellCallBackEventArgs AdminDeleteCloneCell(string appId, string agentPubKey, string dnaHash, string id = null)
        {
            return AdminDeleteCloneCellAsync(appId, agentPubKey, dnaHash, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Delete a clone cell that was previously disabled. This will use the current AgentPubKey/DnaHash stored in Config.AgentPubKey & Config.DnaHash.
        /// </summary>
        /// <param name="appId">The app id that the clone cell belongs to.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AdminDeleteCloneCellCallBackEventArgs> AdminDeleteCloneCellAsync(string appId, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await AdminDeleteCloneCellAsync(appId, await GetCellIdAsync(), conductorResponseCallBackMode, id);
        }

        /// <summary>
        ///  Delete a clone cell that was previously disabled. This will use the current AgentPubKey/DnaHash stored in Config.AgentPubKey & Config.DnaHash.
        /// </summary>
        /// <param name="appId">The app id that the clone cell belongs to.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AdminDeleteCloneCellCallBackEventArgs AdminDeleteCloneCell(string appId, string id = null)
        {
            return AdminDeleteCloneCellAsync(appId, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Get'the storgage info used by hApps.
        /// </summary>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AdminGetStorageInfoCallBackEventArgs> AdminGetStorageInfoAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallAdminFunctionAsync("storage_info", null, _taskCompletionAdminGetStorageInfoCallBack, "OnAdminGetStorageInfoCallBack", conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Get'the storgage info used by hApps.
        /// </summary>
        /// <param name="appId">The app id that the clone cell belongs to.</param>
        /// <param name="cloneCellId"> The clone id or cell id of the clone cell. Can be RoleName (string) or CellId (byte[][]).</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AdminGetStorageInfoCallBackEventArgs AdminGetStorageInfo(string id = null)
        {
            return AdminGetStorageInfoAsync(ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Dump the network metrics tracked by kitsune.
        /// </summary>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AdminDumpNetworkStatsCallBackEventArgs> AdminDumpNetworkStatsAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallAdminFunctionAsync("dump_network_stats", null, _taskCompletionAdminDumpNetworkStatsCallBack, "OnAdminDumpNetworkStatsCallBack", conductorResponseCallBackMode, id);
        }

        /// <summary>
        ///  Dump the network metrics tracked by kitsune.
        /// </summary>
        /// <param name="appId">The app id that the clone cell belongs to.</param>
        /// <param name="cloneCellId"> The clone id or cell id of the clone cell. Can be RoleName (string) or CellId (byte[][]).</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AdminDumpNetworkStatsCallBackEventArgs AdminDumpNetworkStats(string id = null)
        {
            return AdminDumpNetworkStatsAsync(ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Call this method to clear all of HoloNETClient's internal cache. This includes the responses that have been cached using the CallZomeFunction methods if the `cacheData` param was set to true for any of the calls.
        /// </summary>
        public void ClearCache(bool clearPendingRequsts = false)
        {
            _zomeReturnDataLookup.Clear();
            _cacheZomeReturnDataLookup.Clear();
            _dictPropertyInfos.Clear();

            if (clearPendingRequsts)
            {
                _pendingRequests.Clear();
                _zomeLookup.Clear();
                _funcLookup.Clear();
            }
        }

        /// <summary>
        /// Utility method to convert a string to base64 encoded bytes (Holochain Conductor format). This is used to convert the AgentPubKey & DnaHash when making a zome call.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public byte[] ConvertHoloHashToBytes(string hash)
        {
            try
            {
                return Convert.FromBase64String(hash.Replace('-', '+').Replace('_', '/').Substring(1, hash.Length - 1)); //also remove the u prefix.
            }
            catch(Exception e) 
            {
                return null;
            }

            //Span<byte> output = null;
            //int bytesWritten = 0;

            //if (Convert.TryFromBase64String(hash.Replace('-', '+').Replace('_', '/').Substring(1, hash.Length - 1), output, out bytesWritten)) //also remove the u prefix.
            //    return output.ToArray();
            //else
            //    return null;
        }

        /// <summary>
        /// Utility method to convert from base64 bytes (Holochain Conductor format) to a friendly C# format. This is used to convert the AgentPubKey & DnaHash retrieved from the Conductor.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public string ConvertHoloHashToString(byte[] bytes)
        {
            try
            {
                return string.Concat("u", Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_"));
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public byte[][] GetCellId(string DnaHash, string AgentPubKey)
        {
            return new byte[2][] { ConvertHoloHashToBytes(DnaHash), ConvertHoloHashToBytes(AgentPubKey) };
        }

        public async Task<byte[][]> GetCellIdAsync()
        {
            if (string.IsNullOrEmpty(Config.AgentPubKey))
            {
                if (IsAdmin)
                    await AdminGenerateAgentPubKeyAsync();
                else
                    await RetrieveAgentPubKeyAndDnaHashAsync();
            }

            if (string.IsNullOrEmpty(Config.AgentPubKey))
            {
                HandleError("Error occured in GetCellId. Config.AgentPubKey is null, please set before calling this method.", null);
                return null;
            }

            if (string.IsNullOrEmpty(Config.DnaHash))
            {
                HandleError("Error occured in GetCellId. Config.DnaHash is null, please set before calling this method.", null);
                return null;
            }

            return GetCellId(Config.DnaHash, Config.AgentPubKey);
        }

        public byte[][] GetCellId()
        {
            //TODO: Implement this in non async way...
            //if (string.IsNullOrEmpty(Config.AgentPubKey))
            //{
            //    if (IsAdmin)
            //        await AdminGenerateAgentPubKeyAsync();
            //    else
            //        await RetrieveAgentPubKeyAndDnaHashAsync();
            //}

            if (string.IsNullOrEmpty(Config.AgentPubKey))
            {
                HandleError("Error occured in GetCellId. Config.AgentPubKey is null, please set before calling this method.", null);
                return null;
            }

            if (string.IsNullOrEmpty(Config.DnaHash))
            {
                HandleError("Error occured in GetCellId. Config.DnaHash is null, please set before calling this method.", null);
                return null;
            }

            return GetCellId(Config.DnaHash, Config.AgentPubKey);
        }

        /// <summary>
        /// This method maps the data returned from the Conductor zome call onto a dynamic data object passed into the CallZomeFunction method. Alternatively the type of the data object can be passed in, for which an instance of it will be created. Either way the now mapped and populated data object is then returned in the `ZomeFunctionCallBackEventArgs.EntryData.EntryDataObject` property during the OnZomeFunctionCallBack event. Please see OnZomeFunctionCallBack for more info. This method is called internally but can also be called manually and is used by the HoloNETEntryBaseClass and HoloNETAuditEntryBaseClass.
        /// </summary>
        /// <param name="entryDataObjectType">The type of the data object to map the KeyValuePairs returned from the Holochain Conductor onto.</param>
        /// <param name="keyValuePairs">The KeyValuePairs returned from the Holochain Conductor (after they have been decoded by an internal function called `DecodeRawZomeData`) that will be mapped onto the data object.</param>
        /// <param name="cacheEntryDataObjectPropertyInfo">Set this to true if you want HoloNET to cache the property info for the Entry Data Object passed in (this can reduce the slight overhead used by reflection).</param>
        /// <returns></returns>
        public async Task<dynamic> MapEntryDataObjectAsync(Type entryDataObjectType, Dictionary<string, string> keyValuePairs, bool cacheEntryDataObjectPropertyInfo = true)
        {
            return await MapEntryDataObjectAsync(Activator.CreateInstance(entryDataObjectType), keyValuePairs, cacheEntryDataObjectPropertyInfo);
        }

        /// <summary>
        /// This method maps the data returned from the Conductor zome call onto a dynamic data object passed into the CallZomeFunction method. Alternatively the type of the data object can be passed in, for which an instance of it will be created. Either way the now mapped and populated data object is then returned in the `ZomeFunctionCallBackEventArgs.EntryData.EntryDataObject` property during the OnZomeFunctionCallBack event. Please see OnZomeFunctionCallBack for more info. This method is called internally but can also be called manually and is used by the HoloNETEntryBaseClass and HoloNETAuditEntryBaseClass.
        /// </summary>
        /// <param name="entryDataObjectType">The type of the data object to map the KeyValuePairs returned from the Holochain Conductor onto.</param>
        /// <param name="keyValuePairs">The KeyValuePairs returned from the Holochain Conductor (after they have been decoded by an internal function called `DecodeRawZomeData`) that will be mapped onto the data object.</param>
        /// <param name="cacheEntryDataObjectPropertyInfo">Set this to true if you want HoloNET to cache the property info for the Entry Data Object passed in (this can reduce the slight overhead used by reflection).</param>
        /// <returns></returns>
        public dynamic MapEntryDataObject(Type entryDataObjectType, Dictionary<string, string> keyValuePairs, bool cacheEntryDataObjectPropertyInfo = true)
        {
            return MapEntryDataObjectAsync(entryDataObjectType, keyValuePairs, cacheEntryDataObjectPropertyInfo).Result;
        }

        /// <summary>
        /// This method maps the data returned from the Conductor zome call onto a dynamic data object passed into the CallZomeFunction method. Alternatively the type of the data object can be passed in, for which an instance of it will be created. Either way the now mapped and populated data object is then returned in the `ZomeFunctionCallBackEventArgs.EntryData.EntryDataObject` property during the OnZomeFunctionCallBack event. Please see OnZomeFunctionCallBack for more info. This method is called internally but can also be called manually and is used by the HoloNETEntryBaseClass and HoloNETAuditEntryBaseClass.
        /// </summary>
        /// <param name="entryDataObject">The dynamic data object to map the KeyValuePairs returned from the Holochain Conductor onto.</param>
        /// <param name="keyValuePairs">The KeyValuePairs returned from the Holochain Conductor (after they have been decoded by an internal function called `DecodeRawZomeData`) that will be mapped onto the data object.</param>
        /// <param name="cacheEntryDataObjectPropertyInfos">Set this to true if you want HoloNET to cache the property info's for the Entry Data Object passed in (this can reduce the slight overhead used by reflection).</param>
        /// <returns></returns>
        public async Task<dynamic> MapEntryDataObjectAsync(dynamic entryDataObject, Dictionary<string, string> keyValuePairs, bool cacheEntryDataObjectPropertyInfos = true)
        {
            try
            {
                PropertyInfo[] props = null;

                if (keyValuePairs != null && entryDataObject != null)
                {
                    Type type = entryDataObject.GetType();
                    string typeKey = $"{type.AssemblyQualifiedName}.{type.FullName}";

                    if (cacheEntryDataObjectPropertyInfos && _dictPropertyInfos.ContainsKey(typeKey))
                        props = _dictPropertyInfos[typeKey];
                    else
                    {
                        //Cache the props to reduce overhead of reflection.
                        props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                        if (cacheEntryDataObjectPropertyInfos)
                            _dictPropertyInfos[typeKey] = props;
                    }

                    foreach (PropertyInfo propInfo in props)
                    {
                        foreach (CustomAttributeData data in propInfo.CustomAttributes)
                        {
                            if (data.AttributeType == (typeof(HolochainFieldName)))
                            {
                                try
                                {
                                    if (data.ConstructorArguments.Count > 0 && data.ConstructorArguments[0] != null && data.ConstructorArguments[0].Value != null)
                                    {
                                        string key = data.ConstructorArguments[0].Value.ToString();

                                        if (propInfo.PropertyType == typeof(Guid))
                                            propInfo.SetValue(entryDataObject, new Guid(keyValuePairs[key]));

                                        else if (propInfo.PropertyType == typeof(bool))
                                            propInfo.SetValue(entryDataObject, Convert.ToBoolean(keyValuePairs[key]));

                                        else if (propInfo.PropertyType == typeof(DateTime))
                                            propInfo.SetValue(entryDataObject, Convert.ToDateTime(keyValuePairs[key]));

                                        else if (propInfo.PropertyType == typeof(int))
                                            propInfo.SetValue(entryDataObject, Convert.ToInt32(keyValuePairs[key]));

                                        else if (propInfo.PropertyType == typeof(long))
                                            propInfo.SetValue(entryDataObject, Convert.ToInt64(keyValuePairs[key]));

                                        else if (propInfo.PropertyType == typeof(float))
                                            propInfo.SetValue(entryDataObject, Convert.ToDouble(keyValuePairs[key])); //TODO: Check if this is right?! :)

                                        else if (propInfo.PropertyType == typeof(double))
                                            propInfo.SetValue(entryDataObject, Convert.ToDouble(keyValuePairs[key]));

                                        else if (propInfo.PropertyType == typeof(decimal))
                                            propInfo.SetValue(entryDataObject, Convert.ToDecimal(keyValuePairs[key]));

                                        else if (propInfo.PropertyType == typeof(UInt16))
                                            propInfo.SetValue(entryDataObject, Convert.ToUInt16(keyValuePairs[key]));

                                        else if (propInfo.PropertyType == typeof(UInt32))
                                            propInfo.SetValue(entryDataObject, Convert.ToUInt32(keyValuePairs[key]));

                                        else if (propInfo.PropertyType == typeof(UInt64))
                                            propInfo.SetValue(entryDataObject, Convert.ToUInt64(keyValuePairs[key]));

                                        else if (propInfo.PropertyType == typeof(Single))
                                            propInfo.SetValue(entryDataObject, Convert.ToSingle(keyValuePairs[key]));

                                        else if (propInfo.PropertyType == typeof(char))
                                            propInfo.SetValue(entryDataObject, Convert.ToChar(keyValuePairs[key]));

                                        else if (propInfo.PropertyType == typeof(byte))
                                            propInfo.SetValue(entryDataObject, Convert.ToByte(keyValuePairs[key]));

                                        else if (propInfo.PropertyType == typeof(sbyte))
                                            propInfo.SetValue(entryDataObject, Convert.ToSByte(keyValuePairs[key]));

                                        else
                                            propInfo.SetValue(entryDataObject, keyValuePairs[key]);

                                        break;
                                    }
                                }
                                catch (Exception ex)
                                {

                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return entryDataObject;
        }

        /// <summary>
        /// This method maps the data returned from the Conductor zome call onto a dynamic data object passed into the CallZomeFunction method. Alternatively the type of the data object can be passed in, for which an instance of it will be created. Either way the now mapped and populated data object is then returned in the `ZomeFunctionCallBackEventArgs.EntryData.EntryDataObject` property during the OnZomeFunctionCallBack event. Please see OnZomeFunctionCallBack for more info. This method is called internally but can also be called manually and is used by the HoloNETEntryBaseClass and HoloNETAuditEntryBaseClass.
        /// </summary>
        /// <param name="entryDataObject">The dynamic data object to map the KeyValuePairs returned from the Holochain Conductor onto.</param>
        /// <param name="keyValuePairs">The KeyValuePairs returned from the Holochain Conductor (after they have been decoded by an internal function called `DecodeRawZomeData`) that will be mapped onto the data object.</param>
        /// <param name="cacheEntryDataObjectPropertyInfos">Set this to true if you want HoloNET to cache the property info's for the Entry Data Object passed in (this can reduce the slight overhead used by reflection).</param>
        /// <returns></returns>
        public dynamic MapEntryDataObject(dynamic entryDataObject, Dictionary<string, string> keyValuePairs, bool cacheEntryDataObjectPropertyInfos = true)
        {
            return MapEntryDataObjectAsync(entryDataObject, keyValuePairs, cacheEntryDataObjectPropertyInfos).Result;
        }

        ///// <summary>
        ///// Shutsdown all running Holochain Conductors.
        ///// </summary>
        //public HolochainConductorsShutdownEventArgs ShutDownAllHolochainConductors()
        //{
        //    return ShutDownAllHolochainConductorsAsync().Result;
        //}

        //public void Dispose()
        //{
        //    Shutdown();
        //}

        //public ValueTask DisposeAsync()
        //{
        //    if (WebSocket.State != WebSocketState.Closed || WebSocket.State != WebSocketState.CloseReceived || WebSocket.State != WebSocketState.CloseSent)
        //        await DisconnectAsync();

        //    Logger.Loggers.Clear();
        //}

        /// <summary>
        /// This method will shutdown HoloNET by first calling the Disconnect method to disconnect from the Holochain Conductor and then calling the ShutDownHolochainConductors method to shutdown any running Holochain Conductors. This method will then raise the OnHoloNETShutdown event. This method works very similar to the Disconnect method except it also clears the loggers, does any other shutdown tasks necessary and then returns a `HoloNETShutdownEventArgs` object. You can specify if HoloNET should wait until it has finished disconnecting and shutting down the conductors before returning to the caller or whether it should return immediately and then use the OnDisconnected, OnHolochainConductorsShutdownComplete & OnHoloNETShutdownComplete events to notify the caller. 
        /// </summary>
        /// <param name="disconnectedCallBackMode">If this is set to `WaitForHolochainConductorToDisconnect` (default) then it will await until it has disconnected before returning to the caller, otherwise (it is set to `UseCallBackEvents`) it will return immediately and then raise the OnDisconnected once it is disconnected.</param>
        /// <param name="shutdownHolochainConductorsMode">Once it has successfully disconnected it will automatically call the [ShutDownAllHolochainConductors](#ShutDownAllHolochainConductors) method if the `shutdownHolochainConductorsMode` flag (defaults to `UseConfigSettings`) is not set to `DoNotShutdownAnyConductors`. Other values it can be are 'ShutdownCurrentConductorOnly' or 'ShutdownAllConductors'. Please see the [ShutDownConductors](#ShutDownConductors) method below for more detail.</param>
        /// <returns></returns>
        public async Task<HoloNETShutdownEventArgs> ShutdownHoloNETAsync(DisconnectedCallBackMode disconnectedCallBackMode = DisconnectedCallBackMode.WaitForHolochainConductorToDisconnect, ShutdownHolochainConductorsMode shutdownHolochainConductorsMode = ShutdownHolochainConductorsMode.UseConfigSettings)
        {
            _shuttingDownHoloNET = true;

            if (WebSocket.State != WebSocketState.Closed || WebSocket.State != WebSocketState.CloseReceived || WebSocket.State != WebSocketState.CloseSent)
                await DisconnectAsync(disconnectedCallBackMode, shutdownHolochainConductorsMode);

            if (disconnectedCallBackMode == DisconnectedCallBackMode.WaitForHolochainConductorToDisconnect)
                return await _taskCompletionHoloNETShutdown.Task;
            else
                return new HoloNETShutdownEventArgs(EndPoint, Config.DnaHash, Config.AgentPubKey, null);
        }

        /// <summary>
        /// This method will shutdown HoloNET by first calling the Disconnect method to disconnect from the Holochain Conductor and then calling the ShutDownHolochainConductors method to shutdown any running Holochain Conductors. This method will then raise the OnHoloNETShutdown event. This method works very similar to the Disconnect method except it also clears the loggers, does any other shutdown tasks necessary and then returns a `HoloNETShutdownEventArgs` object. You can specify if HoloNET should wait until it has finished disconnecting and shutting down the conductors before returning to the caller or whether it should return immediately and then use the OnDisconnected, OnHolochainConductorsShutdownComplete & OnHoloNETShutdownComplete events to notify the caller. 
        /// </summary>
        /// <param name="shutdownHolochainConductorsMode">Once it has successfully disconnected it will automatically call the [ShutDownAllHolochainConductors](#ShutDownAllHolochainConductors) method if the `shutdownHolochainConductorsMode` flag (defaults to `UseConfigSettings`) is not set to `DoNotShutdownAnyConductors`. Other values it can be are 'ShutdownCurrentConductorOnly' or 'ShutdownAllConductors'. Please see the [ShutDownConductors](#ShutDownConductors) method below for more detail.</param>
        /// <returns></returns>
        public HoloNETShutdownEventArgs ShutdownHoloNET(ShutdownHolochainConductorsMode shutdownHolochainConductorsMode = ShutdownHolochainConductorsMode.UseConfigSettings)
        {
            _shuttingDownHoloNET = true;

            if (WebSocket.State != WebSocketState.Closed || WebSocket.State != WebSocketState.CloseReceived || WebSocket.State != WebSocketState.CloseSent)
                Disconnect(DisconnectedCallBackMode.UseCallBackEvents, shutdownHolochainConductorsMode);

            return new HoloNETShutdownEventArgs(EndPoint, Config.DnaHash, Config.AgentPubKey, null);
        }

        /// <summary>
        /// Will automatically shutdown the current Holochain Conductor (if the `shutdownHolochainConductorsMode` param is set to `ShutdownCurrentConductorOnly`) or all Holochain Conductors (if the `shutdownHolochainConductorsMode` param is set to `ShutdownAllConductors`). If the `shutdownHolochainConductorsMode` param is set to `UseConfigSettings` then it will use the `HoloNETClient.Config.AutoShutdownHolochainConductor` and `HoloNETClient.Config.ShutDownALLHolochainConductors` flags to determine which mode to use. The Disconnect method will automatically call this once it has finished disconnecting from the Holochain Conductor. The ShutdownHoloNET will also call this method.
        /// </summary>
        /// <param name="shutdownHolochainConductorsMode">If this flag is set to `ShutdownCurrentConductorOnly` it will shutdown the currently running Holochain Conductor only. If it is set to `ShutdownAllConductors` it will shutdown all running Holochain Conductors. If it is set to `UseConfigSettings` (default) then it will use the `HoloNETClient.Config.AutoShutdownHolochainConductor` and `HoloNETClient.Config.ShutDownALLHolochainConductors` flags to determine which mode to use.</param>
        /// <returns></returns>
        public async Task<HolochainConductorsShutdownEventArgs> ShutDownHolochainConductorsAsync(ShutdownHolochainConductorsMode shutdownHolochainConductorsMode = ShutdownHolochainConductorsMode.UseConfigSettings)
        {
            HolochainConductorsShutdownEventArgs holochainConductorsShutdownEventArgs = new HolochainConductorsShutdownEventArgs();
            holochainConductorsShutdownEventArgs.AgentPubKey = Config.AgentPubKey;
            holochainConductorsShutdownEventArgs.DnaHash = Config.DnaHash;
            holochainConductorsShutdownEventArgs.EndPoint = EndPoint;

            try
            {
                // Close any conductors down if necessary.
                if ((Config.AutoShutdownHolochainConductor && _shutdownHolochainConductorsMode == ShutdownHolochainConductorsMode.UseConfigSettings)
                    || shutdownHolochainConductorsMode == ShutdownHolochainConductorsMode.ShutdownCurrentConductorOnly
                    || shutdownHolochainConductorsMode == ShutdownHolochainConductorsMode.ShutdownAllConductors)
                {
                    if ((Config.ShutDownALLHolochainConductors && _shutdownHolochainConductorsMode == ShutdownHolochainConductorsMode.UseConfigSettings)
                    || shutdownHolochainConductorsMode == ShutdownHolochainConductorsMode.ShutdownAllConductors)
                        holochainConductorsShutdownEventArgs = await ShutDownAllHolochainConductorsAsync();

                    else if (_conductorProcess != null)
                    {
                        Logger.Log("Shutting Down Holochain Conductor...", LogType.Info, true);

                        if (Config.ShowHolochainConductorWindow)
                            _conductorProcess.CloseMainWindow();

                        _conductorProcess.Kill();
                        _conductorProcess.Close();

                        // _conductorProcess.WaitForExit();
                        _conductorProcess.Dispose();

                        if (_conductorProcess.StartInfo.FileName.Contains("hc.exe"))
                            holochainConductorsShutdownEventArgs.NumberOfHcExeInstancesShutdown = 1;

                        else if (_conductorProcess.StartInfo.FileName.Contains("holochain.exe"))
                            holochainConductorsShutdownEventArgs.NumberOfHolochainExeInstancesShutdown = 1;

                        Logger.Log("Holochain Conductor Successfully Shutdown.", LogType.Info);
                    }
                }

                OnHolochainConductorsShutdownComplete?.Invoke(this, holochainConductorsShutdownEventArgs);

                if (_shuttingDownHoloNET)
                {
                    Logger.Loggers.Clear();

                    HoloNETShutdownEventArgs holoNETShutdownEventArgs = new HoloNETShutdownEventArgs(this.EndPoint, Config.DnaHash, Config.AgentPubKey, holochainConductorsShutdownEventArgs);
                    OnHoloNETShutdownComplete?.Invoke(this, holoNETShutdownEventArgs);
                    _taskCompletionHoloNETShutdown.SetResult(holoNETShutdownEventArgs);
                }
            }
            catch (Exception ex)
            {
                HandleError("Error occurred in HoloNETClient.ShutDownConductorsInternal method.", ex);
            }

            return holochainConductorsShutdownEventArgs;
        }

        /// <summary>
        /// Will automatically shutdown the current Holochain Conductor (if the `shutdownHolochainConductorsMode` param is set to `ShutdownCurrentConductorOnly`) or all Holochain Conductors (if the `shutdownHolochainConductorsMode` param is set to `ShutdownAllConductors`). If the `shutdownHolochainConductorsMode` param is set to `UseConfigSettings` then it will use the `HoloNETClient.Config.AutoShutdownHolochainConductor` and `HoloNETClient.Config.ShutDownALLHolochainConductors` flags to determine which mode to use. The Disconnect method will automatically call this once it has finished disconnecting from the Holochain Conductor. The ShutdownHoloNET will also call this method.
        /// </summary>
        /// <param name="shutdownHolochainConductorsMode">If this flag is set to `ShutdownCurrentConductorOnly` it will shutdown the currently running Holochain Conductor only. If it is set to `ShutdownAllConductors` it will shutdown all running Holochain Conductors. If it is set to `UseConfigSettings` (default) then it will use the `HoloNETClient.Config.AutoShutdownHolochainConductor` and `HoloNETClient.Config.ShutDownALLHolochainConductors` flags to determine which mode to use.</param>
        /// <returns></returns>
        public HolochainConductorsShutdownEventArgs ShutDownHolochainConductors(ShutdownHolochainConductorsMode shutdownHolochainConductorsMode = ShutdownHolochainConductorsMode.UseConfigSettings)
        {
            return ShutDownHolochainConductorsAsync(shutdownHolochainConductorsMode).Result;
        }

        private string GetRequestId()
        {
            _currentId++;
            return _currentId.ToString();
        }

        private async Task<T> CallAdminFunctionAsync<T>(string holochainConductorFunctionName, dynamic holoNETDataDetailed, Dictionary<string, TaskCompletionSource<T>> _taskCompletionCallBack, string eventCallBackName, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null) where T : HoloNETDataReceivedBaseEventArgs, new()
        {
            HoloNETData holoNETData = new HoloNETData()
            {
                type = holochainConductorFunctionName,
                data = holoNETDataDetailed
            };

            if (string.IsNullOrEmpty(id))
                id = GetRequestId();

            _taskCompletionCallBack[id] = new TaskCompletionSource<T> { };
            await SendHoloNETRequestAsync(holoNETData, id);

            if (conductorResponseCallBackMode == ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
            {
                Task<T> returnValue = _taskCompletionCallBack[id].Task;
                return await returnValue;
            }
            else
                return new T() { EndPoint = EndPoint, Id = id, Message = $"conductorResponseCallBackMode is set to UseCallBackEvents so please wait for {eventCallBackName} event for the result." };
        }

        private void CallAdminFunction(string holochainConductorFunctionName, dynamic holoNETDataDetailed, string id = null)
        {
            HoloNETData holoNETData = new HoloNETData()
            {
                type = holochainConductorFunctionName,
                data = holoNETDataDetailed
            };

            if (string.IsNullOrEmpty(id))
                id = GetRequestId();

            SendHoloNETRequest(holoNETData, id);
            //return new T() { EndPoint = EndPoint, Id = id, Message = $"conductorResponseCallBackMode is set to UseCallBackEvents so please wait for {eventCallBackName} event for the result." };
        }

        private async Task<AdminAppInstalledCallBackEventArgs> AdminInstallAppAsync(string installedAppId, string hAppPath = null, AppBundle appBundle = null, string agentKey = null, Dictionary<string, byte[]> membraneProofs = null, string network_seed = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            if (string.IsNullOrEmpty(agentKey))
            {
                if (string.IsNullOrEmpty(Config.AgentPubKey))
                    await AdminGenerateAgentPubKeyAsync();

                agentKey = Config.AgentPubKey;
            }

            if (membraneProofs == null)
                membraneProofs = new Dictionary<string, byte[]>();

            return await CallAdminFunctionAsync("install_app", new InstallAppRequest()
            {
                path = hAppPath,
                bundle = appBundle,
                agent_key = ConvertHoloHashToBytes(agentKey),
                installed_app_id = installedAppId,
                membrane_proofs = membraneProofs,
                network_seed = network_seed
            }, _taskCompletionAdminAppInstalledCallBack, "OnAdminAppInstalledCallBack", conductorResponseCallBackMode, id);
         }

        Dictionary<string, string> _installingAppId = new Dictionary<string, string>();

        private void AdminInstallApp(string agentKey, string installedAppId, string hAppPath = null, AppBundle appBundle = null, Dictionary<string, byte[]> membraneProofs = null, string network_seed = null, string id = null)
        {
            if (string.IsNullOrEmpty(agentKey))
            {
                if (string.IsNullOrEmpty(Config.AgentPubKey))
                {
                    //TODO: Later we may want to add the same functionality in the async version to automatically retreive the agentPubKey but for non async version would require a little more work to store the values passed in in a dictionary keyed by id (id would need to be generated first).

                    if (string.IsNullOrEmpty(id))
                        id = GetRequestId();

                    _installingAppId[id] = installedAppId;

                    //    //_installingApp = true;
                    //    //_installedAppId = installedAppId;
                    //    //_hAppPath = hAppPath;
                    //    //_appBundle = appBundle;
                    //    //_membraneProofs
                    //    AdminGenerateAgentPubKey();
                }

                agentKey = Config.AgentPubKey;
            }

            if (!string.IsNullOrEmpty(agentKey))
            {
                if (membraneProofs == null)
                    membraneProofs = new Dictionary<string, byte[]>();

                CallAdminFunction("install_app", new InstallAppRequest()
                {
                    path = hAppPath,
                    bundle = appBundle,
                    agent_key = ConvertHoloHashToBytes(agentKey),
                    installed_app_id = installedAppId,
                    membrane_proofs = membraneProofs,
                    network_seed = network_seed
                }, id);
            }
        }

        private async Task<AdminRegisterDnaCallBackEventArgs> AdminRegisterDnaAsync(string path, DnaBundle bundle, byte[] hash, string network_seed = null, object properties = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallAdminFunctionAsync("register_dna", new RegisterDnaRequest()
            {
                path = path,
                bundle = bundle,
                hash = hash,
                modifiers = new DnaModifiers()
                {
                    network_seed = network_seed,
                    properties = properties
                }
            }, _taskCompletionAdminRegisterDnaCallBack, "OnAdminRegisterDnaCallBack", conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Update the coordinataor zomes.
        /// </summary>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnAdminDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        private async Task<AdminUpdateCoordinatorsCallBackEventArgs> AdminUpdateCoordinatorsAsync(byte[] dnaHash, string path, CoordinatorBundle bundle, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallAdminFunctionAsync("update_coordinators", new UpdateCoordinatorsRequest()
            {
                dnaHash = dnaHash,
                path = path,
                bundle = bundle
            }, _taskCompletionAdminUpdateCoordinatorsCallBack, "OnAdminUpdateCoordinatorsCallBack", conductorResponseCallBackMode, id);
        }

        private async Task<HolochainConductorsShutdownEventArgs> ShutDownAllHolochainConductorsAsync()
        {
            HolochainConductorsShutdownEventArgs result = new HolochainConductorsShutdownEventArgs();
            result.AgentPubKey = Config.AgentPubKey;
            result.DnaHash = Config.DnaHash;
            result.EndPoint = EndPoint;

            try
            {
                Logger.Log("Shutting Down All Holochain Conductors...", LogType.Info, false);

                foreach (Process process in Process.GetProcessesByName("hc"))
                {
                    if (Config.ShowHolochainConductorWindow)
                        process.CloseMainWindow();

                    process.Kill();
                    process.Close();

                    //process.WaitForExit();
                    process.Dispose();
                    result.NumberOfHcExeInstancesShutdown++;
                }

                //conductorInfo = new FileInfo(Config.FullPathToExternalHolochainConductorBinary);
                //parts = conductorInfo.Name.Split('.');

                foreach (Process process in Process.GetProcessesByName("holochain"))
                {
                    if (Config.ShowHolochainConductorWindow)
                        process.CloseMainWindow();

                    process.Kill();
                    process.Close();

                    //process.WaitForExit();
                    process.Dispose();
                    result.NumberOfHolochainExeInstancesShutdown++;
                }

                foreach (Process process in Process.GetProcessesByName("rustc"))
                {
                    if (Config.ShowHolochainConductorWindow)
                        process.CloseMainWindow();

                    process.Kill();
                    process.Close();

                    //process.WaitForExit();
                    process.Dispose();
                    result.NumberOfRustcExeInstancesShutdown++;
                }

                Logger.Log("All Holochain Conductors Successfully Shutdown.", LogType.Info);
            }
            catch (Exception ex)
            {
                HandleError("Error occurred in HoloNETClient.ShutDownAllConductors method", ex);
            }

            return result;
        }

        private void Init(string holochainConductorURI)
        {
            try
            {
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.UnhandledException += CurrentDomain_UnhandledException;

                WebSocket = new WebSocket.WebSocket(holochainConductorURI, Logger.Loggers);

                //TODO: Impplemnt IDispoasable to unsubscribe event handlers to prevent memory leaks... 
                WebSocket.OnConnected += WebSocket_OnConnected;
                WebSocket.OnDataSent += WebSocket_OnDataSent;
                WebSocket.OnDataReceived += WebSocket_OnDataReceived;
                WebSocket.OnDisconnected += WebSocket_OnDisconnected;
                WebSocket.OnError += WebSocket_OnError;
            }
            catch (Exception ex)
            {
                HandleError("Error in HoloNETClient.Init method.", ex);
            }
        }

        private void WebSocket_OnDataSent(object sender, DataSentEventArgs e)
        {
            OnDataSent?.Invoke(this, new HoloNETDataSentEventArgs { IsCallSuccessful = e.IsCallSuccessful, EndPoint = e.EndPoint, RawBinaryData = e.RawBinaryData, RawBinaryDataAsString = e.RawBinaryDataAsString, RawBinaryDataDecoded = e.RawBinaryDataDecoded });
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleError("CurrentDomain_UnhandledException was raised.", (Exception)e.ExceptionObject);
        }

        private void WebSocket_OnConnected(object sender, ConnectedEventArgs e)
        {
            try
            {
                OnConnected?.Invoke(this, new ConnectedEventArgs { EndPoint = e.EndPoint });

                //If the AgentPubKey & DnaHash have already been retrieved from the hc sandbox command then raise the OnReadyForZomeCalls event.
                if (WebSocket.State == WebSocketState.Open && !string.IsNullOrEmpty(Config.AgentPubKey) && !string.IsNullOrEmpty(Config.DnaHash))
                    SetReadyForZomeCalls();

                //Otherwise, if the retrieveAgentPubKeyAndDnaHashFromConductor param was set to true when calling the Connect method, retrieve them now...
                else if (_getAgentPubKeyAndDnaHashFromConductor) 
                    RetrieveAgentPubKeyAndDnaHashFromConductorAsync();
            }
            catch (Exception ex)
            {
                HandleError("Error in HoloNETClient.WebSocket_OnConnected method.", ex);
            }
        }


        private void WebSocket_OnError(object sender, WebSocketErrorEventArgs e)
        {
            HandleError(e.Reason, e.ErrorDetails);
        }

        private void WebSocket_OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            try
            {
                if (_pendingRequests.Count > 0)
                {
                    string requests = "";

                    foreach (string id in _pendingRequests)
                        requests = string.Concat(requests, ", id: ", id, "| zome: ", _zomeLookup[id], "| function: ", _funcLookup[id]);

                    e.Reason = $"Error Occured. The WebSocket closed with the following requests still in progress: {requests}. Reason: {e.Reason}";
                    
                    _pendingRequests.Clear();
                    HandleError(e.Reason, null);
                }

                if (!_taskCompletionDisconnected.Task.IsCompleted)
                    _taskCompletionDisconnected.SetResult(e);

                //We need to dispose and nullify the socket in case we want to use it to connect again later.
                if (WebSocket.ClientWebSocket != null)
                {
                    WebSocket.ClientWebSocket.Dispose();
                    WebSocket.ClientWebSocket = null;
                }

                OnDisconnected?.Invoke(this, e);
                ShutDownHolochainConductorsAsync(_shutdownHolochainConductorsMode);
            }
            catch (Exception ex)
            {
                HandleError("Error in HoloNETClient.WebSocket_OnDisconnected method.", ex);
            }
        }

        private void WebSocket_OnDataReceived(object sender, WebSocket.DataReceivedEventArgs e)
        {
            ProcessDataReceived(e);
        }

        private void ProcessDataReceived(WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string rawBinaryDataAsString = "";
            string rawBinaryDataDecoded = "";
            string rawBinaryDataAfterMessagePackDecodeAsString = "";
            string rawBinaryDataAfterMessagePackDecodeDecoded = "";

            try
            {
                rawBinaryDataAsString = DataHelper.ConvertBinaryDataToString(dataReceivedEventArgs.RawBinaryData);
                rawBinaryDataDecoded = DataHelper.DecodeBinaryDataAsUTF8(dataReceivedEventArgs.RawBinaryData);

                Logger.Log("DATA RECEIVED", LogType.Info);
                Logger.Log("Raw Data Received: " + rawBinaryDataAsString, LogType.Debug);
                Logger.Log("Raw Data Decoded: " + rawBinaryDataDecoded, LogType.Debug);

                //if (rawBinaryDataDecoded.Contains("error"))
                //    ProcessErrorReceivedFromConductor(dataReceivedEventArgs, rawBinaryDataDecoded, rawBinaryDataAsString, rawBinaryDataAfterMessagePackDecodeAsString, rawBinaryDataAfterMessagePackDecodeDecoded);
                //else
                //{
                    HoloNETResponse response = DecodeDataReceived(dataReceivedEventArgs.RawBinaryData, dataReceivedEventArgs, rawBinaryDataAsString, rawBinaryDataDecoded);

                    if (!response.IsError)
                    {
                        switch (response.HoloNETResponseType)
                        {
                            case HoloNETResponseType.AppInfo:
                                DecodeAppInfoDataReceived(response, dataReceivedEventArgs, rawBinaryDataAsString, rawBinaryDataDecoded, rawBinaryDataAfterMessagePackDecodeAsString, rawBinaryDataAfterMessagePackDecodeDecoded, false);
                                break;

                            case HoloNETResponseType.Signal:
                                DecodeSignalDataReceived(response, dataReceivedEventArgs, rawBinaryDataAsString, rawBinaryDataDecoded, rawBinaryDataAfterMessagePackDecodeAsString, rawBinaryDataAfterMessagePackDecodeDecoded);
                                break;

                            case HoloNETResponseType.ZomeResponse:
                                DecodeZomeDataReceived(response, dataReceivedEventArgs, rawBinaryDataAsString, rawBinaryDataDecoded, rawBinaryDataAfterMessagePackDecodeAsString, rawBinaryDataAfterMessagePackDecodeDecoded);
                                break;

                            case HoloNETResponseType.AdminAgentPubKeyGenerated:
                                DecodeAdminAgentPubKeyGeneratedReceived(response, dataReceivedEventArgs, rawBinaryDataAsString, rawBinaryDataDecoded, rawBinaryDataAfterMessagePackDecodeAsString, rawBinaryDataAfterMessagePackDecodeDecoded);
                                break;

                            case HoloNETResponseType.AdminAppInstalled:
                                DecodeAdminAppInstalledReceived(response, dataReceivedEventArgs, rawBinaryDataAsString, rawBinaryDataDecoded, rawBinaryDataAfterMessagePackDecodeAsString, rawBinaryDataAfterMessagePackDecodeDecoded);
                                break;

                            case HoloNETResponseType.AdminAppEnabled:
                                DecodeAdminAppEnabledReceived(response, dataReceivedEventArgs, rawBinaryDataAsString, rawBinaryDataDecoded, rawBinaryDataAfterMessagePackDecodeAsString, rawBinaryDataAfterMessagePackDecodeDecoded);
                                break;

                            case HoloNETResponseType.Error:
                                ProcessErrorReceivedFromConductor(response, dataReceivedEventArgs, rawBinaryDataDecoded, rawBinaryDataAsString, rawBinaryDataAfterMessagePackDecodeAsString, rawBinaryDataAfterMessagePackDecodeDecoded);
                                break;
                        }
                        
                        //if (rawBinaryDataDecoded.ToUpper().Contains("APP_INFO"))
                        //    DecodeAppInfoDataReceived(response, dataReceivedEventArgs, rawBinaryDataAsString, rawBinaryDataDecoded, rawBinaryDataAfterMessagePackDecodeAsString, rawBinaryDataAfterMessagePackDecodeDecoded);

                        //else if (response.type.ToUpper() == "SIGNAL")
                        //    DecodeSignalDataReceived(response, dataReceivedEventArgs, rawBinaryDataAsString, rawBinaryDataDecoded, rawBinaryDataAfterMessagePackDecodeAsString, rawBinaryDataAfterMessagePackDecodeDecoded);

                        //else if (rawBinaryDataDecoded.ToLower().Contains("agent_pub_key_generated"))
                        //    DecodeAdminAgentPubKeyGeneratedReceived(response, dataReceivedEventArgs, rawBinaryDataAsString, rawBinaryDataDecoded, rawBinaryDataAfterMessagePackDecodeAsString, rawBinaryDataAfterMessagePackDecodeDecoded);

                        //else
                        //    DecodeZomeDataReceived(response, dataReceivedEventArgs, rawBinaryDataAsString, rawBinaryDataDecoded, rawBinaryDataAfterMessagePackDecodeAsString, rawBinaryDataAfterMessagePackDecodeDecoded);
                    }
               // }
            }
            catch (Exception ex)
            {
                string msg = "Error in HoloNETClient.ProcessDataReceived method.";
                HandleError(msg, ex);
            }
        }

        private void ProcessErrorReceivedFromConductor(HoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs, string rawBinaryDataDecoded, string rawBinaryDataAsString, string rawBinaryDataAfterMessagePackDecodeAsString, string rawBinaryDataAfterMessagePackDecodeDecoded)
        {
            string msg = $"Error in HoloNETClient.ProcessErrorReceivedFromConductor method. Error received from Holochain Conductor: {rawBinaryDataDecoded}";
            HandleError(msg, null);

            if (rawBinaryDataDecoded.Contains("app_info"))
            {
                Logger.Log("APPINFO ERROR", LogType.Error);
                AppInfoCallBackEventArgs args = CreateHoloNETArgs<AppInfoCallBackEventArgs>(response, dataReceivedEventArgs, rawBinaryDataAsString, rawBinaryDataDecoded, rawBinaryDataAfterMessagePackDecodeAsString, rawBinaryDataAfterMessagePackDecodeDecoded);
                args.IsError = true;
                args.IsCallSuccessful = false;
                args.Message = msg;
                RaiseAppInfoReceivedEvent(args);
            }
            else if (response != null)
            {
                if (response.type.ToUpper() == "SIGNAL")
                {
                    Logger.Log("SIGNAL ERROR", LogType.Error);
                    SignalCallBackEventArgs signalCallBackEventArgs = CreateHoloNETArgs<SignalCallBackEventArgs>(response, dataReceivedEventArgs, rawBinaryDataAsString, rawBinaryDataDecoded, rawBinaryDataAfterMessagePackDecodeAsString, rawBinaryDataAfterMessagePackDecodeDecoded);
                    signalCallBackEventArgs.IsError = true;
                    signalCallBackEventArgs.IsCallSuccessful = false;
                    signalCallBackEventArgs.Message = msg;
                    RaiseSignalReceivedEvent(signalCallBackEventArgs);
                }
                else if (rawBinaryDataDecoded.ToUpper().Contains("APPALREADYINSTALLED")) //TODO: Improve all error handling code and other areas so can remove this string parsing code, need to decode the conductor response into proper objects asap! ;-)
                {
                    Logger.Log("APPALREADYINSTALLED ERROR", LogType.Error);
                    AdminAppInstalledCallBackEventArgs adminAppInstalledCallBackEventArgs = CreateHoloNETArgs<AdminAppInstalledCallBackEventArgs>(response, dataReceivedEventArgs, rawBinaryDataAsString, rawBinaryDataDecoded, rawBinaryDataAfterMessagePackDecodeAsString, rawBinaryDataAfterMessagePackDecodeDecoded);
                    adminAppInstalledCallBackEventArgs.IsError = true;
                    adminAppInstalledCallBackEventArgs.IsCallSuccessful = false;
                    adminAppInstalledCallBackEventArgs.Message = msg;
                    RaiseAdminAppInstalledEvent(adminAppInstalledCallBackEventArgs);
                }
            }
            else
            {
                Logger.Log("ZOME CALL ERROR", LogType.Error);
                ZomeFunctionCallBackEventArgs zomeFunctionCallBackArgs = CreateHoloNETArgs<ZomeFunctionCallBackEventArgs>(response, dataReceivedEventArgs, rawBinaryDataAsString, rawBinaryDataDecoded, rawBinaryDataAfterMessagePackDecodeAsString, rawBinaryDataAfterMessagePackDecodeDecoded);
                zomeFunctionCallBackArgs.IsError = true;
                zomeFunctionCallBackArgs.IsCallSuccessful = false;
                zomeFunctionCallBackArgs.Message = msg;
                zomeFunctionCallBackArgs.Zome = GetItemFromCache(response != null ? response.id.ToString() : "", _zomeLookup);
                zomeFunctionCallBackArgs.ZomeFunction = GetItemFromCache(response != null ? response.id.ToString() : "", _funcLookup);
                RaiseZomeDataReceivedEvent(zomeFunctionCallBackArgs);
            }
        }

        private HoloNETResponse DecodeDataReceived(byte[] rawBinaryData, WebSocket.DataReceivedEventArgs dataReceivedEventArgs, string rawBinaryDataAsString, string rawBinaryDataDecoded)
        {
            HoloNETResponse response = null;
            HoloNETDataReceivedEventArgs holoNETDataReceivedEventArgs = new HoloNETDataReceivedEventArgs();

            try
            {
                string id = "";
                string rawBinaryDataAfterMessagePackDecodeAsString = "";
                string rawBinaryDataAfterMessagePackDecodeDecoded = "";

                Logger.Log("DecodeDataReceived. About to Deserialize rawBinaryData...", LogType.Debug);
                response = MessagePackSerializer.Deserialize<HoloNETResponse>(rawBinaryData, messagePackSerializerOptions);
                Logger.Log("DecodeDataReceived. About to Deserialize rawBinaryData... DONE!", LogType.Debug);

                Logger.Log("DecodeDataReceived. About to Deserialize response.data...", LogType.Debug);
                AppResponse appResponse = MessagePackSerializer.Deserialize<AppResponse>(response.data, messagePackSerializerOptions);
                Logger.Log("DecodeDataReceived. About to Deserialize response.data... DONE!", LogType.Debug);

                id = response.id.ToString();

                rawBinaryDataAfterMessagePackDecodeAsString = DataHelper.ConvertBinaryDataToString(response.data);
                rawBinaryDataAfterMessagePackDecodeDecoded = DataHelper.DecodeBinaryDataAsUTF8(response.data);

                Logger.Log("Response", LogType.Info);
                Logger.Log($"Id: {response.id}", LogType.Info);
                Logger.Log($"Type: {response.type}", LogType.Info);
                Logger.Log($"Internal Type: {appResponse.type}", LogType.Info);
                Logger.Log(string.Concat("Raw Data Bytes Received After MessagePack Decode: ", rawBinaryDataAfterMessagePackDecodeAsString), LogType.Debug);
                Logger.Log(string.Concat("Raw Data Bytes Decoded After MessagePack Decode: ", rawBinaryDataAfterMessagePackDecodeDecoded), LogType.Debug);

                switch (appResponse.type)
                {
                    case "zome-response":
                        response.HoloNETResponseType = HoloNETResponseType.ZomeResponse;
                        break;

                    case "signal":
                        response.HoloNETResponseType = HoloNETResponseType.Signal;
                        break;

                    case "app_info":
                        response.HoloNETResponseType = HoloNETResponseType.AppInfo;
                        break;

                    case "agent_pub_key_generated":
                        response.HoloNETResponseType = HoloNETResponseType.AdminAgentPubKeyGenerated;
                        break;

                    case "app_installed":
                        response.HoloNETResponseType = HoloNETResponseType.AdminAppInstalled;
                        break;

                    case "app_enabled":
                        response.HoloNETResponseType = HoloNETResponseType.AdminAppEnabled;
                        break;

                    case "app_disabled":
                        response.HoloNETResponseType = HoloNETResponseType.AdminAppDisabled;
                        break;

                    case "grant_zome_call_capability":
                        response.HoloNETResponseType = HoloNETResponseType.AdminGrantZomeCallCapability;
                        break;

                    case "error":
                        response.HoloNETResponseType = HoloNETResponseType.Error;
                        break;
                }

                holoNETDataReceivedEventArgs = CreateHoloNETArgs<HoloNETDataReceivedEventArgs>(response, dataReceivedEventArgs, rawBinaryDataAsString, rawBinaryDataDecoded, rawBinaryDataAfterMessagePackDecodeAsString, rawBinaryDataAfterMessagePackDecodeDecoded);

                //holoNETDataReceivedEventArgs = new HoloNETDataReceivedEventArgs()
                //{
                //    Id = id,
                //    EndPoint = dataReceivedEventArgs.EndPoint,
                //    Type = response.type,
                //    HoloNETResponseType = response.HoloNETResponseType,
                //    IsCallSuccessful = true,
                //    RawBinaryData = rawBinaryData,
                //    RawBinaryDataAsString = rawBinaryDataAsString,
                //    RawBinaryDataDecoded = rawBinaryDataDecoded,
                //    RawBinaryDataAfterMessagePackDecode = response != null ? response.data : null,
                //    RawBinaryDataAfterMessagePackDecodeAsString = rawBinaryDataAfterMessagePackDecodeAsString,
                //    RawBinaryDataAfterMessagePackDecodeDecoded = rawBinaryDataAfterMessagePackDecodeDecoded,
                //    RawJSONData = dataReceivedEventArgs.RawJSONData,
                //    WebSocketResult = dataReceivedEventArgs.WebSocketResult
                //};

                if (Config.EnforceRequestToResponseIdMatchingBehaviour != EnforceRequestToResponseIdMatchingBehaviour.Ignore
                    && !_pendingRequests.Contains(id))
                {
                    holoNETDataReceivedEventArgs.IsError = Config.EnforceRequestToResponseIdMatchingBehaviour == EnforceRequestToResponseIdMatchingBehaviour.Error;
                    holoNETDataReceivedEventArgs.IsCallSuccessful = false;
                    holoNETDataReceivedEventArgs.Message = $"The id returned in the response ({id}) does not match any pending request.";

                    if (holoNETDataReceivedEventArgs.IsError)
                        holoNETDataReceivedEventArgs.Message = string.Concat(holoNETDataReceivedEventArgs.Message, " Config.EnforceRequestToResponseIdMatchingBehaviour is set to Error so Aborting Request.");
                }

                _pendingRequests.Remove(id);
                response.IsError = holoNETDataReceivedEventArgs.IsError;
            }
            catch (Exception ex)
            {
                string msg = $"An unknown error occurred in HoloNETClient.DecodeDataReceived. Reason: {ex}";
                holoNETDataReceivedEventArgs.IsError = true;
                holoNETDataReceivedEventArgs.Message = msg;
                HandleError(msg, ex);
            }

            OnDataReceived?.Invoke(this, holoNETDataReceivedEventArgs);
            return response;
        }

        private void DecodeAdminAgentPubKeyGeneratedReceived(HoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs, string rawBinaryDataAsString, string rawBinaryDataDecoded, string rawBinaryDataAfterMessagePackDecodeAsString, string rawBinaryDataAfterMessagePackDecodeDecoded)
        {
            AdminAgentPubKeyGeneratedCallBackEventArgs args = CreateHoloNETArgs<AdminAgentPubKeyGeneratedCallBackEventArgs>(response, dataReceivedEventArgs, rawBinaryDataAsString, rawBinaryDataDecoded, rawBinaryDataAfterMessagePackDecodeAsString, rawBinaryDataAfterMessagePackDecodeDecoded);

            try
            {
                Logger.Log("ADMIN AGENT PUB KEY GENERATED DATA DETECTED\n", LogType.Info);
                AppResponse appResponse = MessagePackSerializer.Deserialize<AppResponse>(response.data, messagePackSerializerOptions);
                args.AgentPubKey = ConvertHoloHashToString(appResponse.data);
                args.AppResponse = appResponse;
                //args.EndPoint = dataReceivedEventArgs.EndPoint;
                //args.Id = response.id.ToString();
                //args.IsCallSuccessful = true;
                //args.RawBinaryData = dataReceivedEventArgs.RawBinaryData;
                //args.RawBinaryDataAsString = rawBinaryDataAsString;
                //args.RawBinaryDataDecoded = rawBinaryDataDecoded;
                //args.RawBinaryDataAfterMessagePackDecode = response != null ? response.data : null;
                //args.RawBinaryDataAfterMessagePackDecodeAsString = rawBinaryDataAfterMessagePackDecodeAsString;
                //args.RawBinaryDataAfterMessagePackDecodeDecoded = rawBinaryDataAfterMessagePackDecodeDecoded;
                //args.RawJSONData = dataReceivedEventArgs.RawJSONData;
                //args.WebSocketResult = dataReceivedEventArgs.WebSocketResult;

                Logger.Log($"AGENT PUB KEY GENERATED: {args.AgentPubKey}\n", LogType.Info);

                if (_updateDnaHashAndAgentPubKey)
                    Config.AgentPubKey = args.AgentPubKey;
            }
            catch (Exception ex)
            {
                string msg = $"An unknown error occurred in HoloNETClient.DecodeAdminAgentPubKeyGeneratedReceived. Reason: {ex}";
                args.IsError = true;
                args.IsCallSuccessful = false;
                args.Message = msg;
                HandleError(msg, ex);
            }

            RaiseAdminAgentPubKeyGeneratedEvent(args);
        }

        private void DecodeAdminAppInstalledReceived(HoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs, string rawBinaryDataAsString, string rawBinaryDataDecoded, string rawBinaryDataAfterMessagePackDecodeAsString, string rawBinaryDataAfterMessagePackDecodeDecoded)
        {
            AdminAppInstalledCallBackEventArgs args = new AdminAppInstalledCallBackEventArgs();

            try
            {
                Logger.Log("ADMIN APP INSTALLED DATA DETECTED\n", LogType.Info);
                DecodeAppInfoDataReceived(response, dataReceivedEventArgs, rawBinaryDataAsString, rawBinaryDataDecoded, rawBinaryDataAfterMessagePackDecodeAsString, rawBinaryDataAfterMessagePackDecodeDecoded, true);
            }
            catch (Exception ex)
            {
                string msg = $"An unknown error occurred in HoloNETClient.DecodeAdminAppInstalledReceived. Reason: {ex}";
                args.IsError = true;
                args.IsCallSuccessful = false;
                args.Message = msg;
                RaiseAdminAppInstalledEvent(args);
                HandleError(msg, ex);
            }
        }

        private void DecodeAdminAppEnabledReceived(HoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs, string rawBinaryDataAsString, string rawBinaryDataDecoded, string rawBinaryDataAfterMessagePackDecodeAsString, string rawBinaryDataAfterMessagePackDecodeDecoded)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeAdminAppEnabledReceived. Reason: ";
            AdminAppEnabledCallBackEventArgs args = CreateHoloNETArgs<AdminAppEnabledCallBackEventArgs>(response, dataReceivedEventArgs, rawBinaryDataAsString, rawBinaryDataDecoded, rawBinaryDataAfterMessagePackDecodeAsString, rawBinaryDataAfterMessagePackDecodeDecoded);

            try
            {
                Logger.Log("ADMIN APP ENABLED DATA DETECTED\n", LogType.Info);
                EnableAppResponse enableAppResponse = MessagePackSerializer.Deserialize<EnableAppResponse>(response.data, messagePackSerializerOptions);

                if (enableAppResponse != null)
                {
                    enableAppResponse.App = ProcessAppInfo(enableAppResponse.App, args);
                    //args.AppInfo = enableAppResponse.App;
                    args.AppInfoResponse = new AppInfoResponse() { data = enableAppResponse.App };
                    args.HoloNETResponseType = HoloNETResponseType.AdminAppEnabled;
                    args.Errors = enableAppResponse.Errors; //TODO: Need to find out what this contains and the correct data structure.
                }
                else
                {
                    string msg = $"{errorMessage} An error occurred deserialzing EnableAppResponse from the Holochain Conductor";
                    args.IsError = true;
                    args.IsCallSuccessful = false;
                    args.Message = msg;
                    HandleError(msg);
                }
            }
            catch (Exception ex)
            {
                string msg = $"{errorMessage} {ex}";
                args.IsError = true;
                args.IsCallSuccessful = false;
                args.Message = msg;
                HandleError(msg, ex);
            }

            RaiseAdminAppEnabledEvent(args);
        }

        private void DecodeAppInfoDataReceived(HoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs, string rawBinaryDataAsString, string rawBinaryDataDecoded, string rawBinaryDataAfterMessagePackDecodeAsString, string rawBinaryDataAfterMessagePackDecodeDecoded, bool isAdminInstallingApp)
        {
            AppInfoCallBackEventArgs args = new AppInfoCallBackEventArgs();
            AppInfoResponse appInfoResponse = null;

            try
            {
                Logger.Log("APP INFO RESPONSE DATA DETECTED\n", LogType.Info);
                appInfoResponse = MessagePackSerializer.Deserialize<AppInfoResponse>(response.data, messagePackSerializerOptions);
                args = CreateHoloNETArgs<AppInfoCallBackEventArgs>(response, dataReceivedEventArgs, rawBinaryDataAsString, rawBinaryDataDecoded, rawBinaryDataAfterMessagePackDecodeAsString, rawBinaryDataAfterMessagePackDecodeDecoded);

                //args = new AppInfoCallBackEventArgs()
                //{
                //    AppInfoResponse = appInfoResponse,
                //    AppInfo = appInfoResponse.data, //TEMP
                //    EndPoint = dataReceivedEventArgs.EndPoint,
                //    Id = response.id.ToString(),
                //    IsCallSuccessful = true,
                //    RawBinaryData = dataReceivedEventArgs.RawBinaryData,
                //    RawBinaryDataAsString = rawBinaryDataAsString,
                //    RawBinaryDataDecoded = rawBinaryDataDecoded,
                //    RawBinaryDataAfterMessagePackDecode = response != null ? response.data : null,
                //    RawBinaryDataAfterMessagePackDecodeAsString = rawBinaryDataAfterMessagePackDecodeAsString,
                //    RawBinaryDataAfterMessagePackDecodeDecoded = rawBinaryDataAfterMessagePackDecodeDecoded,
                //    RawJSONData = dataReceivedEventArgs.RawJSONData,
                //    WebSocketResult = dataReceivedEventArgs.WebSocketResult
                //};

                if (appInfoResponse != null)
                {
                    appInfoResponse.data = ProcessAppInfo(appInfoResponse.data, args);
                    //args.AppInfo = appInfoResponse.data; //TEMP
                    args.AppInfoResponse = appInfoResponse;
                }
                else
                {
                    args.Message = "Error occured in HoloNETClient.DecodeAppInfoDataReceived. appInfoResponse is null.";
                    args.IsError = true;
                }
            }
            catch (Exception ex)
            {
                string msg = $"An unknown error occurred in HoloNETClient.DecodeAppInfoDataReceived. Reason: {ex}";
                args.IsError = true;
                args.IsCallSuccessful = false;
                args.Message = msg;
                HandleError(msg, ex);
            }

            //If either the AgentPubKey or DnaHash is empty then attempt to get from the sandbox cmd.
            if (!args.IsError && !isAdminInstallingApp)
            {
                if (!string.IsNullOrEmpty(Config.AgentPubKey) && !string.IsNullOrEmpty(Config.DnaHash))
                    SetReadyForZomeCalls();

                else if (_automaticallyAttemptToGetFromSandboxIfConductorFails)
                    RetrieveAgentPubKeyAndDnaHashFromSandbox();
            }

            if (isAdminInstallingApp)
            {
                AdminAppInstalledCallBackEventArgs installedCallBackEventArgs = new AdminAppInstalledCallBackEventArgs()
                {
                    AgentPubKey = args.AgentPubKey,
                    DnaHash = args.DnaHash,
                    InstalledAppId = args.InstalledAppId,
                    //AppInfo = args.AppInfo,
                    AppInfoResponse = args.AppInfoResponse,
                    HoloNETResponseType = HoloNETResponseType.AdminAppInstalled
                };

                CopyHoloNETArgs(args, installedCallBackEventArgs);
                RaiseAdminAppInstalledEvent(installedCallBackEventArgs);

                //RaiseAdminAppInstalledEvent(new AdminAppInstalledCallBackEventArgs()
                //{
                //    AgentPubKey = args.AgentPubKey,
                //    DnaHash = args.DnaHash,
                //    InstalledAppId = args.InstalledAppId,
                //    AppInfo = args.AppInfo,
                //    HoloNETResponseType = HoloNETResponseType.AdminAppInstalled
                //    //EndPoint = args.EndPoint,
                //    //Excception = args.Excception,
                //    //HoloNETResponseType = HoloNETResponseType.AdminAppInstalled,
                //    //Id = args.Id,
                //    //IsCallSuccessful = args.IsCallSuccessful,
                //    //IsError = args.IsError,
                //    //Message = args.Message,
                //    //RawBinaryData = args.RawBinaryData,
                //    //RawBinaryDataAfterMessagePackDecode = args.RawBinaryDataAfterMessagePackDecode,
                //    //RawBinaryDataAfterMessagePackDecodeAsString = args.RawBinaryDataAfterMessagePackDecodeAsString,
                //    //RawBinaryDataAfterMessagePackDecodeDecoded = args.RawBinaryDataAfterMessagePackDecodeDecoded,
                //    //RawBinaryDataDecoded = args.RawBinaryDataDecoded,
                //    //RawBinaryDataAsString = args.RawBinaryDataAsString,
                //    //RawJSONData = args.RawJSONData,
                //    //WebSocketResult = args.WebSocketResult 
                //});
            }
            else
                RaiseAppInfoReceivedEvent(args);

            //return appInfoResponse;
        }

        private T CreateHoloNETArgs<T>(HoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs, string rawBinaryDataAsString, string rawBinaryDataDecoded, string rawBinaryDataAfterMessagePackDecodeAsString, string rawBinaryDataAfterMessagePackDecodeDecoded) where T : HoloNETDataReceivedBaseEventArgs, new()
        {
            return new T()
            {
                EndPoint = dataReceivedEventArgs != null ? dataReceivedEventArgs.EndPoint : "",
                Id = response != null ? response.id.ToString() : "",
                IsCallSuccessful = true,
                RawBinaryData = dataReceivedEventArgs != null ? dataReceivedEventArgs.RawBinaryData : null,
                RawBinaryDataAsString = dataReceivedEventArgs != null ? rawBinaryDataAsString : "",
                RawBinaryDataDecoded = rawBinaryDataDecoded,
                //RawBinaryDataAfterMessagePackDecode = response != null ? response.data : null,
                //RawBinaryDataAfterMessagePackDecodeAsString = rawBinaryDataAfterMessagePackDecodeAsString,
                //RawBinaryDataAfterMessagePackDecodeDecoded = rawBinaryDataAfterMessagePackDecodeDecoded,
                RawJSONData = dataReceivedEventArgs != null ? dataReceivedEventArgs.RawJSONData : "",
                WebSocketResult = dataReceivedEventArgs != null ? dataReceivedEventArgs.WebSocketResult : null
            };
        }

        private T1 CopyHoloNETArgs<T1>(T1 sourceArgs, T1 targetArgs) where T1 : HoloNETDataReceivedBaseEventArgs
        {
            targetArgs.EndPoint = sourceArgs.EndPoint;
            targetArgs.Excception = sourceArgs.Excception;
            //targetArgs.HoloNETResponseType = sourceArgs.HoloNETResponseType;
            targetArgs.Id = sourceArgs.Id;
            targetArgs.IsCallSuccessful = sourceArgs.IsCallSuccessful;
            targetArgs.IsError = sourceArgs.IsError;
            targetArgs.Message = sourceArgs.Message;
            targetArgs.RawBinaryData = sourceArgs.RawBinaryData;
            //targetArgs.RawBinaryDataAfterMessagePackDecode = sourceArgs.RawBinaryDataAfterMessagePackDecode;
            //targetArgs.RawBinaryDataAfterMessagePackDecodeAsString = sourceArgs.RawBinaryDataAfterMessagePackDecodeAsString;
            //targetArgs.RawBinaryDataAfterMessagePackDecodeDecoded = sourceArgs.RawBinaryDataAfterMessagePackDecodeDecoded;
            targetArgs.RawBinaryDataDecoded = sourceArgs.RawBinaryDataDecoded;
            targetArgs.RawBinaryDataAsString = sourceArgs.RawBinaryDataAsString;
            targetArgs.RawJSONData = sourceArgs.RawJSONData;
            targetArgs.WebSocketResult = sourceArgs.WebSocketResult;

            return targetArgs;
        }

        private void DecodeSignalDataReceived(HoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs, string rawBinaryDataAsString, string rawBinaryDataDecoded, string rawBinaryDataAfterMessagePackDecodeAsString, string rawBinaryDataAfterMessagePackDecodeDecoded)
        {
            SignalCallBackEventArgs signalCallBackEventArgs = new SignalCallBackEventArgs();

            try
            {
                Logger.Log("SIGNAL DATA DETECTED\n", LogType.Info);

                SignalResponse appResponse = MessagePackSerializer.Deserialize<SignalResponse>(response.data, messagePackSerializerOptions);
                Dictionary<string, object> signalDataDecoded = new Dictionary<string, object>();
                SignalType signalType = SignalType.App;
                string agentPublicKey = "";
                string dnaHash = "";
                string signalDataAsString = "";

                if (appResponse != null)
                {
                    agentPublicKey = ConvertHoloHashToString(appResponse.App.CellData[0]);
                    dnaHash = ConvertHoloHashToString(appResponse.App.CellData[1]);
                    Dictionary<object, byte[]> signalData = MessagePackSerializer.Deserialize<Dictionary<object, byte[]>>(appResponse.App.Data, messagePackSerializerOptions);

                    foreach (object key in signalData.Keys)
                    {
                        signalDataDecoded[key.ToString()] = MessagePackSerializer.Deserialize<object>(signalData[key]);
                        signalDataAsString = string.Concat(signalDataAsString, key.ToString(), "=", signalDataDecoded[key.ToString()], ",");
                    }

                    signalDataAsString = signalDataAsString.Substring(0, signalDataAsString.Length - 1);
                }
                else
                    signalType = SignalType.System;

                signalCallBackEventArgs = CreateHoloNETArgs<SignalCallBackEventArgs>(response, dataReceivedEventArgs, rawBinaryDataAsString, rawBinaryDataDecoded, rawBinaryDataAfterMessagePackDecodeAsString, rawBinaryDataAfterMessagePackDecodeDecoded);
                signalCallBackEventArgs.DnaHash = dnaHash;
                signalCallBackEventArgs.AgentPubKey = agentPublicKey;
                signalCallBackEventArgs.RawSignalData = appResponse.App;
                signalCallBackEventArgs.SignalData = signalDataDecoded;
                signalCallBackEventArgs.SignalDataAsString = signalDataAsString;
                signalCallBackEventArgs.SignalType = signalType; //TODO: Need to test for System SignalType... Not even sure if we want to raise this event for System signals? (the js client ignores them currently).
            }
            catch (Exception ex)
            {
                string msg = $"An unknown error occurred in HoloNETClient.DecodeSignalDataReceived. Reason: {ex}";
                signalCallBackEventArgs.IsError = true;
                signalCallBackEventArgs.Message = msg;
                HandleError(msg, ex);
            }

            RaiseSignalReceivedEvent(signalCallBackEventArgs);
        }

        private void DecodeZomeDataReceived(HoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs, string rawBinaryDataAsString, string rawBinaryDataDecoded, string rawBinaryDataAfterMessagePackDecodeAsString, string rawBinaryDataAfterMessagePackDecodeDecoded)
        {
            Logger.Log("ZOME RESPONSE DATA DETECTED\n", LogType.Info);
            AppResponse appResponse = MessagePackSerializer.Deserialize<AppResponse>(response.data, messagePackSerializerOptions);
            string id = response.id.ToString();
            ZomeFunctionCallBackEventArgs zomeFunctionCallBackArgs = new ZomeFunctionCallBackEventArgs();

            try
            {
                Dictionary<object, object> rawAppResponseData = MessagePackSerializer.Deserialize<Dictionary<object, object>>(appResponse.data, messagePackSerializerOptions);
                Dictionary<string, object> appResponseData = new Dictionary<string, object>();
                Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
                string keyValuePairsAsString = "";
                EntryData entryData = null;

                (appResponseData, keyValuePairs, keyValuePairsAsString, entryData) = DecodeRawZomeData(rawAppResponseData, appResponseData, keyValuePairs, keyValuePairsAsString);

                if (_entryDataObjectTypeLookup.ContainsKey(id) && _entryDataObjectTypeLookup[id] != null)
                    entryData.EntryDataObject = MapEntryDataObject(_entryDataObjectTypeLookup[id], keyValuePairs);

                else if (_entryDataObjectLookup.ContainsKey(id) && _entryDataObjectLookup[id] != null)
                    entryData.EntryDataObject = MapEntryDataObject(_entryDataObjectLookup[id], keyValuePairs);


                Logger.Log($"Decoded Data:\n{keyValuePairsAsString}", LogType.Info);
                //Logger.Log($"appResponse.data as string: {DataHelper.ConvertBinaryDataToString(appResponse.data)}", LogType.Debug);
                //Logger.Log($"appResponse.data decoded: {DataHelper.DecodeBinaryData(appResponse.data)}", LogType.Debug);

                zomeFunctionCallBackArgs = new ZomeFunctionCallBackEventArgs
                {
                    Id = id,
                    EndPoint = dataReceivedEventArgs.EndPoint,
                    Zome = GetItemFromCache(id, _zomeLookup),
                    ZomeFunction = GetItemFromCache(id, _funcLookup),
                    IsCallSuccessful = true,
                    RawZomeReturnData = rawAppResponseData,
                    ZomeReturnData = appResponseData,
                    KeyValuePair = keyValuePairs,
                    KeyValuePairAsString = keyValuePairsAsString,
                    Entry = entryData,
                    RawBinaryData = dataReceivedEventArgs.RawBinaryData,
                    RawBinaryDataAsString = rawBinaryDataAsString,
                    RawBinaryDataDecoded = rawBinaryDataDecoded,
                    //RawBinaryDataAfterMessagePackDecode = response != null ? response.data : null,
                    //RawBinaryDataAfterMessagePackDecodeAsString = rawBinaryDataAfterMessagePackDecodeAsString,
                    //RawBinaryDataAfterMessagePackDecodeDecoded = rawBinaryDataAfterMessagePackDecodeDecoded,
                    RawJSONData = dataReceivedEventArgs.RawJSONData,
                    WebSocketResult = dataReceivedEventArgs.WebSocketResult
                };
            }
            catch (Exception ex)
            {
                try
                {
                    object rawAppResponseData = MessagePackSerializer.Deserialize<object>(appResponse.data, messagePackSerializerOptions);
                    byte[] holoHash = rawAppResponseData as byte[];

                    zomeFunctionCallBackArgs = new ZomeFunctionCallBackEventArgs
                    {
                        Id = id,
                        EndPoint = dataReceivedEventArgs.EndPoint,
                        Zome = GetItemFromCache(id, _zomeLookup),
                        ZomeFunction = GetItemFromCache(id, _funcLookup),
                        IsCallSuccessful = true,
                        RawBinaryData = dataReceivedEventArgs.RawBinaryData,
                        RawBinaryDataAsString = rawBinaryDataAsString,
                        RawBinaryDataDecoded = rawBinaryDataDecoded,
                        //RawBinaryDataAfterMessagePackDecode = response != null ? response.data : null,
                        //RawBinaryDataAfterMessagePackDecodeAsString = rawBinaryDataAfterMessagePackDecodeAsString,
                        //RawBinaryDataAfterMessagePackDecodeDecoded = rawBinaryDataAfterMessagePackDecodeDecoded,
                        RawJSONData = dataReceivedEventArgs.RawJSONData,
                        WebSocketResult = dataReceivedEventArgs.WebSocketResult
                    };

                    if (holoHash != null)
                    {
                        string hash = ConvertHoloHashToString(holoHash);
                        Logger.Log($"Decoded Data:\nHoloHash: {hash}", LogType.Info);
                        zomeFunctionCallBackArgs.ZomeReturnHash = hash;
                    }
                    else
                    {
                        string msg = $"An unknown response was received from the conductor for type 'Response' (Zome Response). Response Received: {rawAppResponseData}";

                        zomeFunctionCallBackArgs.IsError = true;
                        zomeFunctionCallBackArgs.IsCallSuccessful = false;
                        zomeFunctionCallBackArgs.Message = msg;
                        HandleError(msg, null);
                    }
                }
                catch (Exception ex2)
                {
                    string msg = $"An unknown error occurred in HoloNETClient.DecodeZomeDataReceived. Reason: {ex2}";
                    zomeFunctionCallBackArgs.IsError = true;
                    zomeFunctionCallBackArgs.Message = msg;
                    HandleError(msg, ex2);
                }
            }

            RaiseZomeDataReceivedEvent(zomeFunctionCallBackArgs);
        }

        private AppInfo ProcessAppInfo(AppInfo appInfo, AppInfoCallBackEventArgs args)
        {
            if (appInfo != null)
            {
                string agentPubKey = ConvertHoloHashToString(appInfo.agent_pub_key);
                string agentPubKeyFromCell = "";
                string dnaHash = "";

                if (appInfo.cell_info != null)
                {
                    var first = appInfo.cell_info.First();

                    if (first.Value != null && first.Value.Count > 0 && first.Value[0].Provisioned != null && first.Value[0].Provisioned.cell_id != null)
                    {
                        agentPubKeyFromCell = ConvertHoloHashToString(first.Value[0].Provisioned.cell_id[1]);
                        dnaHash = ConvertHoloHashToString(first.Value[0].Provisioned.cell_id[0]);

                        if (agentPubKeyFromCell != agentPubKey)
                        {
                            args.Message = $"Error occured in HoloNETClient.ProcessAppInfo. appInfoResponse.data.agent_pub_key and agentPubKey dervived from cell data do not match! appInfoResponse.data.agent_pub_key = {agentPubKey}, cellData = {agentPubKeyFromCell} ";
                            args.IsError = true;
                        }


                        //Conductor time is in microseconds so we need to multiple it by 10 to get the ticks needed to convert it to a DateTime.
                        //Also UNIX dateime (which hc uses) starts from 1970).
                        first.Value[0].Provisioned.dna_modifiers.OriginTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(first.Value[0].Provisioned.dna_modifiers.origin_time * 10);
                        //first.Value[0].provisioned.dna_modifiers.OriginTime = new DateTime(first.Value[0].provisioned.dna_modifiers.origin_time * 10); 
                        //first.Value[0].provisioned.dna_modifiers.OriginTime = new DateTime(first.Value[0].provisioned.dna_modifiers.origin_time); 
                        //first.Value[0].provisioned.dna_modifiers.OriginTime = Convert.ToDateTime(first.Value[0].provisioned.dna_modifiers.origin_time);
                    }
                }

                if (_updateDnaHashAndAgentPubKey)
                {
                    if (!string.IsNullOrEmpty(agentPubKey))
                        Config.AgentPubKey = agentPubKey;

                    if (!string.IsNullOrEmpty(dnaHash))
                        Config.DnaHash = dnaHash;
                }

                Logger.Log($"AGENT PUB KEY RETURNED FROM CONDUCTOR: {Config.AgentPubKey}", LogType.Info);
                Logger.Log($"DNA HASH RETURNED FROM CONDUCTOR:: {Config.DnaHash}", LogType.Info);

                args.AgentPubKey = agentPubKey;
                args.DnaHash = dnaHash;
                args.InstalledAppId = appInfo.installed_app_id;

                if (appInfo.manifest != null)
                {
                    if (appInfo.manifest.roles != null && appInfo.manifest.roles.Length > 0)
                    {
                        string strategy = appInfo.manifest.roles[0].provisioning.strategy.ToPascalCase();

                        if (appInfo.manifest.roles[0].provisioning != null)
                            appInfo.manifest.roles[0].provisioning.StrategyType = (ProvisioningStrategyType)Enum.Parse(typeof(ProvisioningStrategyType), appInfo.manifest.roles[0].provisioning.strategy.ToPascalCase());
                    }
                }
            }
            else
            {
                args.Message = "Error occured in HoloNETClient.ProcessAppInfo. appInfo is null.";
                args.IsError = true;
            }

            return appInfo;
        }

        private (Dictionary<string, object>, Dictionary<string, string> keyValuePair, string keyValuePairAsString, EntryData entry) DecodeRawZomeData(Dictionary<object, object> rawAppResponseData, Dictionary<string, object> appResponseData, Dictionary<string, string> keyValuePair, string keyValuePairAsString, EntryData entryData = null)
        {
            string value = "";
            var options = MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);

            if (entryData == null)
                entryData = new EntryData();

            try
            {
                foreach (string key in rawAppResponseData.Keys)
                {
                    try
                    {
                        value = "";
                        byte[] bytes = rawAppResponseData[key] as byte[];

                        if (bytes != null)
                        {
                            if (key == "entry")
                            {
                                string byteString = "";

                                for (int i = 0; i < bytes.Length; i++)
                                    byteString = string.Concat(byteString, bytes[i], ",");

                                byteString = byteString.Substring(0, byteString.Length - 1);

                                Dictionary<object, object> entry = MessagePackSerializer.Deserialize<Dictionary<object, object>>(bytes, options);
                                Dictionary<string, object> decodedEntry = new Dictionary<string, object>();

                                if (entry != null)
                                {
                                    foreach (object entryKey in entry.Keys)
                                    {
                                        decodedEntry[entryKey.ToString()] = entry[entryKey].ToString();
                                        keyValuePair[entryKey.ToString()] = entry[entryKey].ToString();
                                        keyValuePairAsString = string.Concat(keyValuePairAsString, entryKey.ToString(), "=", entry[entryKey].ToString(), "\n");
                                    }

                                    entryData.Bytes = bytes;
                                    entryData.BytesString = byteString;
                                    entryData.Entry = decodedEntry;
                                    appResponseData[key] = entryData;
                                }
                            }
                            else
                                value = ConvertHoloHashToString(bytes);
                        }
                        else
                        {
                            Dictionary<object, object> dict = rawAppResponseData[key] as Dictionary<object, object>;

                            if (dict != null)
                            {
                                Dictionary<string, object> tempDict = new Dictionary<string, object>();
                                (tempDict, keyValuePair, keyValuePairAsString, entryData) = DecodeRawZomeData(dict, tempDict, keyValuePair, keyValuePairAsString, entryData);
                                appResponseData[key] = tempDict;
                            }
                            else if (rawAppResponseData[key] != null)
                                value = rawAppResponseData[key].ToString();
                        }

                        if (!string.IsNullOrEmpty(value))
                        {
                            keyValuePairAsString = string.Concat(keyValuePairAsString, key, "=", value, "\n");
                            keyValuePair[key] = value;
                            appResponseData[key] = value;

                            try
                            {
                                switch (key)
                                {
                                    case "hash":
                                        entryData.Hash = value;
                                        break;

                                    case "entry_hash":
                                        entryData.EntryHash = value;
                                        break;

                                    case "prev_action":
                                        entryData.PreviousHash = value;
                                        break;

                                    case "signature":
                                        entryData.Signature = value;
                                        break;

                                    case "action_seq":
                                        entryData.ActionSequence = Convert.ToInt32(value);
                                        break;

                                    case "author":
                                        entryData.Author = value;
                                        break;

                                    case "original_action_address":
                                        entryData.OriginalActionAddress = value;
                                        break;

                                    case "original_entry_address":
                                        entryData.OriginalEntryAddress = value;
                                        break;

                                    case "timestamp":
                                        {
                                            entryData.Timestamp = Convert.ToInt64(value);
                                            long time = entryData.Timestamp / 1000; // Divide by 1,000 because we need milliseconds, not microseconds.
                                            entryData.DateTime = DateTimeOffset.FromUnixTimeMilliseconds(time).DateTime.AddHours(-5).AddMinutes(1);
                                        }
                                        break;

                                    case "type":
                                        entryData.Type = value;
                                        break;

                                    case "entry_type":
                                        entryData.EntryType = value;
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                HandleError("Error in HoloNETClient.DecodeZomeReturnData method.", ex);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        HandleError("Error in HoloNETClient.DecodeZomeReturnData method.", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError("Error in HoloNETClient.DecodeZomeReturnData method.", ex);
            }

            return (appResponseData, keyValuePair, keyValuePairAsString, entryData);
        }

        private void RaiseSignalReceivedEvent(SignalCallBackEventArgs signalCallBackEventArgs)
        {
            //TODO: Copy Test Harness logging into appropriate logs in this method for log/debug logtypes... (espcially decoded data)
            Logger.Log(string.Concat("Id: ", signalCallBackEventArgs.Id, ", Is Call Successful: ", signalCallBackEventArgs.IsCallSuccessful ? "True" : "False", ", AgentPubKey: ", signalCallBackEventArgs.AgentPubKey, ", DnaHash: ", signalCallBackEventArgs.DnaHash, " Raw Binary Data: ", signalCallBackEventArgs.RawBinaryData, ", Raw JSON Data: ", signalCallBackEventArgs.RawJSONData, ", Signal Type: ", Enum.GetName(typeof(SignalType), signalCallBackEventArgs.SignalType), ", Signal Data: ", signalCallBackEventArgs.SignalDataAsString, "\n"), LogType.Info);
            OnSignalCallBack?.Invoke(this, signalCallBackEventArgs);
        }

        private void RaiseAppInfoReceivedEvent(AppInfoCallBackEventArgs appInfoCallBackEventArgs)
        {
            Logger.Log(string.Concat("Id: ", appInfoCallBackEventArgs.Id, ", Is Call Successful: ", appInfoCallBackEventArgs.IsCallSuccessful ? "True" : "False", ", AgentPubKey: ", appInfoCallBackEventArgs.AgentPubKey, ", DnaHash: ", appInfoCallBackEventArgs.DnaHash, ", Installed App Id: ", appInfoCallBackEventArgs.InstalledAppId, ", Raw Binary Data: ", appInfoCallBackEventArgs.RawBinaryData, ", Raw JSON Data: ", appInfoCallBackEventArgs.RawJSONData, "\n"), LogType.Info);
            OnAppInfoCallBack?.Invoke(this, appInfoCallBackEventArgs);
        }

        private void RaiseZomeDataReceivedEvent(ZomeFunctionCallBackEventArgs zomeFunctionCallBackArgs)
        {
            //Logger.Log(string.Concat("Id: ", zomeFunctionCallBackArgs.Id, ", Zome: ", zomeFunctionCallBackArgs.Zome, ", Zome Function: ", zomeFunctionCallBackArgs.ZomeFunction, ", Is Zome Call Successful: ", zomeFunctionCallBackArgs.IsCallSuccessful ? "True" : "False", ", Raw Zome Return Data: ", zomeFunctionCallBackArgs.RawZomeReturnData, ", Zome Return Data: ", zomeFunctionCallBackArgs.ZomeReturnData, ", Zome Return Hash: ", zomeFunctionCallBackArgs.ZomeReturnHash, ", Raw Binary Data: ", zomeFunctionCallBackArgs.RawBinaryData, ", Raw Binary Data As String: ", zomeFunctionCallBackArgs.RawBinaryDataAsString, "Raw Binary Data Decoded: ", zomeFunctionCallBackArgs.RawBinaryDataDecoded, ", Raw Binary Data After MessagePack Decode: ", zomeFunctionCallBackArgs.RawBinaryDataAfterMessagePackDecode, ",  Raw Binary Data After MessagePack Decode As String: ", zomeFunctionCallBackArgs.RawBinaryDataAfterMessagePackDecodeAsString, ", Raw Binary Data Decoded After MessagePack Decode: ", zomeFunctionCallBackArgs.RawBinaryDataAfterMessagePackDecodeDecoded, ", Raw JSON Data: ", zomeFunctionCallBackArgs.RawJSONData), LogType.Info);
            Logger.Log(string.Concat("Id: ", zomeFunctionCallBackArgs.Id, ", Zome: ", zomeFunctionCallBackArgs.Zome, ", Zome Function: ", zomeFunctionCallBackArgs.ZomeFunction, ", Is Zome Call Successful: ", zomeFunctionCallBackArgs.IsCallSuccessful ? "True" : "False", ", Raw Zome Return Data: ", zomeFunctionCallBackArgs.RawZomeReturnData, ", Zome Return Data: ", zomeFunctionCallBackArgs.ZomeReturnData, ", Zome Return Hash: ", zomeFunctionCallBackArgs.ZomeReturnHash, ", Raw Binary Data: ", zomeFunctionCallBackArgs.RawBinaryData, ", Raw Binary Data As String: ", zomeFunctionCallBackArgs.RawBinaryDataAsString, "Raw Binary Data Decoded: ", zomeFunctionCallBackArgs.RawBinaryDataDecoded, " Raw JSON Data: ", zomeFunctionCallBackArgs.RawJSONData), LogType.Info);

            if (_callbackLookup.ContainsKey(zomeFunctionCallBackArgs.Id) && _callbackLookup[zomeFunctionCallBackArgs.Id] != null)
                _callbackLookup[zomeFunctionCallBackArgs.Id].DynamicInvoke(this, zomeFunctionCallBackArgs);

            if (_taskCompletionZomeCallBack.ContainsKey(zomeFunctionCallBackArgs.Id) && _taskCompletionZomeCallBack[zomeFunctionCallBackArgs.Id] != null)
                _taskCompletionZomeCallBack[zomeFunctionCallBackArgs.Id].SetResult(zomeFunctionCallBackArgs);

            OnZomeFunctionCallBack?.Invoke(this, zomeFunctionCallBackArgs);

            // If the zome call requested for this to be cached then stick it in the cache.
            if (_cacheZomeReturnDataLookup.ContainsKey(zomeFunctionCallBackArgs.Id) && _cacheZomeReturnDataLookup[zomeFunctionCallBackArgs.Id])
                _zomeReturnDataLookup[zomeFunctionCallBackArgs.Id] = zomeFunctionCallBackArgs;

            _zomeLookup.Remove(zomeFunctionCallBackArgs.Id);
            _funcLookup.Remove(zomeFunctionCallBackArgs.Id);
            _callbackLookup.Remove(zomeFunctionCallBackArgs.Id);
            _entryDataObjectTypeLookup.Remove(zomeFunctionCallBackArgs.Id);
            _entryDataObjectLookup.Remove(zomeFunctionCallBackArgs.Id);
            _cacheZomeReturnDataLookup.Remove(zomeFunctionCallBackArgs.Id);
            _taskCompletionZomeCallBack.Remove(zomeFunctionCallBackArgs.Id);
        }

        private void RaiseAdminAgentPubKeyGeneratedEvent(AdminAgentPubKeyGeneratedCallBackEventArgs adminAgentPubKeyGeneratedCallBackEventArgs)
        {
            Logger.Log(string.Concat("Id: ", adminAgentPubKeyGeneratedCallBackEventArgs.Id, ", Is Call Successful: ", adminAgentPubKeyGeneratedCallBackEventArgs.IsCallSuccessful ? "True" : "False", ", AgentPubKey: ", adminAgentPubKeyGeneratedCallBackEventArgs.AgentPubKey, ", Raw Binary Data: ", adminAgentPubKeyGeneratedCallBackEventArgs.RawBinaryData, ", Raw JSON Data: ", adminAgentPubKeyGeneratedCallBackEventArgs.RawJSONData, "\n"), LogType.Info);
            OnAdminAgentPubKeyGeneratedCallBack?.Invoke(this, adminAgentPubKeyGeneratedCallBackEventArgs);
            
            if (_taskCompletionAdminAgentPubKeyGeneratedCallBack != null && !string.IsNullOrEmpty(adminAgentPubKeyGeneratedCallBackEventArgs.Id) && _taskCompletionAdminAgentPubKeyGeneratedCallBack.ContainsKey(adminAgentPubKeyGeneratedCallBackEventArgs.Id))
                _taskCompletionAdminAgentPubKeyGeneratedCallBack[adminAgentPubKeyGeneratedCallBackEventArgs.Id].SetResult(adminAgentPubKeyGeneratedCallBackEventArgs);
        }

        private void RaiseAdminAppInstalledEvent(AdminAppInstalledCallBackEventArgs adminAppInstalledCallBackEventArgs)
        {
            Logger.Log(string.Concat("Id: ", adminAppInstalledCallBackEventArgs.Id, ", Is Call Successful: ", adminAppInstalledCallBackEventArgs.IsCallSuccessful ? "True" : "False", ", Raw Binary Data: ", adminAppInstalledCallBackEventArgs.RawBinaryData, ", Raw JSON Data: ", adminAppInstalledCallBackEventArgs.RawJSONData, "\n"), LogType.Info);
            OnAdminAppInstalledCallBack?.Invoke(this, adminAppInstalledCallBackEventArgs);

            if (_taskCompletionAdminAppInstalledCallBack != null && !string.IsNullOrEmpty(adminAppInstalledCallBackEventArgs.Id) && _taskCompletionAdminAppInstalledCallBack.ContainsKey(adminAppInstalledCallBackEventArgs.Id))
                _taskCompletionAdminAppInstalledCallBack[adminAppInstalledCallBackEventArgs.Id].SetResult(adminAppInstalledCallBackEventArgs);
        }

        private void RaiseAdminAppEnabledEvent(AdminAppEnabledCallBackEventArgs adminAppEnabledCallBackEventArgs)
        {
            Logger.Log(string.Concat("Id: ", adminAppEnabledCallBackEventArgs.Id, ", Is Call Successful: ", adminAppEnabledCallBackEventArgs.IsCallSuccessful ? "True" : "False", ", Raw Binary Data: ", adminAppEnabledCallBackEventArgs.RawBinaryData, ", Raw JSON Data: ", adminAppEnabledCallBackEventArgs.RawJSONData, "\n"), LogType.Info);
            OnAdminAppEnabledCallBack?.Invoke(this, adminAppEnabledCallBackEventArgs);

            if (_taskCompletionAdminAppEnabledCallBack != null && !string.IsNullOrEmpty(adminAppEnabledCallBackEventArgs.Id) && _taskCompletionAdminAppEnabledCallBack.ContainsKey(adminAppEnabledCallBackEventArgs.Id))
                _taskCompletionAdminAppEnabledCallBack[adminAppEnabledCallBackEventArgs.Id].SetResult(adminAppEnabledCallBackEventArgs);
        }

        private void RaiseAdminAppDisabledEvent(AdminAppDisabledCallBackEventArgs adminAppDisabledCallBackEventArgs)
        {
            Logger.Log(string.Concat("Id: ", adminAppDisabledCallBackEventArgs.Id, ", Is Call Successful: ", adminAppDisabledCallBackEventArgs.IsCallSuccessful ? "True" : "False", ", Raw Binary Data: ", adminAppDisabledCallBackEventArgs.RawBinaryData, ", Raw JSON Data: ", adminAppDisabledCallBackEventArgs.RawJSONData, "\n"), LogType.Info);
            OnAdminAppDisabledCallBack?.Invoke(this, adminAppDisabledCallBackEventArgs);

            if (_taskCompletionAdminAppDisabledCallBack != null && !string.IsNullOrEmpty(adminAppDisabledCallBackEventArgs.Id) && _taskCompletionAdminAppDisabledCallBack.ContainsKey(adminAppDisabledCallBackEventArgs.Id))
                _taskCompletionAdminAppDisabledCallBack[adminAppDisabledCallBackEventArgs.Id].SetResult(adminAppDisabledCallBackEventArgs);
        }

        private void SetReadyForZomeCalls()
        {
            RetrievingAgentPubKeyAndDnaHash = false;
            _taskCompletionAgentPubKeyAndDnaHashRetrieved.SetResult(new AgentPubKeyDnaHash() { AgentPubKey = Config.AgentPubKey, DnaHash = Config.DnaHash });

            IsReadyForZomesCalls = true;
            ReadyForZomeCallsEventArgs eventArgs = new ReadyForZomeCallsEventArgs(EndPoint, Config.DnaHash, Config.AgentPubKey);
            OnReadyForZomeCalls?.Invoke(this, eventArgs);
            _taskCompletionReadyForZomeCalls.SetResult(eventArgs);
        }

        private string GetItemFromCache(string id, Dictionary<string, string> cache)
        {
            return cache.ContainsKey(id) ? cache[id] : null;
        }

        private void HandleError(string message, Exception exception = null)
        {
            message = string.Concat(message, exception != null ? $". Error Details: {exception}" : "");
            Logger.Log(message, LogType.Error);

            OnError?.Invoke(this, new HoloNETErrorEventArgs { EndPoint = WebSocket.EndPoint, Reason = message, ErrorDetails = exception });

            switch (Config.ErrorHandlingBehaviour)
            {
                case ErrorHandlingBehaviour.AlwaysThrowExceptionOnError:
                    throw new HoloNETException(message, exception, WebSocket.EndPoint);

                case ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent:
                    {
                        if (OnError == null)
                            throw new HoloNETException(message, exception, WebSocket.EndPoint);
                    }
                    break;
            }
        }
    }
}
