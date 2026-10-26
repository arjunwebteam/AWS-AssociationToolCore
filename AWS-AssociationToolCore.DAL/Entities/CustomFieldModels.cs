using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AWS_AssociationToolCore.DAL;

namespace AWS_AssociationToolCore.Entities
{
    public class FormMaster
    {
        public Int64 RId { get; set; }
        public Int64 FormId { get; set; }
        public string FormName { get; set; }
        public string FormCode { get; set; }
        public bool IsActive { get; set; }
    }

    public class CustomFields
    {
        public Int64 RId { get; set; }
        public Int64 CustomFieldId { get; set; }
        public Int64 FormId { get; set; }
        public string FormName { get; set; }
        public string FieldLabel { get; set; }
        public string FieldName { get; set; }
        public string FieldType { get; set; }
        public string Placeholder { get; set; }
        public string DefaultValue { get; set; }
        public string HelpText { get; set; }
        public bool IsRequired { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public string ValidationPattern { get; set; }
        public bool AllowMultiple { get; set; }
        public string AllowedFileTypes { get; set; }
        public int? MaxFileSizeKB { get; set; }
        public string InsertedBy { get; set; }
        public DateTime InsertedTime { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedTime { get; set; }

        // Populated at render time, not persisted directly on this table
        public List<CustomFieldOptions> Options { get; set; } = new List<CustomFieldOptions>();
    }

    public class CustomFieldOptions
    {
        public Int64 RId { get; set; }
        public Int64 OptionId { get; set; }
        public Int64 CustomFieldId { get; set; }
        public string OptionText { get; set; }
        public string OptionValue { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class CustomFieldConditions
    {
        public Int64 RId { get; set; }
        public Int64 ConditionId { get; set; }
        public Int64 FormId { get; set; }
        public Int64 SourceFieldId { get; set; }
        public string SourceFieldName { get; set; }
        public string SourceFieldLabel { get; set; }
        public string Operator { get; set; }
        public string ComparisonValue { get; set; }
        public Int64 TargetFieldId { get; set; }
        public string TargetFieldName { get; set; }
        public string TargetFieldLabel { get; set; }
        public string ActionType { get; set; }
        public string UpdatedValue { get; set; }
        public bool IsActive { get; set; }
    }

    public class CustomFieldValues
    {
        public Int64 RId { get; set; }
        public Int64 ValueId { get; set; }
        public Int64 CustomFieldId { get; set; }
        public string FieldName { get; set; }
        public string FieldLabel { get; set; }
        public string FieldType { get; set; }
        public Int64 FormId { get; set; }
        public Int64 RecordId { get; set; }
        public string FieldValue { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    /// <summary>
    /// Well-known FormCode constants so callers don't sprinkle magic strings
    /// around existing controllers. Add a new line here (matching a Forms row)
    /// whenever a new form should support custom fields.
    /// </summary>



    public static class CustomFieldFormCodes
    {
        public const string Donation = "DONATION";
        public const string CulturalRegistrations = "CULTURAL_REG";
        public const string Membership = "MEMBERSHIP";
        public const string EventRegistration = "EVENT_REG";
        public const string Volunteer = "VOLUNTEER";
       
        public const string Enquiries = "ENQUIRIES";
        public const string CulturalRegistration = "CULTURAL_REG";


     
    }

    

    /// <summary>
    /// Composite view model used to render + post back a dynamic block of
    /// custom fields alongside an existing form.
    /// </summary>
    public class DynamicFormViewModel
    {
        public Int64 FormId { get; set; }
        public string FormCode { get; set; }
        public Int64 RecordId { get; set; }
        public List<CustomFields> Fields { get; set; } = new List<CustomFields>();
        public List<CustomFieldConditions> Conditions { get; set; } = new List<CustomFieldConditions>();
        public Dictionary<Int64, string> Values { get; set; } = new Dictionary<Int64, string>();
    }
}
