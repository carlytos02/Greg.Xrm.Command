namespace Greg.Xrm.Command.Commands.AppModule
{
    [Command("appmodule", "list", HelpText = "Lists the available Model-driven Apps (AppModule) in current Dataverse environment. It displays id and name.")]
    public class ListCommand
    {
        [Option("verbose", "v", HelpText = "Add unique name.", DefaultValue = false)]
        public bool Verbose { get; set; } = false;
    }
}