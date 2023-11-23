﻿using Greg.Xrm.Command.Parsing;
using Microsoft.Xrm.Sdk.Metadata;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Column
{
	[Command("column", "create", HelpText = "Creates a new column on a given Dataverse table")]
	[Alias("create", "column")]
	public class CreateCommand
	{
		[Option("table", "t", HelpText = "The name of the entity for which you want to create an attribute")]
		[Required]
		public string? EntityName { get; set; }

		[Option("solution", "s", HelpText = "The name of the unmanaged solution to which you want to add this attribute.")]
		public string? SolutionName { get; set; }

		[Option("name", "n", HelpText = "The display name of the attribute.")]
		[Required]
		public string? DisplayName { get; set; }

		[Option("schemaName", "sn", HelpText = "The schema name of the attribute.\nIf not specified, is deducted from the display name")]
		public string? SchemaName { get; set; }

		[Option("description", "d", HelpText = "The description of the attribute.")]
		public string? Description { get; set; }

		[Option("type", "at", HelpText = "The type of the attribute. Default: string")]
		public AttributeTypeCode AttributeType { get; set; } = AttributeTypeCode.String;

		[Option("stringFormat", "sf", HelpText = "The format of the string attribute (default: Text).")]
		public StringFormat StringFormat { get; set; } = StringFormat.Text;

		[Option("intFormat", "if", HelpText = "For whole number type columns indicates the integer format for the column.(default: None)")]
		public IntegerFormat IntegerFormat { get; set; } = IntegerFormat.None;

		[Option("requiredLevel", "r", HelpText = "The required level of the attribute.")]
		public AttributeRequiredLevel RequiredLevel { get; set; } = AttributeRequiredLevel.None;

		[Option("len", "l", HelpText = "The maximum length for string attribute.", SuppressValuesHelp = true)]
		public int? MaxLength { get; set; }

		[Option("autoNumber", "an", HelpText = "In case of autonumber field, the autonumber format to apply.")]
		public string? AutoNumber { get; set; }

		[Option("audit", "a", HelpText = "Indicates whether the attribute is enabled for auditing (default: true).")]
		public bool IsAuditEnabled { get; set; } = true;

		[Option("options", "o", HelpText = "The list of options for the attribute, as a single string separated by comma or pipe.\nValues will be automatically generated")]
		public string? Options { get; internal set; }

		[Option("globalOptionSetName", "gon", HelpText = "For Picklist type columns that must be tied to a global option set,\nprovides the name of the global option set.")]
		public string? GlobalOptionSetName { get; set; }

		[Option("multiselect", "m", HelpText = "Indicates whether the attribute is a multi-select picklist (default: false).", DefaultValue = false)]
		public bool Multiselect { get; set; } = false;

		[Option("min", "min", HelpText = "For number type columns indicates the minimum value for the column.", SuppressValuesHelp = true)]
		public double? MinValue { get; set; }

		[Option("max", "max", HelpText = "For number type columns indicates the maximum value for the column.", SuppressValuesHelp = true)]
		public double? MaxValue { get; set; }

        [Option("precision", "p", HelpText = "For money type columns indicates the precision for the column (default: 2).", SuppressValuesHelp = true)]
        public int? Precision { get; set; }

        [Option("precisionSource", "ps", HelpText = "For money type columns indicates the precision source for the column  (default: 2).", SuppressValuesHelp = true)]
        public int? PrecisionSource{ get; set; }

		[Option("imeMode", "ime", HelpText = "For number/DateTime type columns indicates the input method editor (IME) mode for the column. (default: Disabled)")]
		public ImeMode ImeMode { get; set; } = ImeMode.Disabled;
        [Option("dtFormat", "dtf", HelpText = "For DateTime type columns indicates the DateTimeFormat for the column. (default: DateAndTime)")]
        public DateTimeFormat DateTimeFormat { get; set; } = DateTimeFormat.DateAndTime;

        [Option("trueLabel", "tl", HelpText = "For Boolean type columns that represents the Label to be associated to the \"true\" value. (default: \"true\")")]
		public string? TrueLabel { get; set; } = "true";

        [Option("falseLabel", "fl", HelpText = "For  Boolean type columns that represents the Label to be associated to the \"false\" value.(default: \"false\")")]
		public string? FalseLabel { get; set; } = "false";


    }
}
