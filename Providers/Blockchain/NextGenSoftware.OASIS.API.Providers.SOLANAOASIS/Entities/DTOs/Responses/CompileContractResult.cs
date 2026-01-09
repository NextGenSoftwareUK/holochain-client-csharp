namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;

/// <summary>
/// Result of compiling a Solana smart contract
/// </summary>
public sealed class CompileContractResult
{
    /// <summary>
    /// Whether compilation was successful
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Path to compiled program (.so file)
    /// </summary>
    public string ProgramPath { get; set; }
    
    /// <summary>
    /// Program ID (if available)
    /// </summary>
    public string ProgramId { get; set; }
    
    /// <summary>
    /// Path to IDL file (if generated)
    /// </summary>
    public string IdlPath { get; set; }
    
    /// <summary>
    /// Compilation output/errors
    /// </summary>
    public string Output { get; set; }
    
    /// <summary>
    /// Error message if compilation failed
    /// </summary>
    public string ErrorMessage { get; set; }
    
    /// <summary>
    /// Compilation duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }
}
