// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MqttServiceConfiguration.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   The <see cref="MqttServiceConfiguration" /> read from the configuration file.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MqttBridge;

/// <summary>
///     The <see cref="MqttServiceConfiguration" /> read from the configuration file.
/// </summary>
public sealed class MqttServiceConfiguration
{
    /// <summary>
    /// Gets or sets the port.
    /// </summary>
    public int Port { get; set; } = 1883;

    /// <summary>
    /// Gets or sets the list of valid users.
    /// </summary>
    public List<User> Users { get; set; } = [];

    /// <summary>
    /// Gets or sets the heartbeat delay in milliseconds.
    /// </summary>
    public int DelayInMilliSeconds { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the TLS port.
    /// </summary>
    public int TlsPort { get; set; } = 8883;

    /// <summary>
    /// Gets or sets the bridge server URL.
    /// </summary>
    public string BridgeUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bridge server's port.
    /// </summary>
    public int BridgePort { get; set; } = 8883;

    /// <summary>
    /// Gets or sets a value indiacting whether TLS should be used or not.
    /// </summary>
    public bool UseTls { get; set; } = true;

    /// <summary>
    /// Gets or sets the bridge user.
    /// </summary>
    public BridgeUser BridgeUser { get; set; } = new();

    /// <summary>
    /// Checks whether the configuration is valid or not.
    /// </summary>
    /// <returns>A value indicating whether the configuration is valid or not.</returns>
    public bool IsValid()
    {
        if (this.Port is <= 0 or > 65535)
        {
            throw new Exception("The port is invalid.");
        }

        if (!this.Users.Any())
        {
            throw new Exception("The users are invalid.");
        }

        if (this.DelayInMilliSeconds <= 0)
        {
            throw new Exception("The heartbeat delay is invalid.");
        }

        if (this.TlsPort is <= 0 or > 65535)
        {
            throw new Exception("The TLS port is invalid.");
        }

        if (string.IsNullOrWhiteSpace(this.BridgeUrl))
        {
            throw new Exception("The bridge server URL is invalid.");
        }

        if (this.BridgePort is <= 0 or > 65535)
        {
            throw new Exception("The bridge port is invalid.");
        }

        if (this.BridgeUser is default(BridgeUser))
        {
            throw new Exception("The bridge users are invalid.");
        }

        return true;
    }
}
