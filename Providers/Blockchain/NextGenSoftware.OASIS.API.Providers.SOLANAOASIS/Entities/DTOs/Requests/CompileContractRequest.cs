namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;

/// <summary>
/// Request to compile a Solana smart contract
/// </summary>
public sealed class CompileContractRequest
{
    /// <summary>
    /// Path to source code directory (Anchor project)
    /// </summary>
    public string SourceCodePath { get; set; }
}
