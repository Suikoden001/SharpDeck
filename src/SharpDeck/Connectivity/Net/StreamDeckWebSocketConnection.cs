namespace SharpDeck.Connectivity.Net
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    using SharpDeck.Enums;
    using SharpDeck.Events.Received;
    using SharpDeck.Events.Sent;
    using SharpDeck.Extensions;

    /// <summary>
    /// Provides a connection between Elgato Stream Deck devices and a Stream Deck client.
    /// </summary>
    internal sealed class StreamDeckWebSocketConnection : IStreamDeckConnection
    {
        /// <summary>
        /// Gets the default JSON settings.
        /// </summary>
        internal static readonly JsonSerializerSettings DefaultJsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
            Formatting = Formatting.None
        };

        /// <summary>
        /// Occurs when the plugin registers itself.
        /// </summary>
        public event EventHandler Registered;

        /// <summary>
        /// Occurs when a monitored application is launched.
        /// </summary>
        public event EventHandler<StreamDeckEventArgs<ApplicationPayload>> ApplicationDidLaunch;

        /// <summary>
        /// Occurs when a monitored application is terminated.
        /// </summary>
        public event EventHandler<StreamDeckEventArgs<ApplicationPayload>> ApplicationDidTerminate;

        /// <summary>
        /// Occurs when a device is plugged to the computer.
        /// </summary>
        public event EventHandler<DeviceConnectEventArgs> DeviceDidConnect;

        /// <summary>
        /// Occurs when a device is unplugged from the computer.
        /// </summary>
        public event EventHandler<DeviceEventArgs> DeviceDidDisconnect;

        /// <summary>
        /// Occurs when <see cref="IStreamDeckConnection.GetGlobalSettingsAsync(CancellationToken)"/> has been called to retrieve the persistent global data stored for the plugin.
        /// </summary>
        public event EventHandler<StreamDeckEventArgs<SettingsPayload>> DidReceiveGlobalSettings;

        /// <summary>
        /// Occurs when <see cref="IStreamDeckConnection.GetSettingsAsync(string, CancellationToken)"/> has been called to retrieve the persistent data stored for the action.
        /// </summary>
        public event EventHandler<ActionEventArgs<ActionPayload>> DidReceiveSettings;

        /// <summary>
        /// Occurs when the user presses a key.
        /// </summary>
        public event EventHandler<ActionEventArgs<KeyPayload>> KeyDown;

        /// <summary>
        /// Occurs when the user releases a key.
        /// </summary>
        public event EventHandler<ActionEventArgs<KeyPayload>> KeyUp;

        /// <summary>
        /// Occurs when the Property Inspector appears.
        /// </summary>
        public event EventHandler<ActionEventArgs> PropertyInspectorDidAppear;

        /// <summary>
        /// Occurs when the Property Inspector disappears
        /// </summary>
        public event EventHandler<ActionEventArgs> PropertyInspectorDidDisappear;

        /// <summary>
        /// Occurs when the property inspector sends a message to the plugin.
        /// </summary>
        public event EventHandler<ActionEventArgs<JObject>> SendToPlugin;

        /// <summary>
        /// Occurs when the computer is woken up.
        /// </summary>
        /// <remarks>
        /// A plugin may receive multiple <see cref="SystemDidWakeUp"/> events when waking up the computer.
        /// When the plugin receives the <see cref="SystemDidWakeUp"/> event, there is no garantee that the devices are available.
        /// </remarks>
        public event EventHandler<StreamDeckEventArgs> SystemDidWakeUp;

        /// <summary>
        /// Occurs when the user changes the title or title parameters.
        /// </summary>
        public event EventHandler<ActionEventArgs<TitlePayload>> TitleParametersDidChange;

        /// <summary>
        /// Occurs when an instance of an action appears.
        /// </summary>
        public event EventHandler<ActionEventArgs<AppearancePayload>> WillAppear;

        /// <summary>
        /// Occurs when an instance of an action disappears.
        /// </summary>
        public event EventHandler<ActionEventArgs<AppearancePayload>> WillDisappear;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamDeckWebSocketConnection"/> class.
        /// </summary>
        /// <param name="registrationParameters">The registration parameters.</param>
        /// <param name="logger">The logger.</param>
        public StreamDeckWebSocketConnection(RegistrationParameters registrationParameters, ILogger<StreamDeckWebSocketConnection> logger = null)
        {
            this.Logger = logger;
            this.RegistrationParameters = registrationParameters;
        }

        /// <summary>
        /// Gets the information about the connection.
        /// </summary>
        public RegistrationInfo Info => this.RegistrationParameters.Info;

        /// <summary>
        /// Gets or sets the registration parameters.
        /// </summary>
        private RegistrationParameters RegistrationParameters { get; set; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        private ILogger<StreamDeckWebSocketConnection> Logger { get; }

        /// <summary>
        /// Gets or sets the web socket.
        /// </summary>
        private WebSocketConnection WebSocket { get; set; }

        /// <summary>
        /// Connects to the Stream Deck asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The optioanl cancellation token.</param>
        /// <returns>The task of connecting to the Stream Deck.</returns>
        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            this.Logger?.LogTrace("Connecting to Stream Deck.");
            this.WebSocket = new WebSocketConnection($"ws://localhost:{this.RegistrationParameters.Port}/", StreamDeckWebSocketConnection.DefaultJsonSettings);
            this.WebSocket.MessageReceived += this.WebSocket_MessageReceived;

            await this.WebSocket.ConnectAsync();
            this.Logger?.LogTrace($"Connected to Stream Deck; registering plugin.");

            await this.WebSocket.SendJsonAsync(new RegistrationMessage(this.RegistrationParameters.Event, this.RegistrationParameters.PluginUUID), cancellationToken);
            this.Registered?.Invoke(this, EventArgs.Empty);
            this.Logger?.LogTrace($"Plugin registrered.");
        }

        /// <summary>
        /// Disconnects from the Stream Deck asynchronously.
        /// </summary>
        /// <returns>The task of waiting of disconnecting.</returns>
        public Task DisconnectAsync()
            => this.WebSocket.DisconnectAsync();

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.WebSocket?.Dispose();
            this.WebSocket = null;
        }

        /// <summary>
        /// Requests the persistent global data stored for the plugin.
        /// </summary>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of sending the message; this result does not contain the settings.</returns>
        public Task GetGlobalSettingsAsync(CancellationToken cancellationToken = default)
            => this.SendAsync(new ContextMessage("getGlobalSettings", this.RegistrationParameters.PluginUUID), cancellationToken);

        /// <summary>
        /// Requests the persistent global data stored for the plugin.
        /// </summary>
        /// <typeparam name="T">The type of the settings.</typeparam>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task containing the global settings.</returns>
        public Task<T> GetGlobalSettingsAsync<T>(CancellationToken cancellationToken = default)
            where T : class
        {
            var taskSource = new TaskCompletionSource<T>();

            // Declare the local function handler that sets the task result
            void handler(object sender, StreamDeckEventArgs<SettingsPayload> e)
            {
                if (taskSource.TrySetResult(e.Payload.GetSettings<T>()))
                {
                    this.DidReceiveGlobalSettings -= handler;
                }
            }

            // Register the cancellation.
            cancellationToken.Register(() =>
            {
                if (taskSource.TrySetCanceled())
                {
                    this.DidReceiveGlobalSettings -= handler;
                }
            });

            // Listen for receiving events, and trigger a request.
            this.DidReceiveGlobalSettings += handler;
            this.GetGlobalSettingsAsync(cancellationToken);

            return taskSource.Task;
        }

        /// <summary>
        /// Requests the persistent data stored for the specified context's action instance.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of sending the message; the result does not contain the settings.</returns>
        public Task GetSettingsAsync(string context, CancellationToken cancellationToken = default)
            => this.SendAsync(new ContextMessage("getSettings", context), cancellationToken);

        /// <summary>
        /// Write a debug log to the logs file.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of logging the message.</returns>
        public Task LogMessageAsync(string msg, CancellationToken cancellationToken = default)
            => this.SendAsync(new Message<LogPayload>("logMessage", new LogPayload(msg)), cancellationToken);

        /// <summary>
        /// Open a URL in the default browser.
        /// </summary>
        /// <param name="url">A URL to open in the default browser.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of opening the URL.</returns>
        public Task OpenUrlAsync(string url, CancellationToken cancellationToken = default)
            => this.SendAsync(new Message<UrlPayload>("openUrl", new UrlPayload(url)), cancellationToken);

        /// <summary>
        /// Send a payload to the Property Inspector.
        /// </summary>
        /// <param name="context">An opaque value identifying the instances action.</param>
        /// <param name="action">The action unique identifier.</param>
        /// <param name="payload">A JSON object that will be received by the Property Inspector.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>vvvvvv
        /// <returns>The task of sending payload to the property inspector.</returns>
        public Task SendToPropertyInspectorAsync(string context, string action, object payload, CancellationToken cancellationToken = default)
            => this.SendAsync(new ActionMessage<object>("sendToPropertyInspector", context, action, payload), cancellationToken);

        /// <summary>
        /// Save persistent data for the plugin.
        /// </summary>
        /// <param name="settings">An object which persistently saved globally.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of setting the global settings.</returns>
        public Task SetGlobalSettingsAsync(object settings, CancellationToken cancellationToken = default)
        {
            settings = settings is JObject ? settings : JObject.FromObject(settings, JsonSerializer.Create(StreamDeckWebSocketConnection.DefaultJsonSettings));
            return this.SendAsync(new ContextMessage<object>("setGlobalSettings", this.RegistrationParameters.PluginUUID, settings), cancellationToken);
        }

        /// <summary>
        /// Dynamically change the image displayed by an instance of an action; starting with Stream Deck 4.5.1, this API accepts svg images.
        /// </summary>
        /// <param name="context">An opaque value identifying the instance's action.</param>
        /// <param name="image">The image to display encoded in base64 with the image format declared in the mime type (PNG, JPEG, BMP, ...). svg is also supported. If no image is passed, the image is reset to the default image from the manifest.</param>
        /// <param name="target">Specify if you want to display the title on the hardware and software, only on the hardware, or only on the software.</param>
        /// <param name="state">A 0-based integer value representing the state of an action with multiple states. This is an optional parameter. If not specified, the image is set to all states.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of setting the image.</returns>
        public Task SetImageAsync(string context, string image = "", TargetType target = TargetType.Both, int? state = null, CancellationToken cancellationToken = default)
            => this.SendAsync(new ContextMessage<SetImagePayload>("setImage", context, new SetImagePayload(image, target, state)), cancellationToken);

        /// <summary>
        /// Save persistent data for the action's instance.
        /// </summary>
        /// <param name="context">An opaque value identifying the instance's action.</param>
        /// <param name="settings">An object which is persistently saved for the action's instance.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of setting the settings.</returns>
        public Task SetSettingsAsync(string context, object settings, CancellationToken cancellationToken = default)
            => this.SendAsync(new ContextMessage<object>("setSettings", context, JObject.FromObject(settings, JsonSerializer.Create(StreamDeckWebSocketConnection.DefaultJsonSettings))), cancellationToken);

        /// <summary>
        /// Change the state of the actions instance supporting multiple states.
        /// </summary>
        /// <param name="context">An opaque value identifying the instance's action.</param>
        /// <param name="state">A 0-based integer value representing the state requested.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of setting the state.</returns>
        public Task SetStateAsync(string context, int state = 0, CancellationToken cancellationToken = default)
            => this.SendAsync(new ContextMessage<SetStatePayload>("setState", context, new SetStatePayload(state)), cancellationToken);

        /// <summary>
        /// Dynamically change the title of an instance of an action.
        /// </summary>
        /// <param name="context">An opaque value identifying the instance's action you want to modify.</param>
        /// <param name="title">The title to display. If no title is passed, the title is reset to the default title from the manifest.</param>
        /// <param name="target">Specify if you want to display the title on the hardware and software, only on the hardware, or only on the software.</param>
        /// <param name="state">A 0-based integer value representing the state of an action with multiple states. This is an optional parameter. If not specified, the title is set to all states.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of setting the title.</returns>
        public Task SetTitleAsync(string context, string title = "", TargetType target = TargetType.Both, int? state = null, CancellationToken cancellationToken = default)
            => this.SendAsync(new ContextMessage<SetTitlePayload>("setTitle", context, new SetTitlePayload(title, target, state)), cancellationToken);

        /// <summary>
        /// Temporarily show an alert icon on the image displayed by an instance of an action.
        /// </summary>
        /// <param name="context">An opaque value identifying the instance's action.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of showing the alert.</returns>
        public Task ShowAlertAsync(string context, CancellationToken cancellationToken = default)
            => this.SendAsync(new ContextMessage("showAlert", context), cancellationToken);

        /// <summary>
        /// Temporarily show an OK checkmark icon on the image displayed by an instance of an action.
        /// </summary>
        /// <param name="context">An opaque value identifying the instance's action.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of showing the OK.</returns>
        public Task ShowOkAsync(string context, CancellationToken cancellationToken = default)
            => this.SendAsync(new ContextMessage("showOk", context), cancellationToken);

        /// <summary>
        /// Switch to one of the preconfigured read-only profiles.
        /// </summary>
        /// <param name="context">An opaque value identifying the plugin. This value should be set to the PluginUUID received during the registration procedure.</param>
        /// <param name="device">An opaque value identifying the device. Note that this opaque value will change each time you relaunch the Stream Deck application.</param>
        /// <param name="profile">The name of the profile to switch to. The name should be identical to the name provided in the manifest.json file.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>The task of switching profiles.</returns>
        public Task SwitchToProfileAsync(string context, string device, string profile = "", CancellationToken cancellationToken = default)
            => this.SendAsync(new DeviceMessage<SwitchToProfilePayload>("switchToProfile", context, device, new SwitchToProfilePayload(profile)), cancellationToken);

        /// <summary>
        /// Sends the value to the Stream Deck asynchronously.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task of sending the value.</returns>
        private Task SendAsync(object value, CancellationToken cancellationToken)
            => this.WebSocket.SendJsonAsync(value, cancellationToken);

        /// <summary>
        /// Handles the <see cref="WebSocketConnection.MessageReceived"/> public event of <see cref="WebSocket"/>.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="WebSocketMessageEventArgs"/> instance containing the public event data.</param>
        private void WebSocket_MessageReceived(object sender, WebSocketMessageEventArgs e)
        {
            try
            {
                // attempt to parse the original message
                var args = JObject.Parse(e.Message);
                if (!args.TryGetString(nameof(StreamDeckEventArgs.Event), out var @event))
                {
                    throw new ArgumentException("Unable to parse public event from message");
                }

                // propagate the event, removing the sync context to prpublic event dead-locking
                this.Raise(@event, args);
            }
            catch (Exception ex)
            {
                _ = this.LogMessageAsync(ex.Message);
            }
        }

        /// <summary>
        /// Attempts to propagate the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>The task of propagating the event.</returns>
        private void Raise(string @event, JObject args)
        {
            switch (@event)
            {
                // global
                case "applicationDidLaunch":
                    this.ApplicationDidLaunch?.Invoke(this, args.ToObject<StreamDeckEventArgs<ApplicationPayload>>());
                    break;

                case "applicationDidTerminate":
                    this.ApplicationDidTerminate?.Invoke(this, args.ToObject<StreamDeckEventArgs<ApplicationPayload>>());
                    break;

                case "deviceDidConnect":
                    this.DeviceDidConnect?.Invoke(this, args.ToObject<DeviceConnectEventArgs>());
                    break;

                case "deviceDidDisconnect":
                    this.DeviceDidDisconnect?.Invoke(this, args.ToObject<DeviceEventArgs>());
                    break;

                case "didReceiveGlobalSettings":
                    this.DidReceiveGlobalSettings?.Invoke(this, args.ToObject<StreamDeckEventArgs<SettingsPayload>>());
                    break;

                case "systemDidWakeUp":
                    this.SystemDidWakeUp?.Invoke(this, args.ToObject<StreamDeckEventArgs>());
                    break;

                // action specific
                case "didReceiveSettings":
                    this.DidReceiveSettings?.Invoke(this, args.ToObject<ActionEventArgs<ActionPayload>>());
                    break;

                case "keyDown":
                    this.KeyDown?.Invoke(this, args.ToObject<ActionEventArgs<KeyPayload>>());
                    break;

                case "keyUp":
                    this.KeyUp?.Invoke(this, args.ToObject<ActionEventArgs<KeyPayload>>());
                    break;

                case "propertyInspectorDidAppear":
                    this.PropertyInspectorDidAppear?.Invoke(this, args.ToObject<ActionEventArgs>());
                    break;

                case "propertyInspectorDidDisappear":
                    this.PropertyInspectorDidDisappear?.Invoke(this, args.ToObject<ActionEventArgs>());
                    break;

                case "sendToPlugin":
                    this.SendToPlugin?.Invoke(this, args.ToObject<ActionEventArgs<JObject>>());
                    break;

                case "titleParametersDidChange":
                    this.TitleParametersDidChange?.Invoke(this, args.ToObject<ActionEventArgs<TitlePayload>>());
                    break;

                case "willAppear":
                    this.WillAppear?.Invoke(this, args.ToObject<ActionEventArgs<AppearancePayload>>());
                    break;

                case "willDisappear":
                    this.WillDisappear?.Invoke(this, args.ToObject<ActionEventArgs<AppearancePayload>>());
                    break;

                // unrecognised
                default:
                    throw new ArgumentException($"Unrecognised event: {@event}", nameof(@event));
            }
        }
    }
}
