﻿
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class HoloNETAdminUpdateCoordinatorsRequest
    {
        [Key("dnaHash")]
        public byte[] dnaHash { get; set; }

        [Key("source")]
        public CoordinatorSource source { get; set; }
    }
}