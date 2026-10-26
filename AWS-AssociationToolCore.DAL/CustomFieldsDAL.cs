using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using AWS_AssociationToolCore.Entities;

namespace AWS_AssociationToolCore.DAL
{
    public class CustomFieldsDAL
    {
        private readonly DBAccess _db;

        public CustomFieldsDAL()
        {
            _db = new DBAccess();
        }

        public CustomFieldsDAL(IConfiguration configuration)
        {
            _db = new DBAccess(configuration);
        }

        /* ---------------- Forms ---------------- */

        public DataTable FormsGetList()
        {
            return _db.GetDataTableNoParm("FormsGetList");
        }

        public DataTable FormsGetByCode(string formCode)
        {
            SqlParameter[] p = { new SqlParameter("@FormCode", formCode) };
            return _db.GetDataTable("FormsGetByCode", ref p);
        }

        /* ---------------- CustomFields ---------------- */

        public long CustomFieldsInsert(CustomFields f, string userName, out int qStatus)
        {
            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@CustomFieldId", f.CustomFieldId),
                new SqlParameter("@FormId", f.FormId),
                new SqlParameter("@FieldLabel", f.FieldLabel),
                new SqlParameter("@FieldName", f.FieldName),
                new SqlParameter("@FieldType", f.FieldType),
                new SqlParameter("@Placeholder", (object)f.Placeholder ?? DBNull.Value),
                new SqlParameter("@DefaultValue", (object)f.DefaultValue ?? DBNull.Value),
                new SqlParameter("@HelpText", (object)f.HelpText ?? DBNull.Value),
                new SqlParameter("@IsRequired", f.IsRequired),
                new SqlParameter("@IsActive", f.IsActive),
                new SqlParameter("@DisplayOrder", f.DisplayOrder),
                new SqlParameter("@MinLength", (object)f.MinLength ?? DBNull.Value),
                new SqlParameter("@MaxLength", (object)f.MaxLength ?? DBNull.Value),
                new SqlParameter("@MinValue", (object)f.MinValue ?? DBNull.Value),
                new SqlParameter("@MaxValue", (object)f.MaxValue ?? DBNull.Value),
                new SqlParameter("@ValidationPattern", (object)f.ValidationPattern ?? DBNull.Value),
                new SqlParameter("@AllowMultiple", f.AllowMultiple),
                new SqlParameter("@AllowedFileTypes", (object)f.AllowedFileTypes ?? DBNull.Value),
                new SqlParameter("@MaxFileSizeKB", (object)f.MaxFileSizeKB ?? DBNull.Value),
                new SqlParameter("@UserName", (object)userName ?? DBNull.Value),
                new SqlParameter("@QStatus", SqlDbType.Int) { Direction = ParameterDirection.Output }
            };

            _db.GetDataTable("CustomFieldsInsert", ref p);
            qStatus = (int)p[p.Length - 1].Value;
            return f.CustomFieldId;
        }

        public DataTable CustomFieldsGetById(long customFieldId)
        {
            SqlParameter[] p = { new SqlParameter("@CustomFieldId", customFieldId) };
            return _db.GetDataTable("CustomFieldsGetById", ref p);
        }

