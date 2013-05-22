
namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public class MacroDefinition
    {
        public string Macro { get; private set; }
        public string Example { get; private set; }
        public string Description { get; private set; }

        public MacroDefinition(string macro, string example, string description)
        {
            this.Macro = macro;
            this.Example = example;
            this.Description = description;
        }
    }
}