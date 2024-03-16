using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.AppModule
{
    public class Help : NamespaceHelperBase
	{
		public Help() : base("Lists the available Model-driven Apps (AppModule) in current Dataverse environment; you can show and manage roles for each model-driven app", "appmodule")
		{
		}
	}
}
