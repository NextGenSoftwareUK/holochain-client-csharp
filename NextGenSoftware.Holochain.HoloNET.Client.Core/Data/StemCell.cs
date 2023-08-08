﻿
using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public struct StemCell : ICell
    {
        [Key("original_dna_hash")]
        public byte[] original_dna_hash { get; set; }

        [Key("dna_modifiers")]
        public byte[] dna_modifiers { get; set; } //DnaModifiers

        [Key("name")]
        //public OptionType name { get; set; } // pub name: Option<String>,
        public string name { get; set; } // pub name: Option<String>,
    }
}