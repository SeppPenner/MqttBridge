// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MqttService.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   The main service class of the <see cref="MqttService" />.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MqttBridge;

/// <inheritdoc cref="BackgroundService"/>
/// <inheritdoc cref="IMqttServerSubscriptionInterceptor"/>
/// <inheritdoc cref="IMqttServerApplicationMessageInterceptor"/>
/// <inheritdoc cref="IMqttServerConnectionValidator"/>
/// <inheritdoc cref="IMqttServerClientDisconnectedHandler"/>
/// <summary>
///     The main service class of the <see cref="MqttService" />.
/// </summary>
public class MqttService : BackgroundService, IMqttServerSubscriptionInterceptor, IMqttServerApplicationMessageInterceptor, IMqttServerConnectionValidator, IMqttServerClientDisconnectedHandler
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
    private IMqttClientOptions? clientOptions;

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
        this.logger = Log.ForContext("Type", nameof(MqttService));
        this.serviceName = serviceName;
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
    /// <param name="context">The context.</param>
    /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
    public Task ValidateConnectionAsync(MqttConnectionValidatorContext context)
    {
        try
        {
            var currentUser = this.MqttServiceConfiguration.Users.FirstOrDefault(u => u.UserName == context.Username);

            if (currentUser == null)
            {
                context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                this.LogMessage(context, true);
                return Task.CompletedTask;
            }

            if (context.Username != currentUser.UserName)
            {
                context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                this.LogMessage(context, true);
                return Task.CompletedTask;
            }

            if (context.Password != currentUser.Password)
            {
                context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                this.LogMessage(context, true);
                return Task.CompletedTask;
            }

            context.ReasonCode = MqttConnectReasonCode.Success;
            this.LogMessage(context, false);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            this.logger.Error("An error occurred: {@ex}.", ex);
            return Task.FromException(ex);
        }
    }

    /// <summary>
    /// Validates the MQTT subscriptions.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
    public Task InterceptSubscriptionAsync(MqttSubscriptionInterceptorContext context)
    {
        try
        {
            context.AcceptSubscription = true;
            this.LogMessage(context, true);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            this.logger.Error("An error occurred: {@ex}.", ex);
            return Task.FromException(ex);
        }
    }

    /// <summary>
    /// Validates the MQTT application messages.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
    public async Task InterceptApplicationMessagePublishAsync(MqttApplicationMessageInterceptorContext context)
    {
        try
        {
            context.AcceptPublish = true;
            await this.mqttClient!.PublishAsync(context.ApplicationMessage, this.cancellationToken);
            this.LogMessage(context);
        }
        catch (Exception ex)
        {
            this.logger.Error("An error occurred: {@ex}.", ex);
        }
    }

    /// <summary>
    /// Validates the MQTT client disconnect.
    /// </summary>
    /// <param name="eventArgs">The event args.</param>
    /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
    public async Task HandleClientDisconnectedAsync(MqttServerClientDisconnectedEventArgs eventArgs)
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
            this.logger.Error("An error occurred: {@ex}.", ex);
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
                .WithTls()
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
            .WithEncryptedEndpointPort(this.MqttServiceConfiguration.TlsPort)
            .WithConnectionValidator(this)
            .WithSubscriptionInterceptor(this)
            .WithApplicationMessageInterceptor(this);

        var mqttServer = new MqttFactory().CreateMqttServer();
        mqttServer.StartAsync(optionsBuilder.Build());
    }

    /// <summary> 
    ///     Logs the message from the MQTT subscription interceptor context. 
    /// </summary> 
    /// <param name="context">The MQTT subscription interceptor context.</param> 
    /// <param name="successful">A <see cref="bool"/> value indicating whether the subscription was successful or not.</param> 
    private void LogMessage(MqttSubscriptionInterceptorContext context, bool successful)
    {
        this.logger.Information(
            successful
                ? "New subscription: ClientId = {clientId}, TopicFilter = {topicFilter}"
                : "Subscription failed for clientId = {clientId}, TopicFilter = {topicFilter}",
            context.ClientId,
            context.TopicFilter);
    }

    /// <summary>
    ///     Logs the message from the MQTT message interceptor context.
    /// </summary>
    /// <param name="context">The MQTT message interceptor context.</param>
    private void LogMessage(MqttApplicationMessageInterceptorContext context)
    {
        var payload = context.ApplicationMessage?.Payload == null ? null : Encoding.UTF8.GetString(context.ApplicationMessage.Payload);

        this.logger.Information(
            "Message: ClientId = {clientId}, Topic = {topic}, Payload = {payload}, QoS = {qos}, Retain-Flag = {retainFlag}",
            context.ClientId,
            context.ApplicationMessage?.Topic,
            payload,
            context.ApplicationMessage?.QualityOfServiceLevel,
            context.ApplicationMessage?.Retain);
    }

    /// <summary> 
    ///     Logs the message from the MQTT connection validation context. 
    /// </summary> 
    /// <param name="context">The MQTT connection validation context.</param> 
    /// <param name="showPassword">A <see cref="bool"/> value indicating whether the password is written to the log or not.</param> 
    private void LogMessage(MqttConnectionValidatorContext context, bool showPassword)
    {
        if (showPassword)
        {
            this.logger.Information(
                "New connection: ClientId = {clientId}, Endpoint = {endpoint}, Username = {userName}, Password = {password}, CleanSession = {cleanSession}",
                context.ClientId,
                context.Endpoint,
                context.Username,
                context.Password,
                context.CleanSession);
        }
        else
        {
            this.logger.Information(
                "New connection: ClientId = {clientId}, Endpoint = {endpoint}, Username = {userName}, CleanSession = {cleanSession}",
                context.ClientId,
                context.Endpoint,
                context.Username,
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