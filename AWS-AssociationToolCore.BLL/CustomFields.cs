using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AWS_AssociationToolCore.DAL;
using AWS_AssociationToolCore.Entities;

namespace AWS_AssociationToolCore.BLL
{
    public class CustomFields
    {
        private readonly CustomFieldsDAL _dal;

        public CustomFields()
        {
            _dal = new CustomFieldsDAL();
        }

        /* ---------------- Forms ---------------- */

        public List<FormMaster> GetForms()
        {
            DataTable dt = _dal.FormsGetList();
            return MapForms(dt);
        }

        public FormMaster GetFormByCode(string formCode)
        {
            DataTable dt = _dal.FormsGetByCode(formCode);
            var list = MapForms(dt);
            return list.FirstOrDefault();
        }

        private List<FormMaster> MapForms(DataTable dt)
        {
            var list = new List<FormMaster>();
            if (dt == null) return list;
            foreach (DataRow r in dt.Rows)
            {
                list.Add(new FormMaster
                {
                    FormId = Convert.ToInt64(r["FormId"]),
                    FormName = r["FormName"].ToString(),
                    FormCode = r["FormCode"].ToString(),
                    IsActive = Convert.ToBoolean(r["IsActive"])
                });
            }
            return list;
        }

        /* ---------------- CustomFields ---------------- */

        /// <summary>
        /// Validates and saves a field. Returns (success, message, qStatus).
        /// Business rules enforced here (not in the DAL/SQL):
        ///   - FieldName must be a valid identifier-style key (letters, numbers, underscore)
        ///   - Choice-type fields (Dropdown/Radio/Checkbox/MultiSelect) get AllowMultiple auto-set
        ///   - Min/Max sanity: Min cannot exceed Max
        /// </summary>
        public (bool Success, string Message, int QStatus) SaveCustomField(Entities.CustomFields field, string userName)
        {
            int qStatus;

            if (string.IsNullOrWhiteSpace(field.FieldLabel))
                return (false, "Field Label is required.", 0);

            if (string.IsNullOrWhiteSpace(field.FieldName) ||
                !System.Text.RegularExpressions.Regex.IsMatch(field.FieldName, @"^[A-Za-z][A-Za-z0-9_]*$"))
                return (false, "Field Name must start with a letter and contain only letters, numbers, or underscores.", 0);

            if (field.MinLength.HasValue && field.MaxLength.HasValue && field.MinLength > field.MaxLength)
                return (false, "Minimum Length cannot be greater than Maximum Length.", 0);

            if (field.MinValue.HasValue && field.MaxValue.HasValue && field.MinValue > field.MaxValue)
                return (false, "Minimum Value cannot be greater than Maximum Value.", 0);

            var choiceTypes = new[] { "Dropdown", "Radio", "Checkbox", "MultiSelect" };
            if (field.FieldType == "MultiSelect")
                field.AllowMultiple = true;
            else if (choiceTypes.Contains(field.FieldType) == false)
                field.AllowMultiple = field.FieldType == "File" && field.AllowMultiple; // allow multi-file if explicitly set

            long id = _dal.CustomFieldsInsert(field, userName, out qStatus);

            string message = qStatus switch
            {
                1 => "Custom field created successfully.",
                2 => "Custom field updated successfully.",
                3 => "A field with this Field Name already exists on the selected form.",
                _ => "Failed to save the custom field."
            };

            return (qStatus == 1 || qStatus == 2, message, qStatus);
        }

        public Entities.CustomFields GetCustomFieldById(long customFieldId)
        {
            DataTable dt = _dal.CustomFieldsGetById(customFieldId);
            var field = MapCustomFields(dt).FirstOrDefault();
            if (field != null)
                field.Options = GetOptionsByFieldId(field.CustomFieldId, false);
            return field;
        }

        public List<Entities.CustomFields> GetCustomFieldsByForm(long formId, bool onlyActive)
        {
            DataTable dt = _dal.CustomFieldsGetListByForm(formId, onlyActive);
            var fields = MapCustomFields(dt);

            // attach options for choice-type fields in one shot to avoid N+1 round trips
            var choiceTypes = new[] { "Dropdown", "Radio", "Checkbox", "MultiSelect" };
            foreach (var f in fields.Where(f => choiceTypes.Contains(f.FieldType)))
                f.Options = GetOptionsByFieldId(f.CustomFieldId, onlyActive);

            return fields;
        }

