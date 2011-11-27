
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
        /// Gets the description associated with the view.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModelInfo"/> class.
        /// </summary>
        /// <param name="title">The title of the view to be shown in the UI.</param>
        /// <param name="description">The description associated with the view.</param>
        public ViewModelInfo(string title, string description)
        {
            this.Title = title;
            this.Description = description;
        }
    }
}