// --------------------------------------------------------------------------------------------------------------------
// <copyright file="User.cs" company="Hämmer Electronics">
//   Copyright (c) 2020 All rights reserved.
// </copyright>
// <summary>
//   The <see cref="User" /> read from the config.json file.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MqttBridge;

/// <summary>
///     The <see cref="User" /> read from the config.json file.
/// </summary>
public class User
{
    /// <summary>
    ///     Gets or sets the user name.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the password.
    /// </summary>
    [JsonIgnore]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the client identifier.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    ///     Returns a <seealso cref="string" /> which represents the object instance.
    /// </summary>
    /// <returns>A <seealso cref="string" /> representation of the instance.</returns>
    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}
