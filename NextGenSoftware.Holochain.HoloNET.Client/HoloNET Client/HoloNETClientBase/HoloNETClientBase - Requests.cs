using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using MessagePack;
using NextGenSoftware.Logging;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using System.Text.Json;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    public abstract partial class HoloNETClientBase : IHoloNETClientBase
    {

        /// <summary>
        /// This method allows you to send your own raw request to holochain. This method raises the OnDataRecived event once it has received a response from the Holochain conductor.
        /// </summary>
        /// <param name="id">The id of the request to send to the Holochain Conductor. This will be matched to the id in the response received from the Holochain Conductor.</param>
        /// <param name="holoNETData">The raw data packet you wish to send to the Holochain conductor.</param>
        /// <returns></returns>
        public virtual async Task SendHoloNETRequestAsync(HoloNETData holoNETData, HoloNETRequestType requestType, string id = "")
        {
            // Check if we should use JSON-RPC 2.0 protocol (Holochain 0.5.2)
            if (HoloNETDNA?.HolochainVersion == HolochainVersion.Holochain_0_5_2)
            {
                await SendHoloNETRequestAsyncHolochain052(holoNETData, requestType, id);
            }
            else
            {
                // Use legacy MessagePack protocol (Redux/RSM)
                await SendHoloNETRequestAsync(MessagePackSerializer.Serialize(holoNETData), requestType, id);
            }
        }

        /// <summary>
        /// This method allows you to send your own raw request to holochain. This method raises the OnDataRecived event once it has received a response from the Holochain conductor.
        /// </summary>
        /// <param name="id">The id of the request to send to the Holochain Conductor. This will be matched to the id in the response received from the Holochain Conductor.</param>
        /// <param name="holoNETData">The raw data packet you wish to send to the Holochain conductor.</param>
        public virtual void SendHoloNETRequest(HoloNETData holoNETData, HoloNETRequestType requestType, string id = "")
        {
            SendHoloNETRequestAsync(holoNETData, requestType, id);
        }

        /// <summary>
        /// This method allows you to send your own raw request to holochain. This method raises the OnDataRecived event once it has received a response from the Holochain conductor.
        /// </summary>
        /// <param name="id">The id of the request to send to the Holochain Conductor. This will be matched to the id in the response received from the Holochain Conductor.</param>
        /// <param name="holoNETData">The raw data packet you wish to send to the Holochain conductor.</param>
        public virtual async Task SendHoloNETRequestAsync(byte[] data, HoloNETRequestType requestType, string id = "")
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

                if (HoloNETDNA.EnforceRequestToResponseIdMatchingBehaviour != EnforceRequestToResponseIdMatchingBehaviour.Ignore)
                    _pendingRequests.Add(id);

                _requestTypeLookup[id] = requestType;

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
        public virtual void SendHoloNETRequest(byte[] data, HoloNETRequestType requestType, string id = "")
        {
            SendHoloNETRequestAsync(data, requestType, id);
        }

        protected virtual string GetRequestId()
        {
            _currentId++;
            return _currentId.ToString();
        }

        /// <summary>
        /// Sends a request using JSON-RPC 2.0 protocol (Holochain 0.5.2)
        /// </summary>
        protected virtual async Task SendHoloNETRequestAsyncHolochain052(HoloNETData holoNETData, HoloNETRequestType requestType, string id = "")
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    id = GetRequestId();

                // Create JSON-RPC 2.0 request
                var jsonRpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = id,
                    method = GetJsonRpcMethod(requestType),
                    @params = new
                    {
                        data = holoNETData
                    }
                };

                // Serialize to JSON
                string jsonString = JsonSerializer.Serialize(jsonRpcRequest, new JsonSerializerOptions 
                { 
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                Logger.Log($"JSON-RPC 2.0 Request: {jsonString}", LogType.Debug);

                // Convert to bytes
                byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(jsonString);

                if (HoloNETDNA.EnforceRequestToResponseIdMatchingBehaviour != EnforceRequestToResponseIdMatchingBehaviour.Ignore)
                    _pendingRequests.Add(id);

                _requestTypeLookup[id] = requestType;

                if (WebSocket.State == WebSocketState.Open)
                {
                    Logger.Log("Sending JSON-RPC 2.0 Request to Holochain Conductor...", LogType.Info, true);
                    await WebSocket.SendRawDataAsync(jsonData);
                    Logger.Log("JSON-RPC 2.0 Request Successfully Sent To Holochain Conductor.", LogType.Info, false);
                }
            }
            catch (Exception ex)
            {
                HandleError("Error occurred in HoloNETClient.SendHoloNETRequestAsyncHolochain052 method.", ex);
            }
        }

        /// <summary>
        /// Maps HoloNET request types to JSON-RPC 2.0 method names
        /// </summary>
        protected virtual string GetJsonRpcMethod(HoloNETRequestType requestType)
        {
            switch (requestType)
            {
                case HoloNETRequestType.ZomeCall:
                    return "call_zome";
                case HoloNETRequestType.AppInfo:
                    return "app_info";
                case HoloNETRequestType.AdminGenerateAgentPubKey:
                    return "admin_generate_agent_pub_key";
                case HoloNETRequestType.AdminInstallApp:
                    return "admin_install_app";
                case HoloNETRequestType.AdminUninstallApp:
                    return "admin_uninstall_app";
                case HoloNETRequestType.AdminEnableApp:
                    return "admin_enable_app";
                case HoloNETRequestType.AdminDisableApp:
                    return "admin_disable_app";
                case HoloNETRequestType.AdminGrantZomeCallCapability:
                    return "admin_grant_zome_call_capability";
                case HoloNETRequestType.AdminAttachAppInterface:
                    return "admin_attach_app_interface";
                case HoloNETRequestType.AdminListApps:
                    return "admin_list_apps";
                case HoloNETRequestType.AdminListDnas:
                    return "admin_list_dnas";
                case HoloNETRequestType.AdminListCellIds:
                    return "admin_list_cell_ids";
                case HoloNETRequestType.AdminListAppInterfaces:
                    return "admin_list_app_interfaces";
                case HoloNETRequestType.AdminRegisterDna:
                    return "admin_register_dna";
                case HoloNETRequestType.AdminGetDnaDefinition:
                    return "admin_get_dna_definition";
                case HoloNETRequestType.AdminAgentInfo:
                    return "admin_agent_info";
                case HoloNETRequestType.AdminAddAgentInfo:
                    return "admin_add_agent_info";
                default:
                    return "unknown";
            }
        }
}
}
