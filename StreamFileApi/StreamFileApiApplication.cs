using System.Net;
using HttpMultipartParser;
using Microsoft.AspNetCore.Http.Features;
using Vostok.Applications.AspNetCore;
using Vostok.Applications.AspNetCore.Builders;
using Vostok.Hosting.Abstractions;

namespace StreamFileApi;

public class StreamFileApiApplication : VostokAspNetCoreWebApplication
{
    public override Task SetupAsync(IVostokAspNetCoreWebApplicationBuilder builder, IVostokHostingEnvironment environment)
    {
        builder.SetupWebApplication(builder =>
        {
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddVostokTracing(setup => setup.ResponseTraceIdHeader = "TraceId");
            builder.Services.AddVostokRequestInfo(setup => setup.DefaultTimeoutProvider = _ => TimeSpan.FromSeconds(30));
            builder.Services.AddVostokRequestLogging(setup =>
            {
                setup.LogRequestHeaders = true;
                setup.LogResponseHeaders = true;
                setup.LogResponseCompletion = true;
            });
            builder.Services.Configure<FormOptions>(options =>
            {
                options.BufferBody = false;
                options.ValueLengthLimit = int.MaxValue;
                options.MultipartBodyLengthLimit = long.MaxValue;
                options.MultipartHeadersLengthLimit = int.MaxValue;
            });
        });

        builder.CustomizeWebApplication(app =>
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseVostokTracing();
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.UseRouting();
            app.UseVostokRequestInfo();
            app.UseVostokUnhandledExceptions();
            app.UseResponseExceptionHandler(setup =>
            {
                setup.Map<MultipartParseException>(HttpStatusCode.BadRequest, "Invalid or empty content provided.");
            });
        });

        return Task.CompletedTask;
    }

    public override Task WarmupAsync(IVostokHostingEnvironment environment, WebApplication webApplication)
    {
        return Task.CompletedTask;
    }
}