﻿using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using System.Text;

namespace Greg.Xrm.Command.Commands.Relationship
{
	public class CreateN1CommandExecutor : ICommandExecutor<CreateN1Command>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;

		public CreateN1CommandExecutor(IOutput output, IOrganizationServiceRepository organizationServiceRepository)
        {
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
		}

		public async Task ExecuteAsync(CreateN1Command command, CancellationToken cancellationToken)
		{
			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("Done", ConsoleColor.Green);


			try
			{
				var defaultLanguageCode = await crm.GetDefaultLanguageCodeAsync();

				var currentSolutionName = command.SolutionName;
				if (string.IsNullOrWhiteSpace(currentSolutionName))
				{
					currentSolutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
					if (currentSolutionName == null)
					{
						output.WriteLine("No solution name provided and no current solution name found in the settings. Please provide a solution name or set a current solution name in the settings.", ConsoleColor.Red);
						return;
					}
				}






				output.WriteLine("Checking solution existence and retrieving publisher prefix");

				var query = new QueryExpression("solution");
				query.ColumnSet.AddColumns("ismanaged");
				query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, currentSolutionName);
				var link = query.AddLink("publisher", "publisherid", "publisherid");
				link.Columns.AddColumns("customizationprefix");
				link.EntityAlias = "publisher";
				query.NoLock = true;
				query.TopCount = 1;


				var solutionList = (await crm.RetrieveMultipleAsync(query)).Entities;
				if (solutionList.Count == 0)
				{
					output.WriteLine("Invalid solution name: ", ConsoleColor.Red).WriteLine(currentSolutionName, ConsoleColor.Red);
					return;
				}

				var managed = solutionList[0].GetAttributeValue<bool>("ismanaged");
				if (managed)
				{
					output.WriteLine("The provided solution is managed. You must specify an unmanaged solution.", ConsoleColor.Red);
					return;
				}

				var publisherPrefix = solutionList[0].GetAttributeValue<AliasedValue>("publisher.customizationprefix").Value as string;
				if (string.IsNullOrWhiteSpace(publisherPrefix))
				{
					output.WriteLine("Unable to retrieve the publisher prefix. Please report a bug to the project GitHub page.", ConsoleColor.Red);
					return;
				}


				await CheckEligibilityAsync(crm, command);









				output.Write("Setting up CreateOneToManyRequest...");

				var request = new CreateOneToManyRequest
				{
					SolutionUniqueName = command.SolutionName,
					OneToManyRelationship = new OneToManyRelationshipMetadata
					{
						ReferencingEntity = command.ChildTable,
						ReferencedEntity = command.ParentTable,
						SchemaName = CreateRelationshipSchemaName(command, publisherPrefix),
						AssociatedMenuConfiguration = new AssociatedMenuConfiguration
						{
							Behavior = command.MenuBehavior,
							Group = CreateMenuGroup(command),
							Label = CreateMenuLabel(command, defaultLanguageCode),
							Order = command.MenuOrder
						},
						CascadeConfiguration = new CascadeConfiguration
						{
							Assign = command.CascadeAssign,
							Archive = command.CascadeArchive,
							Share = command.CascadeShare,
							Unshare = command.CascadeUnshare,
							Delete = command.CascadeDelete,
							Merge = command.CascadeMerge,
							Reparent = command.CascadeReparent
						}
					},
					Lookup = new LookupAttributeMetadata
					{
						DisplayName = await CreateLookupDisplayNameAsync(crm, output, command, defaultLanguageCode),
						SchemaName = CreateLookupSchemaName(command, publisherPrefix),
						Description = null,
						RequiredLevel = new AttributeRequiredLevelManagedProperty(command.RequiredLevel)
					}
				};

				var response = (CreateOneToManyResponse)await crm.ExecuteAsync(request);

				this.output.WriteLine("Done", ConsoleColor.Green)
					.Write("  Relationship ID : ")
					.WriteLine(response.RelationshipId, ConsoleColor.Yellow)
					.Write("  Lookup Column ID: ")
					.WriteLine(response.AttributeId, ConsoleColor.Yellow);

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

		private async Task CheckEligibilityAsync(IOrganizationServiceAsync2 crm, CreateN1Command command)
		{
			var request1 = new CanBeReferencedRequest
			{
				EntityName = command.ParentTable
			};
			var response1 = (CanBeReferencedResponse)await crm.ExecuteAsync(request1);
			if (!response1.CanBeReferenced)
				throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The entity {command.ParentTable} cannot be parent of an N-1 relationship");


			var request2 = new CanBeReferencingRequest()
			{
				EntityName = command.ChildTable
			};
			var response2 = (CanBeReferencingResponse)await crm.ExecuteAsync(request2);
			if (!response2.CanBeReferencing)
				throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The entity {command.ChildTable} cannot be child of an N-1 relationship");
		}

		private static AssociatedMenuGroup? CreateMenuGroup(CreateN1Command command)
		{
			if (command.MenuBehavior == AssociatedMenuBehavior.DoNotDisplay)
				return null;

			return command.MenuGroup;
		}

		private Label? CreateMenuLabel(CreateN1Command command, int defaultLanguageCode)
		{
			if (command.MenuBehavior != AssociatedMenuBehavior.UseLabel)
				return null;

			if (string.IsNullOrWhiteSpace(command.MenuLabel))
				throw new CommandException(CommandException.CommandInvalidArgumentValue, "The menu label is required when the menu behavior is set to UseLabel");

			return new Label(command.MenuLabel, defaultLanguageCode);
		}

		private static string CreateRelationshipSchemaName(CreateN1Command command, string publisherPrefix)
		{
			if (!string.IsNullOrWhiteSpace(command.RelationshipName))
			{
				if (!command.RelationshipName.StartsWith(publisherPrefix + "_"))
					throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The relationship name must start with the publisher prefix. Current publisher prefix is <{publisherPrefix}>, provided value is <{command.RelationshipName.Split("_").FirstOrDefault()}>");

				return command.RelationshipName;
			}


			var sb = new StringBuilder();
			sb.Append(publisherPrefix);
			sb.Append('_');
			

			var childTable = command.ChildTable?? string.Empty;
			if (childTable.StartsWith(publisherPrefix + "_"))
				sb.Append(childTable.AsSpan(publisherPrefix.Length + 1));
			else
				sb.Append(childTable);

			sb.Append('_');

			var parentTable = command.ParentTable ?? string.Empty;
			if (parentTable.StartsWith(publisherPrefix + "_"))
				sb.Append(parentTable.AsSpan(publisherPrefix.Length + 1));
			else
				sb.Append(parentTable);

			if (!string.IsNullOrWhiteSpace(command.RelationshipNameSuffix))
			{
				sb.Append('_');
				sb.Append(command.RelationshipNameSuffix);
			}

			return sb.ToString();
		}

		private static string CreateLookupSchemaName(CreateN1Command command, string publisherPrefix)
		{
			if (!string.IsNullOrWhiteSpace(command.LookupAttributeSchemaName))
			{
				if (!command.LookupAttributeSchemaName.StartsWith(publisherPrefix + "_"))
					throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The primary attribute schema name must start with the publisher prefix. Current publisher prefix is <{publisherPrefix}>, provided value is <{command.LookupAttributeSchemaName.Split("_").FirstOrDefault()}>");

				return command.LookupAttributeSchemaName;
			}

			if (!string.IsNullOrWhiteSpace(command.LookupAttributeDisplayName))
			{
				var namePart = command.LookupAttributeDisplayName.OnlyLettersNumbersOrUnderscore();
				if (string.IsNullOrWhiteSpace(namePart))
					throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, $"Is not possible to infer the primary attribute schema name from the display name, please explicit a primary attribute schema name");

				return $"{publisherPrefix}_{namePart}";
			}

			if (command.ParentTable == null)
				throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, $"Is not possible to infer the primary attribute schema name from the parent table name, please explicit a primary attribute schema name");

