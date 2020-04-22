namespace MqttBridge
{
    /// <summary>
    ///     The <see cref="Config" /> read from the config.json file.
    /// </summary>
    public class Config
    {
        /// <summary>
        ///     Gets or sets the bridge port.
        /// </summary>
        public int BridgePort { get; set; }

        /// <summary>
        ///     Gets or sets the bridge url.
        /// </summary>
        public string BridgeUrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether SSL should be used or not.
        /// </summary>
        public bool UseSsl { get; set; }

        /// <summary>
        ///     Gets or sets the bridge user.
        /// </summary>
        public User BridgeUser { get; set; }
    }
}