        public DataTable CustomFieldsGetListByForm(long formId, bool onlyActive)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@FormId", formId),
                new SqlParameter("@OnlyActive", onlyActive)
            };
            return _db.GetDataTable("CustomFieldsGetListByForm", ref p);
        }

        public DataTable CustomFieldsGetListAll()
        {
            return _db.GetDataTableNoParm("CustomFieldsGetListAll");
        }

        public int CustomFieldsUpdateStatus(long customFieldId, bool isActive, string userName)
        {
            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@CustomFieldId", customFieldId),
                new SqlParameter("@IsActive", isActive),
                new SqlParameter("@UserName", (object)userName ?? DBNull.Value),
                new SqlParameter("@QStatus", SqlDbType.Int) { Direction = ParameterDirection.Output }
            };
            _db.GetDataTable("CustomFieldsUpdateStatus", ref p);
            return (int)p[p.Length - 1].Value;
        }

        public int CustomFieldsUpdateDisplayOrder(long customFieldId, int displayOrder)
        {
            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@CustomFieldId", customFieldId),
                new SqlParameter("@DisplayOrder", displayOrder),
                new SqlParameter("@QStatus", SqlDbType.Int) { Direction = ParameterDirection.Output }
            };
            _db.GetDataTable("CustomFieldsUpdateDisplayOrder", ref p);
            return (int)p[p.Length - 1].Value;
        }

        public int CustomFieldsDelete(long customFieldId)
        {
            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@CustomFieldId", customFieldId),
                new SqlParameter("@QStatus", SqlDbType.Int) { Direction = ParameterDirection.Output }
            };
            _db.GetDataTable("CustomFieldsDelete", ref p);
            return (int)p[p.Length - 1].Value;
        }

        /* ---------------- CustomFieldOptions ---------------- */

        public long CustomFieldOptionsInsert(CustomFieldOptions o, string userName, out int qStatus)
        {
            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@OptionId", o.OptionId),
                new SqlParameter("@CustomFieldId", o.CustomFieldId),
                new SqlParameter("@OptionText", o.OptionText),
                new SqlParameter("@OptionValue", o.OptionValue),
                new SqlParameter("@DisplayOrder", o.DisplayOrder),
                new SqlParameter("@IsActive", o.IsActive),
                new SqlParameter("@UserName", (object)userName ?? DBNull.Value),
                new SqlParameter("@QStatus", SqlDbType.Int) { Direction = ParameterDirection.Output }
            };
            _db.GetDataTable("CustomFieldOptionsInsert", ref p);
            qStatus = (int)p[p.Length - 1].Value;
            return o.OptionId;
        }

        public DataTable CustomFieldOptionsGetByFieldId(long customFieldId, bool onlyActive)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@CustomFieldId", customFieldId),
                new SqlParameter("@OnlyActive", onlyActive)
            };
            return _db.GetDataTable("CustomFieldOptionsGetByFieldId", ref p);
        }

        public int CustomFieldOptionsDelete(long optionId)
        {
            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@OptionId", optionId),
                new SqlParameter("@QStatus", SqlDbType.Int) { Direction = ParameterDirection.Output }
            };
            _db.GetDataTable("CustomFieldOptionsDelete", ref p);
            return (int)p[p.Length - 1].Value;
        }

        public int CustomFieldOptionsUpdateStatus(long optionId, bool isActive)
        {
            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@OptionId", optionId),
                new SqlParameter("@IsActive", isActive),
                new SqlParameter("@QStatus", SqlDbType.Int) { Direction = ParameterDirection.Output }
            };
            _db.GetDataTable("CustomFieldOptionsUpdateStatus", ref p);
            return (int)p[p.Length - 1].Value;
        }

        public int CustomFieldOptionsUpdateOrder(long optionId, int displayOrder)
        {
            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@OptionId", optionId),
                new SqlParameter("@DisplayOrder", displayOrder),
                new SqlParameter("@QStatus", SqlDbType.Int) { Direction = ParameterDirection.Output }
            };
            _db.GetDataTable("CustomFieldOptionsUpdateOrder", ref p);
            return (int)p[p.Length - 1].Value;
        }

        /* ---------------- CustomFieldConditions ---------------- */

        public long CustomFieldConditionsInsert(CustomFieldConditions c, string userName, out int qStatus)
        {
            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@ConditionId", c.ConditionId),
                new SqlParameter("@FormId", c.FormId),
                new SqlParameter("@SourceFieldId", c.SourceFieldId),
                new SqlParameter("@Operator", c.Operator),
                new SqlParameter("@ComparisonValue", (object)c.ComparisonValue ?? DBNull.Value),
                new SqlParameter("@TargetFieldId", c.TargetFieldId),
                new SqlParameter("@ActionType", c.ActionType),
                new SqlParameter("@UpdatedValue", (object)c.UpdatedValue ?? DBNull.Value),
                new SqlParameter("@IsActive", c.IsActive),
                new SqlParameter("@UserName", (object)userName ?? DBNull.Value),
                new SqlParameter("@QStatus", SqlDbType.Int) { Direction = ParameterDirection.Output }
            };
            _db.GetDataTable("CustomFieldConditionsInsert", ref p);
            qStatus = (int)p[p.Length - 1].Value;
            return c.ConditionId;
        }

        public DataTable CustomFieldConditionsGetByForm(long formId, bool onlyActive)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@FormId", formId),
                new SqlParameter("@OnlyActive", onlyActive)
            };
            return _db.GetDataTable("CustomFieldConditionsGetByForm", ref p);
        }

        public int CustomFieldConditionsDelete(long conditionId)
        {
            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@ConditionId", conditionId),
                new SqlParameter("@QStatus", SqlDbType.Int) { Direction = ParameterDirection.Output }
            };
            _db.GetDataTable("CustomFieldConditionsDelete", ref p);
            return (int)p[p.Length - 1].Value;
        }

        /* ---------------- CustomFieldValues ---------------- */

        public int CustomFieldValuesSave(long customFieldId, long formId, long recordId, string fieldValue)
        {
            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@CustomFieldId", customFieldId),
                new SqlParameter("@FormId", formId),
                new SqlParameter("@RecordId", recordId),
                new SqlParameter("@FieldValue", (object)fieldValue ?? DBNull.Value),
                new SqlParameter("@QStatus", SqlDbType.Int) { Direction = ParameterDirection.Output }
            };
            _db.GetDataTable("CustomFieldValuesSave", ref p);
            return (int)p[p.Length - 1].Value;
        }

        public DataTable CustomFieldValuesGetByRecord(long formId, long recordId)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@FormId", formId),
                new SqlParameter("@RecordId", recordId)
            };
            return _db.GetDataTable("CustomFieldValuesGetByRecord", ref p);
        }

        public int CustomFieldValuesDeleteByRecord(long formId, long recordId)
        {
            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@FormId", formId),
                new SqlParameter("@RecordId", recordId),
                new SqlParameter("@QStatus", SqlDbType.Int) { Direction = ParameterDirection.Output }
            };
            _db.GetDataTable("CustomFieldValuesDeleteByRecord", ref p);
            return (int)p[p.Length - 1].Value;
        }



        // Paged/searched/sorted list for the AJAX grid (CustomFieldsController.List).
        // formId = 0 means "all forms". @Total is returned as an output param, same
        // convention as @QStatus elsewhere in this class.
        public DataTable CustomFieldsGetListByVariable(long formId, string search, string sort, int pageNo, int items, out int total)
        {
            SqlParameter[] p = new SqlParameter[]
            {
        new SqlParameter("@FormId", formId),
        new SqlParameter("@Search", (object)search ?? DBNull.Value),
        new SqlParameter("@Sort", (object)sort ?? DBNull.Value),
        new SqlParameter("@PageNo", pageNo),
        new SqlParameter("@Items", items),
        new SqlParameter("@Total", SqlDbType.Int) { Direction = ParameterDirection.Output }
            };
            DataTable dt = _db.GetDataTable("CustomFieldsGetListByVariable", ref p);
            total = p[p.Length - 1].Value == DBNull.Value || p[p.Length - 1].Value == null
                ? 0
                : (int)p[p.Length - 1].Value;
            return dt;
        }


    }
}
