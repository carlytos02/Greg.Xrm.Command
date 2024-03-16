
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.AppModule
{
    public class ListCommandExecutor : ICommandExecutor<ListCommand>
    {
        private readonly IOutput output;
        private readonly IOrganizationServiceRepository organizationServiceRepository;
        public ListCommandExecutor(IOutput output, IOrganizationServiceRepository organizationServiceRepository)
        {
            this.output = output;
            this.organizationServiceRepository = organizationServiceRepository;
        }
        public async Task<CommandResult> ExecuteAsync(ListCommand command, CancellationToken cancellationToken)
        {
            try
            {
                this.output.Write($"Connecting to the current dataverse environment...");
                var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
                this.output.WriteLine("Done", ConsoleColor.Green);

                var query = new QueryExpression("appmodule");
                query.NoLock = true;
                // Add columns to query.ColumnSet
                query.ColumnSet.AddColumns(
                    "name",
                    "uniquename");

                // Add conditions to query.Criteria
                query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

                var appmoduleList = (await crm.RetrieveMultipleAsync(query)).Entities;


                this.output.WriteTable(appmoduleList,
                    appModuleListColumns(command.Verbose),
                    appModuleListData(command.Verbose),
                    (index, row) =>
                    {
                        if (index == 0)
                            return ConsoleColor.Yellow;

                        return null;
                    }
                );


                return CommandResult.Success();
            }
            catch (Exception ex)
            {
                return CommandResult.Fail("Error while getting list of appmodule: " + ex.Message, ex);
            }
        }

        private Func<string[]> appModuleListColumns(bool verbose)
        {
            string[] columns = {
                "Id",
                "Name"
            };

            if (verbose)
            {
                columns = columns.Concat(new[] {
                    "Unique name"
                }).ToArray();
            }

            return () => columns;
        }
        private Func<Entity, string[]> appModuleListData(bool verbose)
        {
            return (appModule) =>
            {
                string[] values = {
                    appModule.Id.ToString(),
                    appModule.GetAttributeValue<string>("name") ?? string.Empty
                };

                if (verbose)
                {
                    values = values.Concat(new[] {
                        appModule.GetAttributeValue<string>("uniquename") ?? string.Empty
                    }).ToArray();
                }

                return values;
            };
        }
    }
}
