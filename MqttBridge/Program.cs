// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Haemmer Electronics">
//   Copyright (c) 2020 All rights reserved.
// </copyright>
// <summary>
//   The main program.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MqttBridge
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading;

    using MQTTnet;
    using MQTTnet.Client.Options;
    using MQTTnet.Server;

    using Newtonsoft.Json;

    using Serilog;

    /// <summary>
    ///     The main program.
    /// </summary>
    public class Program
    {
        /// <summary>
        ///     The main method that starts the service.
        /// </summary>
        [SuppressMessage(
            "StyleCop.CSharp.DocumentationRules",
            "SA1650:ElementDocumentationMustBeSpelledCorrectly",
            Justification = "Reviewed. Suppression is OK here.")]
        public static void Main()
        {
            var currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                // ReSharper disable once AssignNullToNotNullAttribute
                .WriteTo.File(Path.Combine(currentPath,
                    @"log\MqttBridge_.txt"), rollingInterval: RollingInterval.Day)
                .WriteTo.Console()
                .CreateLogger();

            var config = ReadConfiguration(currentPath);

            var optionsBuilder = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint().WithApplicationMessageInterceptor(
                    async c =>
                    {
                        IMqttClientOptions options;

                        if (config.UseSsl)
                        {
                            options = new MqttClientOptionsBuilder()
                                .WithClientId(config.BridgeUser.ClientId)
                                .WithTcpServer(config.BridgeUrl, config.BridgePort)
                                .WithCredentials(config.BridgeUser.UserName, config.BridgeUser.Password)
                                .WithTls()
                                .WithCleanSession()
                                .Build();
                        }
                        else
                        {
                            options = new MqttClientOptionsBuilder()
                                .WithClientId(config.BridgeUser.ClientId)
                                .WithTcpServer(config.BridgeUrl, config.BridgePort)
                                .WithCredentials(config.BridgeUser.UserName, config.BridgeUser.Password)
                                .WithCleanSession()
                                .Build();
                        }

                        var mqttClient = new MqttFactory().CreateMqttClient();
                        await mqttClient.ConnectAsync(options, CancellationToken.None);
                        await mqttClient.PublishAsync(c.ApplicationMessage, CancellationToken.None);
                        await mqttClient.DisconnectAsync(null, CancellationToken.None);

                        c.AcceptPublish = true;
                        LogMessage(c);
                    });

            var mqttServer = new MqttFactory().CreateMqttServer();
            mqttServer.StartAsync(optionsBuilder.Build());
            Console.ReadLine();
        }

        /// <summary>
        ///     Reads the configuration.
        /// </summary>
        /// <param name="currentPath">The current path.</param>
        /// <returns>A <see cref="Config" /> object.</returns>
        private static Config ReadConfiguration(string currentPath)
        {
            var filePath = $"{currentPath}\\config.json";

            Config config = null;

            // ReSharper disable once InvertIf
            if (File.Exists(filePath))
            {
                using var r = new StreamReader(filePath);
                var json = r.ReadToEnd();
                config = JsonConvert.DeserializeObject<Config>(json);
            }

            return config;
        }

        /// <summary>
        ///     Logs the message from the MQTT message interceptor context.
        /// </summary>
        /// <param name="context">The MQTT message interceptor context.</param>
        private static void LogMessage(MqttApplicationMessageInterceptorContext context)
        {
            if (context == null)
            {
                return;
            }

            var payload = context.ApplicationMessage?.Payload == null ? null : Encoding.UTF8.GetString(context.ApplicationMessage?.Payload);

            Log.Information(
                $"Message: ClientId = {context.ClientId}, Topic = {context.ApplicationMessage?.Topic},"
                + $" Payload = {payload}, QoS = {context.ApplicationMessage?.QualityOfServiceLevel},"
                + $" Retain-Flag = {context.ApplicationMessage?.Retain}");
        }
    }
}