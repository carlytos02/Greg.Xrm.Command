namespace Greg.Xrm.Command.Commands.AppModule
{
    [Command("appmodule", "listroles", HelpText = "Lists the roles associated to one or all model-driven apps.")]
    public class ListAppRolesCommand
    {
        [Option("all", "a", HelpText = "Show all model-driven app / roles associations", DefaultValue = false)]
        public bool All { get; set; } = false;

        [Option("id", HelpText = "Model-driven app id used to retrieve related roles", DefaultValue = false)]
        public string? ModelDrivenAppId { get; set; }
    }
}
