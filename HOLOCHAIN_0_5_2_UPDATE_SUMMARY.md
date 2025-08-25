# HoloNET Holochain 0.5.2 Update Summary

## Overview
This document summarizes the changes made to update HoloNET to support the latest Holochain version 0.5.2 (released May 8, 2025).

## Key Changes Made

### 1. Updated HolochainVersion Enum
**File**: `Archived/NextGenSoftware.Holochain.HoloNET.Client.Core/Enums/HolochainVersionEnum.cs`

- Added new enum value `Holochain_0_5_2` for the latest version
- Organized versions chronologically with comments

```csharp
public enum HolochainVersion
{
    Redux,           // Legacy version (0.0.x series)
    RSM,             // RSM version (0.0.x series) 
    Holochain_0_5_2  // Latest stable version (0.5.2)
}
```

### 2. Enhanced HoloNETClientBase
**File**: `Archived/NextGenSoftware.Holochain.HoloNET.Client.Core/HoloNETClientBase.cs`

#### Added Support for Holochain 0.5.2 in WebSocket_OnDataReceived
- Implemented JSON-RPC 2.0 protocol handling for the new version
- Added proper error handling for Holochain 0.5.2 responses
- Enhanced signal handling for the new protocol
- Added comprehensive logging for debugging

#### Added Support for Holochain 0.5.2 in CallZomeFunctionAsync
- Implemented new request format for Holochain 0.5.2
- Updated cell_id structure to use string arrays instead of byte arrays
- Added proper JSON-RPC 2.0 request formatting

#### Added Support for Holochain 0.5.2 in GetHolochainInstancesAsync
- Updated request format to include proper params object
- Enhanced logging for the new version

### 3. Updated HoloNETDNA Class
**File**: `holochain-client-csharp/NextGenSoftware.Holochain.HoloNET.Client.Core/HoloNET DNA/HoloNETDNA.cs`

- Added `HolochainVersion` property with default value `Holochain_0_5_2`
- Added XML documentation for the new property

### 4. Updated IHoloNETDNA Interface
**File**: `holochain-client-csharp/NextGenSoftware.Holochain.HoloNET.Client.Core/Interfaces/IHoloNETDNA.cs`

- Added `HolochainVersion` property to the interface
- Added necessary using statement for the enum

### 5. Enhanced HoloOASIS Provider
**File**: `NextGenSoftware.OASIS.API.Providers.HoloOASIS/HoloOASIS.cs`

- Added new constructor that accepts `HolochainVersion` parameter
- Allows users to specify the Holochain version when creating a HoloOASIS instance

### 6. Updated Test Harness
**File**: `NextGenSoftware.OASIS.API.Providers.HoloOASIS.TestHarness/Program.cs`

- Updated to use the new Holochain 0.5.2 version by default
- Added necessary using statement for the enum

## Protocol Changes for Holochain 0.5.2

### JSON-RPC 2.0 Protocol
Holochain 0.5.2 uses a more standardized JSON-RPC 2.0 protocol compared to previous versions:

#### Request Format
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "method": "call",
  "params": {
    "cell_id": ["dna_hash", "agent_pub_key"],
    "zome_name": "zome_name",
    "fn_name": "function_name",
    "payload": "payload_data",
    "provenance": "agent_pub_key",
    "cap": null
  }
}
```

#### Response Format
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "result": "result_data"
}
```

#### Error Response Format
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "error": {
    "code": -1,
    "message": "error_message"
  }
}
```

## Breaking Changes

1. **Cell ID Format**: Changed from byte arrays to string arrays
2. **Request Structure**: Updated to use JSON-RPC 2.0 format
3. **Response Handling**: Enhanced error handling and response parsing

## Migration Guide

### For Existing Users

1. **Update Constructor Calls**: If you want to use the new version, update your HoloOASIS constructor calls:
   ```csharp
   // Old way
   var holoOASIS = new HoloOASIS("ws://localhost:8888");
   
   // New way (explicit version)
   var holoOASIS = new HoloOASIS("ws://localhost:8888", HolochainVersion.Holochain_0_5_2);
   ```

2. **Default Behavior**: The new version defaults to `Holochain_0_5_2`, so existing code will automatically use the latest version.

3. **Backward Compatibility**: The code maintains backward compatibility with Redux and RSM versions.

## Testing

The test harness has been updated to use the new version by default. To test:

1. Ensure you have Holochain 0.5.2 installed
2. Run the test harness: `NextGenSoftware.OASIS.API.Providers.HoloOASIS.TestHarness`
3. Verify that connections and zome calls work correctly

## Future Considerations

1. **Version Detection**: Consider implementing automatic version detection based on conductor response
2. **Protocol Negotiation**: Add support for protocol negotiation between client and conductor
3. **Performance Optimization**: Optimize the new JSON-RPC 2.0 implementation for better performance

## Dependencies

- Holochain 0.5.2 or later
- .NET Framework 4.7.2 or later
- Newtonsoft.Json for JSON serialization
- MessagePack for RSM version compatibility

## Notes

- The update maintains full backward compatibility with existing Redux and RSM versions
- All existing functionality should continue to work as before
- The new version provides better error handling and more standardized protocol
- Enhanced logging has been added for better debugging capabilities
