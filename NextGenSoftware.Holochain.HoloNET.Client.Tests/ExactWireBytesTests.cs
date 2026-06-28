using System.Collections.Generic;
using MessagePack;
using Xunit;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests;

namespace NextGenSoftware.Holochain.HoloNET.Client.Tests
{
    /// <summary>
    /// Group (d): exact-wire-bytes tests. These hand-construct the expected MessagePack byte
    /// sequence (based on the [Key] attribute names/positions read from the source classes) for a
    /// known set of field values, and assert the serializer produces exactly those bytes. This is
    /// a stronger guarantee than a round-trip test because it independently locks in the wire
    /// format rather than just checking serialize(deserialize(x)) == x.
    /// </summary>
    public class ExactWireBytesTests
    {
        private static readonly MessagePackSerializerOptions Options =
            MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);

        /// <summary>
        /// CellId uses positional integer [Key(0)]/[Key(1)], so MessagePack encodes it as a
        /// fixarray of length 2: [dna_hash, agent_pub_key]. With two small byte arrays (3 bytes
        /// each, well under the 31-byte threshold for fixstr/the bin8 8-bit-length threshold of
        /// 255 used by bin8), each array is encoded as: bin8 header (0xC4), 1-byte length, then
        /// the raw bytes. The outer 2-element array uses a fixarray header (0x92 = fixarray of
        /// size 2).
        /// </summary>
        [Fact]
        public void CellId_ProducesExactExpectedBytes()
        {
            var cellId = new CellId(
                dnaHash: new byte[] { 0x01, 0x02, 0x03 },
                agentPubKey: new byte[] { 0x04, 0x05, 0x06 });

            byte[] actual = MessagePackSerializer.Serialize(cellId, Options);

            byte[] expected =
            {
                0x92,                   // fixarray, 2 elements
                0xC4, 0x03, 0x01, 0x02, 0x03, // bin8, length 3, dna_hash bytes
                0xC4, 0x03, 0x04, 0x05, 0x06  // bin8, length 3, agent_pub_key bytes
            };

            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// RevokeAppAuthenticationTokenRequest has a single named [Key("token")] byte[] field, so
        /// it is encoded as a fixmap with 1 entry: { "token": &lt;bin8 bytes&gt; }. The string key
        /// "token" (5 ASCII chars) is encoded as a fixstr (0xA5 = fixstr of length 5).
        /// </summary>
        [Fact]
        public void RevokeAppAuthenticationTokenRequest_ProducesExactExpectedBytes()
        {
            var request = new RevokeAppAuthenticationTokenRequest
            {
                token = new byte[] { 0xAA, 0xBB }
            };

            byte[] actual = MessagePackSerializer.Serialize(request, Options);

            byte[] expected =
            {
                0x81,                               // fixmap, 1 entry
                0xA5, (byte)'t', (byte)'o', (byte)'k', (byte)'e', (byte)'n', // fixstr "token"
                0xC4, 0x02, 0xAA, 0xBB              // bin8, length 2, token bytes
            };

            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// ListCapabilityGrantsRequest has two named keys: [Key("installed_app_id")] string and
        /// [Key("include_revoked")] bool. Encoded as a fixmap with 2 entries. Key insertion/
        /// declaration order in the class (installed_app_id first, then include_revoked) is
        /// preserved by MessagePack-CSharp's contract-based (reflection) resolver used by
        /// MessagePackSerializerOptions.Standard.
        ///
        /// "installed_app_id" is 16 ASCII chars -> fixstr header 0xA0 | 16 = 0xB0.
        /// "my_app" is 6 ASCII chars -> fixstr header 0xA0 | 6 = 0xA6.
        /// "include_revoked" is 15 ASCII chars -> fixstr header 0xA0 | 15 = 0xAF.
        /// bool true/false are encoded as single bytes 0xC3/0xC2 respectively.
        /// </summary>
        [Fact]
        public void ListCapabilityGrantsRequest_ProducesExactExpectedBytes()
        {
            var request = new ListCapabilityGrantsRequest
            {
                installed_app_id = "my_app",
                include_revoked = true
            };

            byte[] actual = MessagePackSerializer.Serialize(request, Options);

            var expected = new List<byte> { 0x82 }; // fixmap, 2 entries
            expected.Add(0xB0); // fixstr length 16
            expected.AddRange(System.Text.Encoding.ASCII.GetBytes("installed_app_id"));
            expected.Add(0xA6); // fixstr length 6
            expected.AddRange(System.Text.Encoding.ASCII.GetBytes("my_app"));
            expected.Add(0xAF); // fixstr length 15
            expected.AddRange(System.Text.Encoding.ASCII.GetBytes("include_revoked"));
            expected.Add(0xC3); // true

            Assert.Equal(expected.ToArray(), actual);
        }

        /// <summary>
        /// ProvideMemproofsRequest is not itself a [MessagePackObject] - it converts implicitly to
        /// Dictionary&lt;string, byte[]&gt;, which MessagePack serializes natively as a map. Rather
        /// than hand-predict exact byte output for a Dictionary (whose key enumeration order is an
        /// implementation detail not guaranteed by the BCL, making exact-byte prediction fragile/
        /// unreliable for multi-entry maps), this test instead asserts that deserializing a
        /// hand-written byte sequence produces the expected object - i.e. it locks in the wire
        /// format from the other direction, which is robust regardless of internal dictionary
        /// enumeration order.
        /// </summary>
        [Fact]
        public void ProvideMemproofsRequest_DeserializesHandWrittenBytes_ToExpectedDictionary()
        {
            // fixmap, 1 entry: { "role_one": bin8[0x01, 0x02, 0x03] }
            byte[] handWritten =
            {
                0x81,
                0xA8, (byte)'r', (byte)'o', (byte)'l', (byte)'e', (byte)'_', (byte)'o', (byte)'n', (byte)'e', // fixstr "role_one" (8 chars)
                0xC4, 0x03, 0x01, 0x02, 0x03 // bin8, length 3
            };

            var result = MessagePackSerializer.Deserialize<Dictionary<string, byte[]>>(handWritten, Options);

            Assert.Single(result);
            Assert.True(result.ContainsKey("role_one"));
            Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, result["role_one"]);
        }
    }
}
