// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.Extensions.Azure;

namespace Microsoft.BotBuilderSamples
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        config.AddJsonFile("appsettings.json",
                            optional: true,
                            reloadOnChange: true).AddEnvironmentVariables();
                    });
                    webBuilder.ConfigureLogging((logging) =>
                    {
                        logging.AddDebug();
                        logging.AddConsole();
                        logging.AddOpenTelemetry(options=>{
                            options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("TelemetryApp"));
                            options.AddAzureMonitorLogExporter(o=>{
                                o.ConnectionString = "InstrumentationKey=556f60a4-79d1-493d-a476-866cefddb6f3;IngestionEndpoint=https://centralus-2.in.applicationinsights.azure.com/;LiveEndpoint=https://centralus.livediagnostics.monitor.azure.com/;ApplicationId=1bf7d614-2a59-4371-856b-e0cbb17b8b1d";

                            });
                            options.IncludeFormattedMessage = true;
                        });
                    });
                    webBuilder.UseStartup<Startup>();
                });
    }
}
