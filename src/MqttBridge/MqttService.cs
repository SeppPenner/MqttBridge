// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MqttService.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   The main service class of the <see cref="MqttService" />.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MqttBridge;

using Serilog.Core;

/// <inheritdoc cref="BackgroundService"/>
/// <summary>
///     The main service class of the <see cref="MqttService" />.
/// </summary>
public class MqttService : BackgroundService
{
    /// <summary>
    /// The logger.
    /// </summary>
    private readonly ILogger logger;

    /// <summary>
    /// The service name.
    /// </summary>
    private readonly string serviceName;

    /// <summary>
    /// The bytes divider. (Used to convert from bytes to kilobytes and so on).
    /// </summary>
    private static double BytesDivider => 1048576.0;

    /// <summary>
    /// The MQTT client.
    /// </summary>
    private IMqttClient? mqttClient;

    /// <summary>
    /// The MQTT client options.
    /// </summary>
    private MqttClientOptions? clientOptions;

    /// <summary>
    /// The cancellation token.
    /// </summary>
    private CancellationToken cancellationToken;

    /// <summary>
    /// The retry attempts counter.
    /// </summary>
    private int retryAttempts;

    /// <summary>
    /// Gets or sets the MQTT service configuration.
    /// </summary>
    public MqttServiceConfiguration MqttServiceConfiguration { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttService"/> class.
    /// </summary>
    /// <param name="mqttServiceConfiguration">The MQTT service configuration.</param>
    /// <param name="serviceName">The service name.</param>
    public MqttService(MqttServiceConfiguration mqttServiceConfiguration, string serviceName)
    {
        this.MqttServiceConfiguration = mqttServiceConfiguration;
        this.serviceName = serviceName;

        // Create the logger.
        this.logger = LoggerConfig.GetLoggerConfiguration(nameof(MqttService))
            .WriteTo.Sink((ILogEventSink)Log.Logger)
            .CreateLogger();
    }

    /// <inheritdoc cref="BackgroundService"/>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!this.MqttServiceConfiguration.IsValid())
        {
            throw new Exception("The configuration is invalid");
        }

        this.logger.Information("Starting service");
        this.cancellationToken = cancellationToken;
        await this.StartMqttClient();
        this.StartMqttServer();
        this.logger.Information("Service started");
        await base.StartAsync(cancellationToken);
    }

