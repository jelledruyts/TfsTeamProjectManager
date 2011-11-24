
namespace TeamProjectManager.Common.Infrastructure
{
    /// <summary>
    /// Provides information about a view model.
    /// </summary>
    public class ViewModelInfo
    {
        /// <summary>
        /// Gets the title of the view to be shown in the UI.
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Gets the help text associated with the view model.
        /// </summary>
        public string HelpText { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModelInfo"/> class.
        /// </summary>
        /// <param name="title">The title of the view to be shown in the UI.</param>
        /// <param name="helpText">The help text associated with the view model.</param>
        public ViewModelInfo(string title, string helpText)
        {
            this.Title = title;
            this.HelpText = helpText;
        }
    }
}