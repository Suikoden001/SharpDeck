﻿namespace SharpDeck.Models
{
    /// <summary>
    /// Provides information about an action.
    /// </summary>
    public class ActionPayload
    {
        /// <summary>
        /// Gets or sets the coordinates of a triggered action.
        /// </summary>
        public Coordinates Coordinates { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the action is inside a Multi Action.
        /// </summary>
        public bool IsInMultiAction { get; set; }

        /// <summary>
        /// Gets or sets the JSON containing data that you can set and are stored persistently.
        /// </summary>
        public object Settings { get; set; }

        /// <summary>
        /// Gets or sets the state; this is a parameter that is only set when the action has multiple states defined in its manifest.json. The 0-based value contains the current state of the action.
        /// </summary>
        public int State { get; set; }
    }
}
