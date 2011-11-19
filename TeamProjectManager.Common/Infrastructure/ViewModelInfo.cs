
namespace TeamProjectManager.Common.Infrastructure
{
    public class ViewModelInfo
    {
        public string Title { get; private set; }
        public string HelpText { get; private set; }

        public ViewModelInfo(string title, string helpText)
        {
            this.Title = title;
            this.HelpText = helpText;
        }
    }
}