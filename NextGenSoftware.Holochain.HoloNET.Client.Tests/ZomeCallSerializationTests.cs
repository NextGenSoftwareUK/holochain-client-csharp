using MessagePack;
using Xunit;

namespace NextGenSoftware.Holochain.HoloNET.Client.Tests
{
    /// <summary>
    /// Group (a): round-trip serialization tests for the core zome-call types using the exact
    /// MessagePackSerializerOptions used on the real send/receive path
    /// (see HoloNETClientBase.cs: MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData)).
    /// </summary>
    public class ZomeCallSerializationTests
    {
        // Same options object the production code uses for every Serialize/Deserialize call on
        // the wire (HoloNETClientBase.messagePackSerializerOptions).
        private static readonly MessagePackSerializerOptions Options =
            MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);

        // 39 bytes - illustrative/arbitrary-but-realistic length for a Holochain hash
        // (real Holochain hashes are 39 bytes: 3-byte prefix + 32-byte hash + 4-byte DHT location).
        // Not asserting Holochain's exact hash format, just a plausible fixed-length byte array.
        private static byte[] MakeHashLikeBytes(byte seed)
        {
            var bytes = new byte[39];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)(seed + i);
            return bytes;
        }

        [Fact]
        public void CellId_RoundTrips_AllFields()
        {
            var original = new CellId(MakeHashLikeBytes(1), MakeHashLikeBytes(50));

            byte[] bytes = MessagePackSerializer.Serialize(original, Options);
            CellId result = MessagePackSerializer.Deserialize<CellId>(bytes, Options);

            Assert.Equal(original.dna_hash, result.dna_hash);
            Assert.Equal(original.agent_pub_key, result.agent_pub_key);
        }

        [Fact]
        public void ZomeCall_RoundTrips_AllFields()
        {
            var original = new ZomeCall
            {
                provenance = MakeHashLikeBytes(10),
                cell_id = new CellId(MakeHashLikeBytes(1), MakeHashLikeBytes(50)),
                zome_name = "my_zome",
                fn_name = "my_fn",
                cap_secret = new byte[] { 1, 2, 3, 4, 5 },
                payload = new byte[] { 9, 8, 7, 6, 5, 4, 3, 2, 1 },
                nonce = new byte[] { 11, 22, 33, 44, 55, 66, 77, 88, 99, 100 },
                expires_at = 1735689600000000L
            };

            byte[] bytes = MessagePackSerializer.Serialize(original, Options);
            ZomeCall result = MessagePackSerializer.Deserialize<ZomeCall>(bytes, Options);

            Assert.Equal(original.provenance, result.provenance);
            Assert.Equal(original.cell_id.dna_hash, result.cell_id.dna_hash);
            Assert.Equal(original.cell_id.agent_pub_key, result.cell_id.agent_pub_key);
            Assert.Equal(original.zome_name, result.zome_name);
            Assert.Equal(original.fn_name, result.fn_name);
            Assert.Equal(original.cap_secret, result.cap_secret);
            Assert.Equal(original.payload, result.payload);
            Assert.Equal(original.nonce, result.nonce);
            Assert.Equal(original.expires_at, result.expires_at);
        }

        [Fact]
        public void ZomeCall_RoundTrips_WithNullCapSecret()
        {
            // cap_secret is Option<CapSecret> on the Rust side; null is a valid/common value.
            var original = new ZomeCall
            {
                provenance = MakeHashLikeBytes(10),
                cell_id = new CellId(MakeHashLikeBytes(1), MakeHashLikeBytes(50)),
                zome_name = "my_zome",
                fn_name = "my_fn",
                cap_secret = null,
                payload = new byte[] { 1 },
                nonce = new byte[] { 2 },
                expires_at = 0L
            };

            byte[] bytes = MessagePackSerializer.Serialize(original, Options);
            ZomeCall result = MessagePackSerializer.Deserialize<ZomeCall>(bytes, Options);

            Assert.Null(result.cap_secret);
            Assert.Equal(original.expires_at, result.expires_at);
        }

        [Fact]
        public void ZomeCallSigned_RoundTrips_AllFields_IncludingInheritedZomeCallFields()
        {
            var original = new ZomeCallSigned
            {
                provenance = MakeHashLikeBytes(10),
                cell_id = new CellId(MakeHashLikeBytes(1), MakeHashLikeBytes(50)),
                zome_name = "my_zome",
                fn_name = "my_fn",
                cap_secret = new byte[] { 1, 2, 3 },
                payload = new byte[] { 4, 5, 6 },
                nonce = new byte[] { 7, 8, 9 },
                expires_at = 42L,
                signature = MakeHashLikeBytes(64)
            };

            byte[] bytes = MessagePackSerializer.Serialize(original, Options);
            ZomeCallSigned result = MessagePackSerializer.Deserialize<ZomeCallSigned>(bytes, Options);

            Assert.Equal(original.provenance, result.provenance);
            Assert.Equal(original.cell_id.dna_hash, result.cell_id.dna_hash);
            Assert.Equal(original.cell_id.agent_pub_key, result.cell_id.agent_pub_key);
            Assert.Equal(original.zome_name, result.zome_name);
            Assert.Equal(original.fn_name, result.fn_name);
            Assert.Equal(original.cap_secret, result.cap_secret);
            Assert.Equal(original.payload, result.payload);
            Assert.Equal(original.nonce, result.nonce);
            Assert.Equal(original.expires_at, result.expires_at);
            Assert.Equal(original.signature, result.signature);
        }

        [Fact]
        public void ZomeCallParamsSigned_RoundTrips_AllFields()
        {
            // bytes here represents holochain_serialized_bytes::encode(&ZomeCallParams) - for the
            // purposes of this test it is simply an opaque byte payload being round-tripped.
            var original = new ZomeCallParamsSigned(
                bytes: new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                signature: MakeHashLikeBytes(64));

            byte[] serialized = MessagePackSerializer.Serialize(original, Options);
            ZomeCallParamsSigned result = MessagePackSerializer.Deserialize<ZomeCallParamsSigned>(serialized, Options);

            Assert.Equal(original.bytes, result.bytes);
            Assert.Equal(original.signature, result.signature);
        }

        [Fact]
        public void ZomeCallParamsSigned_RoundTrips_DefaultConstructor()
        {
            var original = new ZomeCallParamsSigned
            {
                bytes = new byte[] { 0xAA, 0xBB },
                signature = new byte[] { 0xCC, 0xDD }
            };

            byte[] serialized = MessagePackSerializer.Serialize(original, Options);
            ZomeCallParamsSigned result = MessagePackSerializer.Deserialize<ZomeCallParamsSigned>(serialized, Options);

            Assert.Equal(original.bytes, result.bytes);
            Assert.Equal(original.signature, result.signature);
        }
    }
}