        public List<Entities.CustomFields> GetCustomFieldsByFormCode(string formCode, bool onlyActive)
        {
            var form = GetFormByCode(formCode);
            if (form == null) return new List<Entities.CustomFields>();
            return GetCustomFieldsByForm(form.FormId, onlyActive);
        }

        public List<Entities.CustomFields> GetAllCustomFields()
        {
            DataTable dt = _dal.CustomFieldsGetListAll();
            return MapCustomFields(dt);
        }

        public (bool Success, string Message) UpdateStatus(long customFieldId, bool isActive, string userName)
        {
            int qStatus = _dal.CustomFieldsUpdateStatus(customFieldId, isActive, userName);
            return (qStatus == 1, qStatus == 1 ? "Status updated." : "Failed to update status.");
        }

        public (bool Success, string Message) UpdateDisplayOrder(long customFieldId, int displayOrder)
        {
            int qStatus = _dal.CustomFieldsUpdateDisplayOrder(customFieldId, displayOrder);
            return (qStatus == 1, qStatus == 1 ? "Order updated." : "Failed to update order.");
        }

        public (bool Success, string Message) DeleteCustomField(long customFieldId)
        {
            int qStatus = _dal.CustomFieldsDelete(customFieldId);
            return (qStatus == 1, qStatus == 1
                ? "Custom field deleted."
                : "Failed to delete the custom field. It may still be referenced elsewhere.");
        }

        private List<Entities.CustomFields> MapCustomFields(DataTable dt)
        {
            var list = new List<Entities.CustomFields>();
            if (dt == null) return list;
            foreach (DataRow r in dt.Rows)
            {
                list.Add(new Entities.CustomFields
                {
                    RId = Convert.ToInt64(r["RId"]),
                    CustomFieldId = Convert.ToInt64(r["CustomFieldId"]),
                    FormId = Convert.ToInt64(r["FormId"]),
                    FormName = dt.Columns.Contains("FormName") ? r["FormName"].ToString() : "",
                    FieldLabel = r["FieldLabel"].ToString(),
                    FieldName = r["FieldName"].ToString(),
                    FieldType = r["FieldType"].ToString(),
                    Placeholder = r["Placeholder"] as string,
                    DefaultValue = r["DefaultValue"] as string,
                    HelpText = r["HelpText"] as string,
                    IsRequired = Convert.ToBoolean(r["IsRequired"]),
                    IsActive = Convert.ToBoolean(r["IsActive"]),
                    DisplayOrder = Convert.ToInt32(r["DisplayOrder"]),
                    MinLength = r["MinLength"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["MinLength"]),
                    MaxLength = r["MaxLength"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["MaxLength"]),
                    MinValue = r["MinValue"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["MinValue"]),
                    MaxValue = r["MaxValue"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["MaxValue"]),
                    ValidationPattern = r["ValidationPattern"] as string,
                    AllowMultiple = Convert.ToBoolean(r["AllowMultiple"]),
                    AllowedFileTypes = r["AllowedFileTypes"] as string,
                    MaxFileSizeKB = r["MaxFileSizeKB"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["MaxFileSizeKB"])
                });
            }
            return list;
        }

        /* ---------------- Options ---------------- */

        public (bool Success, string Message) SaveOption(CustomFieldOptions option, string userName)
        {
            if (string.IsNullOrWhiteSpace(option.OptionText))
                return (false, "Option Text is required.");
            if (string.IsNullOrWhiteSpace(option.OptionValue))
                option.OptionValue = option.OptionText;

            _dal.CustomFieldOptionsInsert(option, userName, out int qStatus);
            return (qStatus == 1 || qStatus == 2, qStatus == 1 ? "Option added." : qStatus == 2 ? "Option updated." : "Failed to save option.");
        }

        public List<CustomFieldOptions> GetOptionsByFieldId(long customFieldId, bool onlyActive)
        {
            DataTable dt = _dal.CustomFieldOptionsGetByFieldId(customFieldId, onlyActive);
            var list = new List<CustomFieldOptions>();
            if (dt == null) return list;
            foreach (DataRow r in dt.Rows)
            {
                list.Add(new CustomFieldOptions
                {
                    RId = Convert.ToInt64(r["RId"]),
                    OptionId = Convert.ToInt64(r["OptionId"]),
                    CustomFieldId = Convert.ToInt64(r["CustomFieldId"]),
                    OptionText = r["OptionText"].ToString(),
                    OptionValue = r["OptionValue"].ToString(),
                    DisplayOrder = Convert.ToInt32(r["DisplayOrder"]),
                    IsActive = Convert.ToBoolean(r["IsActive"])
                });
            }
            return list;
        }

