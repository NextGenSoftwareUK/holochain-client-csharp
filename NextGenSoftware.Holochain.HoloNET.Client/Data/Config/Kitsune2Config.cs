namespace NextGenSoftware.Holochain.HoloNET.Client
{
    /// <summary>
    /// Mirrors the Kitsune2-related fields of holochain_conductor_api::config::conductor::NetworkConfig
    /// from Holochain 0.6.1 (the fields NOT already covered by NextGenSoftware.Holochain.HoloNET.Client.NetworkConfig,
    /// which only mirrors RequestTimeoutS). Kitsune2 is the real, current P2P networking crate used by
    /// Holochain (successor to kitsune_p2p), but it is configured as part of the conductor's
    /// NetworkConfig struct, not via a separate nested Kitsune2/Agent/Signature/Transport object graph
    /// as a previous, unverified pass on this file invented (BootstrapNodes list, NetworkSpace string,
    /// AgentConfig.PublicKey/PrivateKey, SignatureConfig, TransportConfig.Port/BindAddress - none of
    /// these exist in the real struct).
    ///
    /// The real NetworkConfig exposes: bootstrap_url, signal_url, relay_url (all single URLs, not a
    /// node list), request_timeout_s, webrtc_config (opaque JSON), target_arc_factor, report
    /// (reporting config), and an "advanced" opaque JSON blob for direct kitsune2 tuning that the
    /// maintainers explicitly say to only use "if you know what you are doing".
    ///
    /// Verified against:
    /// https://github.com/holochain/holochain/blob/holochain-0.6.1/crates/holochain_conductor_api/src/config/conductor.rs
    /// (struct NetworkConfig, see also https://github.com/holochain/kitsune2 for the underlying crate).
    /// </summary>
    public class Kitsune2Config
    {
        /// <summary>
        /// The Kitsune2 bootstrap server to use for WAN discovery.
        /// Mirrors NetworkConfig.bootstrap_url: url2::Url2 (default
        /// "https://dev-test-bootstrap2.holochain.org").
        /// </summary>
        public string BootstrapUrl { get; set; } = "https://dev-test-bootstrap2.holochain.org";

        /// <summary>
        /// The Kitsune2 signalling server for WebRTC connections to use.
        /// Mirrors NetworkConfig.signal_url: url2::Url2 (default
        /// "wss://dev-test-bootstrap2.holochain.org").
        /// </summary>
        public string SignalUrl { get; set; } = "wss://dev-test-bootstrap2.holochain.org";

        /// <summary>
        /// The iroh relay server address used with the iroh transport.
        /// Mirrors NetworkConfig.relay_url: url2::Url2.
        /// </summary>
        public string RelayUrl { get; set; } = "https://use1-1.relay.n0.iroh-canary.iroh.link./";

        /// <summary>
        /// The Kitsune2 webrtc_config to use for connecting to peers (raw JSON passed through to
        /// kitsune2/tx5, e.g. ICE server list). Mirrors NetworkConfig.webrtc_config: Option&lt;serde_json::Value&gt;.
        /// Left as a raw JSON string here since HoloNET does not need to interpret its contents.
        /// </summary>
        public string WebrtcConfigJson { get; set; } = null;

        /// <summary>
        /// The target arc factor applied when receiving hints from kitsune2. Leave at 1 in normal
        /// operation; set to 0 for leecher nodes that do not contribute to gossip.
        /// Mirrors NetworkConfig.target_arc_factor: u32 (default 1).
        /// </summary>
        public uint TargetArcFactor { get; set; } = 1;

        /// <summary>
        /// Advanced escape hatch for directly configuring kitsune2 (raw JSON), bypassing the
        /// higher-level fields above. Mirrors NetworkConfig.advanced: Option&lt;serde_json::Value&gt;.
        /// The Rust docs explicitly warn: "use only if you know what you are doing".
        /// Not currently wired through to any websocket/conductor call by HoloNET.
        /// </summary>
        public string AdvancedJson { get; set; } = null;
    }
}
