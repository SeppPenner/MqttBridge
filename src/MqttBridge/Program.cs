// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Hämmer Electronics">
//   Copyright (c) 2020 All rights reserved.
// </copyright>
// <summary>
//   The main program.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MqttBridge;

/// <summary>
///     The main program.
/// </summary>
public class Program
{
    /// <summary>
    /// The logger.
    /// </summary>
    private static readonly ILogger Logger = Log.ForContext<Program>();

    /// <summary>
    /// The MQTT client.
    /// </summary>
    private static IMqttClient mqttClient = new MqttFactory().CreateMqttClient();

    /// <summary>
    /// The MQTT client options.
    /// </summary>
    private static IMqttClientOptions options = new MqttClientOptionsBuilder().Build();

    /// <summary>
    ///     The main method that starts the service.
    /// </summary>
    public static void Main()
    {
        var currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(Path.Combine(currentPath,
                @"log\MqttBridge_.txt"), rollingInterval: RollingInterval.Day)
            .WriteTo.Console()
            .CreateLogger();

        var config = ReadConfiguration(currentPath) ?? new();

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

        var optionsBuilder = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint().WithApplicationMessageInterceptor(
                async c =>
                {
                    await mqttClient.PublishAsync(c.ApplicationMessage, CancellationToken.None);

                    c.AcceptPublish = true;
                    LogMessage(c);
                });

        var mqttServer = new MqttFactory().CreateMqttServer();
        mqttServer.StartAsync(optionsBuilder.Build());
        Console.ReadLine();
    }

    /// <summary>
    /// Connects the MQTT client.
    /// </summary>
    private static async Task ConnectMqttClient()
    {
        await mqttClient.ConnectAsync(options, CancellationToken.None);
    }

    /// <summary>
    ///     Reads the configuration.
    /// </summary>
    /// <param name="currentPath">The current path.</param>
    /// <returns>A <see cref="Config" /> object.</returns>
    private static Config? ReadConfiguration(string currentPath)
    {
        var filePath = $"{currentPath}\\config.json";

        Config? config = null;

        if (File.Exists(filePath))
        {
            using var r = new StreamReader(filePath);
            var json = r.ReadToEnd();
            config = JsonConvert.DeserializeObject<Config?>(json);
        }

        return config;
    }

    /// <summary>
    ///     Logs the message from the MQTT message interceptor context.
    /// </summary>
    /// <param name="context">The MQTT message interceptor context.</param>
    private static void LogMessage(MqttApplicationMessageInterceptorContext context)
    {
        if (context is null)
        {
            return;
        }

        var payload = context.ApplicationMessage?.Payload is null ? null : Encoding.UTF8.GetString(context.ApplicationMessage.Payload);

        Logger.Information(
            "Message: ClientId = {clientId}, Topic = {topic}, Payload = {payload}, QoS = {qos}, Retain-Flag = {retainFlag}",
            context.ClientId,
            context.ApplicationMessage?.Topic,
            payload,
            context.ApplicationMessage?.QualityOfServiceLevel,
            context.ApplicationMessage?.Retain);
    }
}
