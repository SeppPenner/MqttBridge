// --------------------------------------------------------------------------------------------------------------------
// <copyright file="User.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   The <see cref="BridgeUser" /> read from the configuration file.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MqttBridge;

/// <summary>
///     The <see cref="BridgeUser" /> read from the configuration file.
/// </summary>
public sealed class BridgeUser : User
{
    /// <summary>
    /// Gets or sets the client identifier.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
}
