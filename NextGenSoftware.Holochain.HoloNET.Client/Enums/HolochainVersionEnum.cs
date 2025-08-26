namespace NextGenSoftware.Holochain.HoloNET.Client
{
    /// <summary>
    /// Defines the supported versions of Holochain for protocol compatibility.
    /// </summary>
    public enum HolochainVersion
    {
        /// <summary>
        /// Legacy Holochain version (0.0.x series) - Redux protocol
        /// </summary>
        Redux,
        
        /// <summary>
        /// RSM version (0.0.x series) - RSM protocol
        /// </summary>
        RSM,
        
        /// <summary>
        /// Latest stable Holochain version (0.5.2) - JSON-RPC 2.0 protocol
        /// </summary>
        Holochain_0_5_2
    }
}
