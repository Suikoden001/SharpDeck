namespace SharpDeck
{
    using System.Threading.Tasks;
    using SharpDeck.Events;
    using SharpDeck.Events.Received;

    /// <summary>
    /// Provides an implementation of an action, containing settings, that can be registered on a <see cref="StreamDeckClient"/>.
    /// </summary>
    /// <typeparam name="TSettings">The type of the settings.</typeparam>
    public class StreamDeckAction<TSettings> : StreamDeckAction
        where TSettings : class
    {
        /// <summary>
        /// Gets this action's instances settings asynchronously.
        /// </summary>
        /// <returns>The task containing the settings.</returns>
        public Task<TSettings> GetSettingsAsync()
            => this.GetSettingsAsync<TSettings>();

        /// <summary>
        /// Raises the <see cref="StreamDeckActionEventPropagator.DidReceiveSettings" /> event.
        /// </summary>
        /// <param name="args">The <see cref="ActionEventArgs{ActionPayload}" /> instance containing the event data.</param>
        /// <returns>The task of updating the state of the object based on the settings.</returns>
        protected internal override async Task OnDidReceiveSettings(ActionEventArgs<ActionPayload> args)
        {
            await this.OnDidReceiveSettings(args, args.Payload.GetSettings<TSettings>());
            await base.OnDidReceiveSettings(args);
        }

        /// <summary>
        /// Handles the <see cref="StreamDeckActionEventPropagator.DidReceiveSettings" /> event.
        /// </summary>
        /// <param name="args">The <see cref="ActionEventArgs{TActionPayload}" /> instance containing the event data.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The task of updating the state of the object based on the settings.</returns>
        protected virtual Task OnDidReceiveSettings(ActionEventArgs<ActionPayload> args, TSettings settings)
            => Task.CompletedTask;

        /// <summary>
        /// Occurs when this instance is initialized.
        /// </summary>
        /// <param name="args">The <see cref="ActionEventArgs{AppearancePayload}" /> instance containing the event data.</param>
        protected override void OnInit(ActionEventArgs<AppearancePayload> args)
        {
            this.OnInit(args, args.Payload.GetSettings<TSettings>());
            base.OnInit(args);
        }

        /// <summary>
        /// Occurs when this instance is initialized.
        /// </summary>
        /// <param name="args">The <see cref="ActionEventArgs{AppearancePayload}"/> instance containing the event data.</param>
        /// <param name="settings">The settings.</param>
        protected virtual void OnInit(ActionEventArgs<AppearancePayload> args, TSettings settings) { }
    }
}
