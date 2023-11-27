﻿using Greg.Xrm.Command.Commands.UnifiedRouting;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Table
{
    public class GetAgentStatusCommandExecutor : ICommandExecutor<GetAgentStatusCommand>
    {
        private readonly IOutput output;
        private readonly IOrganizationServiceRepository organizationServiceRepository;

        public GetAgentStatusCommandExecutor(
            IOutput output,
            IOrganizationServiceRepository organizationServiceFactory)
        {
            this.output = output;
            organizationServiceRepository = organizationServiceFactory;
        }

        public async Task ExecuteAsync(GetAgentStatusCommand command, CancellationToken cancellationToken)
		{
			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();

            if (crm == null)
            {
                output.WriteLine("No connection selected.");
                return;
            }

            this.output.WriteLine("Done", ConsoleColor.Green);


			try
			{

                output.WriteLine($"Checking agent status {command.AgentPrimaryEmail}");

                // Set Condition Values
                var timeQuery = command.DateTimeStatus ?? DateTime.UtcNow;

                // Instantiate QueryExpression query
                var query = new QueryExpression("msdyn_agentstatushistory");

                // Add columns to query.ColumnSet
                query.ColumnSet.AddColumns(
                    "msdyn_agentstatushistoryid",
                    "createdon",
                    "msdyn_starttime",
                    "msdyn_presenceid",
                    "msdyn_endtime",
                    "msdyn_availablecapacity",
                    "msdyn_agentid");

                // Add conditions to query.Criteria
                query.Criteria.AddCondition("msdyn_starttime", ConditionOperator.OnOrAfter, timeQuery);
                //query.Criteria.AddCondition("msdyn_endtime", ConditionOperator.OnOrBefore, timeQuery);

                // Add orders
                query.AddOrder("createdon", OrderType.Descending);

                // Add link-entity aa
                var systemuserJoin = query.AddLink("systemuser", "msdyn_agentid", "systemuserid");
                systemuserJoin.EntityAlias = "systemuserJoin";

                var queryAgentAddress = new FilterExpression(LogicalOperator.Or);
                systemuserJoin.LinkCriteria.AddFilter(queryAgentAddress);

                // Add conditions to aa.LinkCriteria
                queryAgentAddress.AddCondition("internalemailaddress", ConditionOperator.Equal, command.AgentPrimaryEmail);
                queryAgentAddress.AddCondition("domainname", ConditionOperator.Equal, command.AgentPrimaryEmail);

                var presenceJoin = query.AddLink("msdyn_presence", "msdyn_presenceid", "msdyn_presenceid", JoinOperator.Inner);
                presenceJoin.EntityAlias = "presenceJoin";

                // Add columns to presence.Columns
                presenceJoin.Columns.AddColumns("msdyn_presencestatustext", "msdyn_basepresencestatus");

                var result = (await crm.RetrieveMultipleAsync(query)).Entities.FirstOrDefault();
                if (result == null)
                {
                    output.WriteLine("No records found for: ", ConsoleColor.Yellow).WriteLine(command.AgentPrimaryEmail, ConsoleColor.Yellow);
                    return;
                }

                var status = result.GetAttributeValue<string>("presenceJoin.msdyn_presencestatustext");
                var statusOpt = result.GetAttributeValue<OptionSetValue>("presenceJoin.msdyn_basepresencestatus");
                var dateStart = result.GetFormattedValue("msdyn_starttime");


                this.output.WriteLine(command.AgentPrimaryEmail)
					.Write("is ")
					.WriteLine(status)
					.Write("since ")
					.WriteLine(dateStart);
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                output.WriteLine()
                    .Write("Error: ", ConsoleColor.Red)
                    .WriteLine(ex.Message, ConsoleColor.Red);

                if (ex.InnerException != null)
                {
                    output.Write("  ").WriteLine(ex.InnerException.Message, ConsoleColor.Red);
                }
            }
        }
    }
}


/*
 * 
 * 
 * 
 * Available	192360000
Busy	192360001
Busy - DND	192360002
Away	192360003
Offline	192360004

*/