    /// <inheritdoc cref="BackgroundService"/>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
    }

    /// <inheritdoc cref="BackgroundService"/>
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Log some memory information.
                this.LogMemoryInformation();
                await Task.Delay(this.MqttServiceConfiguration.DelayInMilliSeconds, cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.Error("An error occurred: {Exception}", ex);
            }
        }
    }

    /// <summary>
    /// Validates the MQTT connection.
    /// </summary>
    /// <param name="args">The arguments.</param>
    private Task ValidateConnectionAsync(ValidatingConnectionEventArgs args)
    {
        try
        {
            var currentUser = this.MqttServiceConfiguration.Users.FirstOrDefault(u => u.UserName == args.UserName);

            if (currentUser == null)
            {
                args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                this.LogMessage(args, true);
                return Task.CompletedTask;
            }

            if (args.UserName != currentUser.UserName)
            {
                args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                this.LogMessage(args, true);
                return Task.CompletedTask;
            }

            if (args.Password != currentUser.Password)
            {
                args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                this.LogMessage(args, true);
                return Task.CompletedTask;
            }

            args.ReasonCode = MqttConnectReasonCode.Success;
            this.LogMessage(args, false);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            this.logger.Error("An error occurred: {Exception}.", ex);
            return Task.FromException(ex);
        }
    }

    /// <summary>
    /// Validates the MQTT subscriptions.
    /// </summary>
    /// <param name="args">The arguments.</param>
    private Task InterceptSubscriptionAsync(InterceptingSubscriptionEventArgs args)
    {
        try
        {
            args.ProcessSubscription = true;
            this.LogMessage(args, true);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            this.logger.Error("An error occurred: {Exception}.", ex);
            return Task.FromException(ex);
        }
    }

    /// <summary>
    /// Validates the MQTT application messages.
    /// </summary>
    /// <param name="args">The arguments.</param>
    private async Task InterceptApplicationMessagePublishAsync(InterceptingPublishEventArgs args)
    {
        try
        {
            args.ProcessPublish = true;
            await this.mqttClient!.PublishAsync(args.ApplicationMessage, this.cancellationToken);
            this.LogMessage(args);
        }
        catch (Exception ex)
        {
            this.logger.Error("An error occurred: {Exception}.", ex);
        }
    }

    /// <summary>
    /// Validates the MQTT client disconnect.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
    private async Task HandleClientDisconnectedAsync(ClientDisconnectedEventArgs args)
    {
        try
        {
            var seconds = Math.Pow(2, this.retryAttempts);
            var maxSeconds = Math.Max(seconds, 60);
            var timeToWait = TimeSpan.FromSeconds(maxSeconds);
            await Task.Delay(timeToWait, this.cancellationToken);
            await this.mqttClient!.ConnectAsync(this.clientOptions, this.cancellationToken);
            this.retryAttempts = 0;
        }
        catch (Exception ex)
        {
            this.retryAttempts++;
            this.logger.Error("An error occurred: {Exception}.", ex);
        }
    }

    /// <summary>
    /// Starts the MQTT client.
    /// </summary>
    private async Task StartMqttClient()
    {
        if (this.MqttServiceConfiguration.UseTls)
        {
            this.clientOptions = new MqttClientOptionsBuilder()
                .WithClientId(this.MqttServiceConfiguration.BridgeUser.ClientId)
                .WithTcpServer(this.MqttServiceConfiguration.BridgeUrl, this.MqttServiceConfiguration.BridgePort)
                .WithCredentials(this.MqttServiceConfiguration.BridgeUser.UserName, this.MqttServiceConfiguration.BridgeUser.Password)
                .WithTlsOptions(new MqttClientTlsOptions
                {
                    UseTls = true
                })
                .WithCleanSession()
                .Build();
        }
        else
        {
            this.clientOptions = new MqttClientOptionsBuilder()
                .WithClientId(this.MqttServiceConfiguration.BridgeUser.ClientId)
                .WithTcpServer(this.MqttServiceConfiguration.BridgeUrl, this.MqttServiceConfiguration.BridgePort)
                .WithCredentials(this.MqttServiceConfiguration.BridgeUser.UserName, this.MqttServiceConfiguration.BridgeUser.Password)
                .WithCleanSession()
                .Build();
        }

        this.mqttClient = new MqttFactory().CreateMqttClient();
        await this.mqttClient!.ConnectAsync(this.clientOptions, this.cancellationToken);
    }

    /// <summary>
    /// Starts the MQTT server.
    /// </summary>
    private void StartMqttServer()
    {
        var optionsBuilder = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(this.MqttServiceConfiguration.Port)
            .WithEncryptedEndpointPort(this.MqttServiceConfiguration.TlsPort);

        var mqttServer = new MqttFactory().CreateMqttServer(optionsBuilder.Build());
        mqttServer.ValidatingConnectionAsync += this.ValidateConnectionAsync;
        mqttServer.InterceptingSubscriptionAsync += this.InterceptSubscriptionAsync;
        mqttServer.InterceptingPublishAsync += this.InterceptApplicationMessagePublishAsync;
        mqttServer.ClientDisconnectedAsync += this.HandleClientDisconnectedAsync;
        mqttServer.StartAsync();
    }

    /// <summary> 
    ///     Logs the message from the MQTT subscription interceptor context. 
    /// </summary> 
    /// <param name="args">The arguments.</param> 
    /// <param name="successful">A <see cref="bool"/> value indicating whether the subscription was successful or not.</param> 
    private void LogMessage(InterceptingSubscriptionEventArgs args, bool successful)
    {
#pragma warning disable Serilog004 // Constant MessageTemplate verifier
        this.logger.Information(
            successful
                ? "New subscription: ClientId = {ClientId}, TopicFilter = {TopicFilter}"
                : "Subscription failed for clientId = {clientId}, TopicFilter = {TopicFilter}",
            args.ClientId,
            args.TopicFilter);
#pragma warning restore Serilog004 // Constant MessageTemplate verifier
    }

    /// <summary>
    ///     Logs the message from the MQTT message interceptor context.
    /// </summary>
    /// <param name="args">The arguments.</param>
    private void LogMessage(InterceptingPublishEventArgs args)
    {
        var payload = args.ApplicationMessage?.PayloadSegment is null ? null : Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);

        this.logger.Information(
            "Message: ClientId = {ClientId}, Topic = {Topic}, Payload = {Payload}, QoS = {Qos}, Retain-Flag = {RetainFlag}",
            args.ClientId,
            args.ApplicationMessage?.Topic,
            payload,
            args.ApplicationMessage?.QualityOfServiceLevel,
            args.ApplicationMessage?.Retain);
    }

    /// <summary> 
    ///     Logs the message from the MQTT connection validation context. 
    /// </summary> 
    /// <param name="context">The MQTT connection validation context.</param> 
    /// <param name="showPassword">A <see cref="bool"/> value indicating whether the password is written to the log or not.</param> 
    private void LogMessage(ValidatingConnectionEventArgs context, bool showPassword)
    {
        if (showPassword)
        {
            this.logger.Information(
                "New connection: ClientId = {ClientId}, Endpoint = {Endpoint}, Username = {UserName}, Password = {Password}, CleanSession = {CleanSession}",
                context.ClientId,
                context.Endpoint,
                context.UserName,
                context.Password,
                context.CleanSession);
        }
        else
        {
            this.logger.Information(
                "New connection: ClientId = {ClientId}, Endpoint = {Endpoint}, Username = {UserName}, CleanSession = {CleanSession}",
                context.ClientId,
                context.Endpoint,
                context.UserName,
                context.CleanSession);
        }
    }

    /// <summary>
    /// Logs the heartbeat message with some memory information.
    /// </summary>
    private void LogMemoryInformation()
    {
        var totalMemory = GC.GetTotalMemory(false);
        var memoryInfo = GC.GetGCMemoryInfo();
        var divider = BytesDivider;
        Log.Information(
            "Heartbeat for service {ServiceName}: Total {Total}, heap size: {HeapSize}, memory load: {MemoryLoad}.",
            this.serviceName, $"{(totalMemory / divider):N3}", $"{(memoryInfo.HeapSizeBytes / divider):N3}", $"{(memoryInfo.MemoryLoadBytes / divider):N3}");
    }
}
