using System;

namespace TeamProjectManager.Common.Infrastructure
{
    /// <summary>
    /// Provides event arguments for help content.
    /// </summary>
    public class HelpContentEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the help content.
        /// </summary>
        public object HelpContent { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HelpContentEventArgs"/> class.
        /// </summary>
        /// <param name="helpContent">The help content.</param>
        public HelpContentEventArgs(object helpContent)
        {
            this.HelpContent = helpContent;
        }
    }
}