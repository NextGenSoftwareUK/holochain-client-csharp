﻿
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public struct HolonNETAppResponse
    {
        [Key("type")]
        public string type { get; set; }

        [Key("data")]
        public byte[] data { get; set; }
    }
}