			if (command.ParentTable.StartsWith(publisherPrefix + "_")) 
				return $"{command.ParentTable}id";

			if (!command.ParentTable.Contains("_"))
				return $"{publisherPrefix}_{command.ParentTable}id";

			var parentTableParts = command.ParentTable.Split("_");
			if (parentTableParts.Length != 2)
				throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, $"Is not possible to infer the primary attribute schema name from the parent table name, please explicit a primary attribute schema name");

			if (string.IsNullOrWhiteSpace(parentTableParts[1]))
				throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, $"Is not possible to infer the primary attribute schema name from the parent table name, please explicit a primary attribute schema name");

			return $"{publisherPrefix}_{parentTableParts[1]}id";
		}


		private static async Task<Label> CreateLookupDisplayNameAsync(IOrganizationServiceAsync2 crm, IOutput output, CreateN1Command command, int defaultLanguageCode)
		{
			if (!string.IsNullOrWhiteSpace(command.LookupAttributeDisplayName))
			{
				return new Label(command.LookupAttributeDisplayName, defaultLanguageCode);
			}

			output.Write("Lookup attribute display name has not been specified. Retrieving parent table display name...");

			var parentTableName = command.ParentTable;
			var request = new RetrieveEntityRequest
			{
				LogicalName = parentTableName,
				EntityFilters = Microsoft.Xrm.Sdk.Metadata.EntityFilters.Entity,
				RetrieveAsIfPublished = false
			};

			var response = (RetrieveEntityResponse)await crm.ExecuteAsync(request);
			return response.EntityMetadata.DisplayName;
		}
	}
}