        public (bool Success, string Message) DeleteOption(long optionId)
        {
            int qStatus = _dal.CustomFieldOptionsDelete(optionId);
            return (qStatus == 1, qStatus == 1 ? "Option deleted." : "Failed to delete option.");
        }

        public (bool Success, string Message) UpdateOptionStatus(long optionId, bool isActive)
        {
            int qStatus = _dal.CustomFieldOptionsUpdateStatus(optionId, isActive);
            return (qStatus == 1, qStatus == 1 ? "Status updated." : "Failed to update status.");
        }

        public (bool Success, string Message) UpdateOptionOrder(long optionId, int displayOrder)
        {
            int qStatus = _dal.CustomFieldOptionsUpdateOrder(optionId, displayOrder);
            return (qStatus == 1, qStatus == 1 ? "Order updated." : "Failed to update order.");
        }

        /* ---------------- Conditions ---------------- */

        private static readonly string[] ValidOperators =
            { "Equals", "NotEquals", "Contains", "NotContains", "GreaterThan", "LessThan", "IsEmpty", "IsNotEmpty" };

        private static readonly string[] ValidActions =
            { "Show", "Hide", "Enable", "Disable", "Require", "Optional", "UpdateValue" };

        public (bool Success, string Message) SaveCondition(CustomFieldConditions condition, string userName)
        {
            if (condition.SourceFieldId == condition.TargetFieldId)
                return (false, "Source Field and Target Field cannot be the same field.");
            if (!ValidOperators.Contains(condition.Operator))
                return (false, "Invalid operator selected.");
            if (!ValidActions.Contains(condition.ActionType))
                return (false, "Invalid action selected.");
            if (condition.ActionType == "UpdateValue" && string.IsNullOrWhiteSpace(condition.UpdatedValue))
                return (false, "Updated Value is required when action is Update Value.");
            if (condition.Operator != "IsEmpty" && condition.Operator != "IsNotEmpty" && string.IsNullOrWhiteSpace(condition.ComparisonValue))
                return (false, "Comparison Value is required for the selected operator.");

            _dal.CustomFieldConditionsInsert(condition, userName, out int qStatus);
            return (qStatus == 1 || qStatus == 2, qStatus == 1 ? "Condition added." : qStatus == 2 ? "Condition updated." : "Failed to save condition.");
        }

        public List<CustomFieldConditions> GetConditionsByForm(long formId, bool onlyActive)
        {
            DataTable dt = _dal.CustomFieldConditionsGetByForm(formId, onlyActive);
            var list = new List<CustomFieldConditions>();
            if (dt == null) return list;
            foreach (DataRow r in dt.Rows)
            {
                list.Add(new CustomFieldConditions
                {
                    RId = Convert.ToInt64(r["RId"]),
                    ConditionId = Convert.ToInt64(r["ConditionId"]),
                    FormId = Convert.ToInt64(r["FormId"]),
                    SourceFieldId = Convert.ToInt64(r["SourceFieldId"]),
                    SourceFieldName = r["SourceFieldName"].ToString(),
                    SourceFieldLabel = r["SourceFieldLabel"].ToString(),
                    Operator = r["Operator"].ToString(),
                    ComparisonValue = r["ComparisonValue"] as string,
                    TargetFieldId = Convert.ToInt64(r["TargetFieldId"]),
                    TargetFieldName = r["TargetFieldName"].ToString(),
                    TargetFieldLabel = r["TargetFieldLabel"].ToString(),
                    ActionType = r["ActionType"].ToString(),
                    UpdatedValue = r["UpdatedValue"] as string,
                    IsActive = Convert.ToBoolean(r["IsActive"])
                });
            }
            return list;
        }

        public (bool Success, string Message) DeleteCondition(long conditionId)
        {
            int qStatus = _dal.CustomFieldConditionsDelete(conditionId);
            return (qStatus == 1, qStatus == 1 ? "Condition deleted." : "Failed to delete condition.");
        }

        /* ---------------- Values (save / load for a record) ---------------- */

