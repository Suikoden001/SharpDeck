namespace SharpDeck.Connectivity
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using SharpDeck.Enums;
    using SharpDeck.Events.Received;

    /// <summary>
    /// Provides a connection to a Stream Deck.
    /// </summary>
    public interface IStreamDeckConnection : IDisposable
    {
        /// <summary>
        /// Occurs when the plugin registers itself.
        /// </summary>
        event EventHandler Registered;

        /// <summary>
        /// Occurs when a monitored application is launched.
        /// </summary>
        event EventHandler<StreamDeckEventArgs<ApplicationPayload>> ApplicationDidLaunch;

        /// <summary>
        /// Occurs when a monitored application is terminated.
        /// </summary>
        event EventHandler<StreamDeckEventArgs<ApplicationPayload>> ApplicationDidTerminate;

        /// <summary>
        /// Occurs when a device is plugged to the computer.
        /// </summary>
        event EventHandler<DeviceConnectEventArgs> DeviceDidConnect;

        /// <summary>
        /// Occurs when a device is unplugged from the computer.
        /// </summary>
        event EventHandler<DeviceEventArgs> DeviceDidDisconnect;

        /// <summary>
        /// Occurs when <see cref="IStreamDeckConnection.GetGlobalSettingsAsync(CancellationToken)"/> has been called to retrieve the persistent global data stored for the plugin.
        /// </summary>
        event EventHandler<StreamDeckEventArgs<SettingsPayload>> DidReceiveGlobalSettings;

        /// <summary>
        /// Occurs when <see cref="IStreamDeckConnection.GetSettingsAsync(string, CancellationToken)"/> has been called to retrieve the persistent data stored for the action.
        /// </summary>
        event EventHandler<ActionEventArgs<ActionPayload>> DidReceiveSettings;

        /// <summary>
        /// Occurs when the user presses a key.
        /// </summary>
        event EventHandler<ActionEventArgs<KeyPayload>> KeyDown;

        /// <summary>
        /// Occurs when the user releases a key.
        /// </summary>
        event EventHandler<ActionEventArgs<KeyPayload>> KeyUp;

        /// <summary>
        /// Occurs when the Property Inspector appears.
        /// </summary>
        event EventHandler<ActionEventArgs> PropertyInspectorDidAppear;

        /// <summary>
        /// Occurs when the Property Inspector disappears
        /// </summary>
        event EventHandler<ActionEventArgs> PropertyInspectorDidDisappear;

        /// <summary>
        /// Occurs when the property inspector sends a message to the plugin.
        /// </summary>
        event EventHandler<ActionEventArgs<JObject>> SendToPlugin;

        /// <summary>
        /// Occurs when the computer is woken up.
        /// </summary>
        /// <remarks>
        /// A plugin may receive multiple <see cref="SystemDidWakeUp"/> events when waking up the computer.
        /// When the plugin receives the <see cref="SystemDidWakeUp"/> event, there is no garantee that the devices are available.
        /// </remarks>
        event EventHandler<StreamDeckEventArgs> SystemDidWakeUp;

        /// <summary>
        /// Occurs when the user changes the title or title parameters.
        /// </summary>
        event EventHandler<ActionEventArgs<TitlePayload>> TitleParametersDidChange;

        /// <summary>
        /// Occurs when an instance of an action appears.
        /// </summary>
        event EventHandler<ActionEventArgs<AppearancePayload>> WillAppear;

        /// <summary>
        /// Occurs when an instance of an action disappears.
        /// </summary>
        event EventHandler<ActionEventArgs<AppearancePayload>> WillDisappear;

        /// <summary>
        /// Requests the persistent global data stored for the plugin.
        /// </summary>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of sending the message; this result does not contain the settings.</returns>
        Task GetGlobalSettingsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Requests the persistent global data stored for the plugin.
        /// </summary>
        /// <typeparam name="T">The type of the settings.</typeparam>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task containing the global settings.</returns>
        Task<T> GetGlobalSettingsAsync<T>(CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Requests the persistent data stored for the specified context's action instance.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of sending the message; this result does not contain the settings.</returns>
        Task GetSettingsAsync(string context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Write a debug log to the logs file.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of logging the message.</returns>
        Task LogMessageAsync(string msg, CancellationToken cancellationToken = default);

        /// <summary>
        /// Open a URL in the default browser.
        /// </summary>
        /// <param name="url">A URL to open in the default browser.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of opening the URL.</returns>
        Task OpenUrlAsync(string url, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send a payload to the Property Inspector.
        /// </summary>
        /// <param name="context">An opaque value identifying the instances action.</param>
        /// <param name="action">The action unique identifier.</param>
        /// <param name="payload">A JSON object that will be received by the Property Inspector.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of sending payload to the property inspector.</returns>
        Task SendToPropertyInspectorAsync(string context, string action, object payload, CancellationToken cancellationToken = default);

        /// <summary>
        /// Save persistent data for the plugin.
        /// </summary>
        /// <param name="settings">An object which persistently saved globally.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of setting the global settings.</returns>
        Task SetGlobalSettingsAsync(object settings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Dynamically change the image displayed by an instance of an action; starting with Stream Deck 4.5.1, this API accepts svg images.
        /// </summary>
        /// <param name="context">An opaque value identifying the instance's action.</param>
        /// <param name="image">The image to display encoded in base64 with the image format declared in the mime type (PNG, JPEG, BMP, ...). svg is also supported. If no image is passed, the image is reset to the default image from the manifest.</param>
        /// <param name="target">Specify if you want to display the title on the hardware and software, only on the hardware, or only on the software.</param>
        /// <param name="state">A 0-based integer value representing the state of an action with multiple states. This is an optional parameter. If not specified, the image is set to all states.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of setting the image.</returns>
        Task SetImageAsync(string context, string image = "", TargetType target = TargetType.Both, int? state = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Save persistent data for the actions instance.
        /// </summary>
        /// <param name="context">An opaque value identifying the instance's action.</param>
        /// <param name="settings">A JSON object which is persistently saved for the action's instance.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of setting the settings.</returns>
        Task SetSettingsAsync(string context, object settings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Dynamically change the title of an instance of an action.
        /// </summary>
        /// <param name="context">An opaque value identifying the instance's action you want to modify.</param>
        /// <param name="title">The title to display. If no title is passed, the title is reset to the default title from the manifest.</param>
        /// <param name="target">Specify if you want to display the title on the hardware and software, only on the hardware, or only on the software.</param>
        /// <param name="state">A 0-based integer value representing the state of an action with multiple states. This is an optional parameter. If not specified, the title is set to all states.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of setting the title.</returns>
        Task SetTitleAsync(string context, string title = "", TargetType target = TargetType.Both, int? state = null, CancellationToken cancellationToken = default);

        /// <summary>
        ///	Change the state of the actions instance supporting multiple states.
        /// </summary>
        /// <param name="context">An opaque value identifying the instance's action.</param>
        /// <param name="state">A 0-based integer value representing the state requested.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of setting the state.</returns>
        Task SetStateAsync(string context, int state = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// Temporarily show an alert icon on the image displayed by an instance of an action.
        /// </summary>
        /// <param name="context">An opaque value identifying the instance's action.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of showing the alert.</returns>
        Task ShowAlertAsync(string context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Temporarily show an OK checkmark icon on the image displayed by an instance of an action.
        /// </summary>
        /// <param name="context">An opaque value identifying the instance's action.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of showing the OK.</returns>
        Task ShowOkAsync(string context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Switch to one of the preconfigured read-only profiles.
        /// </summary>
        /// <param name="context">An opaque value identifying the plugin. This value should be set to the PluginUUID received during the registration procedure.</param>
        /// <param name="device">An opaque value identifying the device. Note that this opaque value will change each time you relaunch the Stream Deck application.</param>
        /// <param name="profile">The optional name of the profile to switch to. The name should be identical to the name provided in the manifest.json file; when empty, the Stream Deck will switch to the previous profile.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of switching profiles.</returns>
        Task SwitchToProfileAsync(string context, string device, string profile = "", CancellationToken cancellationToken = default);
    }
}
