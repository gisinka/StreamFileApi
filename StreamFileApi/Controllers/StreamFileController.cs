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
    public async Task<IActionResult> GetHash()
    {
        return Ok(await GetHashFromMultipart());
    }

    [HttpPost("hashslow")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> GetHash([FromForm] HashRequest request)
    {
        await using var bufferedStream = new BufferedStream(request.File.OpenReadStream());
        using var hasher = SHA1.Create();
        var hash = await hasher.ComputeHashAsync(bufferedStream);
        return Ok(new HashResponse {Filename = request.File.FileName, Hash = Convert.ToHexString(hash)});
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