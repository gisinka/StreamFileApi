using System.Security.Cryptography;
using HttpMultipartParser;
using Microsoft.AspNetCore.Mvc;
using StreamFileApi.Model.Requests;
using StreamFileApi.Model.Responses;

namespace StreamFileApi.Controllers;

[ApiController]
[Route("stream")]
public class StreamFileController : ControllerBase
{
    [HttpPost("hash")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> GetMultipartHash()
    {
        return Ok(await GetHashFromMultipart());
    }

    [HttpPost("hashslow")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> GetHash([FromForm] HashRequest request)
    {
        await using var stream = request.File.OpenReadStream(); 
        return Ok(new HashResponse
        {
            Filename = request.File.FileName, 
            Hash = Convert.ToHexString(await GetStreamHash(stream))
        });
    }

    [HttpPost("hashbody")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> GetBodyHash([FromQuery] string? filename)
    {
        await using var stream = new BufferedStream(Request.Body); 
        return Ok(new HashResponse
        {
            Filename = filename ?? nameof(Request.Body), 
            Hash = Convert.ToHexString(await GetStreamHash(stream))
        });
    }

    private static async Task<byte[]> GetStreamHash(Stream bufferedStream)
    {
        using var hasher = SHA1.Create();
        return await hasher.ComputeHashAsync(bufferedStream);
    }

    private async Task<HashResponse> GetHashFromMultipart()
    {
        await using var bufferedStream = new BufferedStream(HttpContext.Request.Body);
        var parser = new StreamingMultipartFormDataParser(bufferedStream);
        using var hasher = SHA1.Create();
        parser.ParameterHandler += _ => { };
        var modelFilename = (string?)null;
        parser.FileHandler += (name, fileName, type, disposition, buffer, bytes, partNumber, additionalProperties) =>
        {
            modelFilename ??= fileName;
            if (bytes > 0)
            {
                hasher.TransformBlock(buffer, 0, bytes, null, 0);
            }
        };
        parser.StreamClosedHandler += () => { };
        await parser.RunAsync().ConfigureAwait(false);
        hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        var result = new HashResponse
        {
            Filename = modelFilename,
            Hash = Convert.ToHexString(hasher.Hash)
        };
        return result;
    }
}