        /// <summary>
        /// Server-side re-validation of required/min/max rules before persisting.
        /// The client-side JS in dynamic-custom-fields.js mirrors these rules for UX,
        /// but this is the authoritative check.
        /// </summary>
        public (bool Success, List<string> Errors) ValidateAndSaveValues(
            long formId, long recordId, List<Entities.CustomFields> fields, Dictionary<string, string> postedValues)
        {
            var errors = new List<string>();

            foreach (var field in fields.Where(f => f.IsActive))
            {
                postedValues.TryGetValue(field.FieldName, out string value);
                value ??= string.Empty;

                if (field.IsRequired && string.IsNullOrWhiteSpace(value))
                {
                    errors.Add($"{field.FieldLabel} is required.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                if (field.MinLength.HasValue && value.Length < field.MinLength.Value)
                    errors.Add($"{field.FieldLabel} must be at least {field.MinLength.Value} characters.");

                if (field.MaxLength.HasValue && value.Length > field.MaxLength.Value)
                    errors.Add($"{field.FieldLabel} must be at most {field.MaxLength.Value} characters.");

                if ((field.FieldType == "Number") && decimal.TryParse(value, out decimal numVal))
                {
                    if (field.MinValue.HasValue && numVal < field.MinValue.Value)
                        errors.Add($"{field.FieldLabel} must be at least {field.MinValue.Value}.");
                    if (field.MaxValue.HasValue && numVal > field.MaxValue.Value)
                        errors.Add($"{field.FieldLabel} must be at most {field.MaxValue.Value}.");
                }

                if (!string.IsNullOrWhiteSpace(field.ValidationPattern))
                {
                    try
                    {
                        if (!System.Text.RegularExpressions.Regex.IsMatch(value, field.ValidationPattern))
                            errors.Add($"{field.FieldLabel} is not in a valid format.");
                    }
                    catch (ArgumentException) { /* bad admin-entered regex: skip rather than fail the whole submit */ }
                }
            }

            if (errors.Count > 0)
                return (false, errors);

            foreach (var field in fields.Where(f => f.IsActive))
            {
                postedValues.TryGetValue(field.FieldName, out string value);
                _dal.CustomFieldValuesSave(field.CustomFieldId, formId, recordId, value ?? string.Empty);
            }

            return (true, errors);
        }

        public Dictionary<long, string> GetValuesForRecord(long formId, long recordId)
        {
            DataTable dt = _dal.CustomFieldValuesGetByRecord(formId, recordId);
            var dict = new Dictionary<long, string>();
            if (dt == null) return dict;
            foreach (DataRow r in dt.Rows)
                dict[Convert.ToInt64(r["CustomFieldId"])] = r["FieldValue"] as string ?? string.Empty;
            return dict;
        }

        public void DeleteValuesForRecord(long formId, long recordId)
        {
            _dal.CustomFieldValuesDeleteByRecord(formId, recordId);
        }

        /// <summary>
        /// Builds the full render-ready package (fields + options + conditions + existing values)
        /// for a form/record pair. This is what the ViewComponent and GetFieldsByForm controller
        /// action both call so the two entry points can never drift apart.
        /// </summary>
        public DynamicFormViewModel GetDynamicForm(string formCode, long recordId)
        {
            var form = GetFormByCode(formCode);
            var model = new DynamicFormViewModel { FormCode = formCode, RecordId = recordId };
            if (form == null) return model;

            model.FormId = form.FormId;
            model.Fields = GetCustomFieldsByForm(form.FormId, onlyActive: true);
            model.Conditions = GetConditionsByForm(form.FormId, onlyActive: true);
            model.Values = recordId > 0 ? GetValuesForRecord(form.FormId, recordId) : new Dictionary<long, string>();
            return model;
        }



        // Backs CustomFieldsController.List: paged/searched/sorted grid, formId = 0 for "all forms".
        public List<Entities.CustomFields> GetCustomFieldsListByVariable(long formId, string search, string sort, int pageNo, int items, ref int total)
        {
            DataTable dt = _dal.CustomFieldsGetListByVariable(formId, search, sort, pageNo, items, out int totalOut);
            total = totalOut;

            var fields = MapCustomFields(dt);

            // attach options for choice-type fields in one shot to avoid N+1 round trips
            var choiceTypes = new[] { "Dropdown", "Radio", "Checkbox", "MultiSelect" };
            foreach (var f in fields.Where(f => choiceTypes.Contains(f.FieldType)))
                f.Options = GetOptionsByFieldId(f.CustomFieldId, false);

            return fields;
        }



    }
}
