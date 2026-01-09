using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using NextGenSoftware.OASIS.Common;
using Solnet.Metaplex;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SolanaController : OASISControllerBase
    {
        private readonly ISolanaService _solanaService;

        public SolanaController(ISolanaService solanaService)
        {
            _solanaService = solanaService;
        }

        /// <summary>
        /// Mint NFT (non-fungible token)
        /// </summary>
        /// <param name="request">Mint Public Key Account, and Mint Decimals for Mint NFT</param>
        /// <returns>Mint NFT Transaction Hash</returns>
        [HttpPost]
        [Route("Mint")]
        public async Task<OASISResult<MintNftResult>> MintNft([FromBody] MintWeb3NFTRequest request)
        {
            return await _solanaService.MintNftAsync(request);
        }

        /// <summary>
        /// Handles a transaction between accounts with a specific Lampposts size
        /// </summary>
        /// <param name="request">FromAccount(Public Key) and ToAccount(Public Key)
        /// between which the transaction will be carried out</param>
        /// <returns>Send Transaction Hash</returns>
        [HttpPost]
        [Route("Send")]
        public async Task<OASISResult<SendTransactionResult>> SendTransaction([FromBody] SendTransactionRequest request)
        {
            return await _solanaService.SendTransaction(request);
        }

        /// <summary>
        /// Compile a Solana smart contract (Anchor program)
        /// </summary>
        /// <param name="sourceCodeFile">Source code file (ZIP archive containing Anchor project)</param>
        /// <returns>Compilation result with program path and IDL</returns>
        [HttpPost]
        [Route("CompileContract")]
        public async Task<OASISResult<CompileContractResult>> CompileContract(IFormFile sourceCodeFile)
        {
            if (sourceCodeFile == null || sourceCodeFile.Length == 0)
            {
                return new OASISResult<CompileContractResult>
                {
                    IsError = true,
                    Message = "Source code file is required"
                };
            }

            // Save uploaded file to temp location
            string tempZipPath = Path.Combine(Path.GetTempPath(), $"oasis_compile_{Guid.NewGuid()}.zip");
            await using (var stream = new FileStream(tempZipPath, FileMode.Create))
            {
                await sourceCodeFile.CopyToAsync(stream);
            }

            try
            {
                var request = new CompileContractRequest
                {
                    SourceCodePath = tempZipPath
                };
                
                return await _solanaService.CompileContractAsync(request);
            }
            finally
            {
                // Clean up temp file
                try
                {
                    if (System.IO.File.Exists(tempZipPath))
                        System.IO.File.Delete(tempZipPath);
                }
                catch { /* Ignore cleanup errors */ }
            }
        }
    }
}