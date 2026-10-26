using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Web;
//using System.Web.Security;

namespace AWS_AssociationToolCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[Models.SessionClass.PermitAccess(Roles = "SuperAdmin,Enquiries,ChapterAdmin,SiteAdmin,Administrator,DeveloperAdmin,")]
    public class EnquiryController : Controller
    {
        AWS_AssociationToolCore.BLL.Enquiries _Enquiry = new AWS_AssociationToolCore.BLL.Enquiries();
        AWS_AssociationToolCore.Entities.Enquiries objRequestCalls = new AWS_AssociationToolCore.Entities.Enquiries();
        AWS_AssociationToolCore.DAL.Enquiries _DEnquiries = new AWS_AssociationToolCore.DAL.Enquiries();
        BLL.Events _Events = new BLL.Events();
        BLL.Users _users = new BLL.Users();

        [Authorize]
        public IActionResult Index(long mid = 0)
        {
            long userId = 0;

            var userIdStr = HttpContext.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userIdStr))
            {
                long.TryParse(userIdStr, out userId);
            }

            if (Request.Cookies.TryGetValue("UserRole", out string userRoleValue))
            {
                HttpContext.Session.SetString("userrole", userRoleValue);
            }
            long chapterId = 0;

            var chapterValue = HttpContext.Session.GetString("chapterid");

            if (!string.IsNullOrEmpty(chapterValue))
            {
                long.TryParse(chapterValue, out chapterId);
            }

            int qstatus = 0;
            int status = 0;

            var objuserroles = new Entities.UserRoles();

            // ✅ Get event list
            ViewBag.eventlist = _Events.GetEnquiryEventsListall(ref qstatus);

            // ✅ Get role permissions
            objuserroles = _users.GetRoleDetialsById(userId, mid, ref status);

            // ✅ Store in session (Core requires typed methods)
            HttpContext.Session.SetString("IsEdit", objuserroles.IsEdit.ToString());
            HttpContext.Session.SetString("IsView", objuserroles.IsView.ToString());
            HttpContext.Session.SetString("IsDelete", objuserroles.IsDelete.ToString());
            HttpContext.Session.SetString("IsExport", objuserroles.IsExport.ToString());
            HttpContext.Session.SetString("IsAdd", objuserroles.IsAdd.ToString());

            // ✅ Pass to ViewBag (convert back if needed)
            ViewBag.IsEdit = objuserroles.IsEdit;
            ViewBag.IsView = objuserroles.IsView;
            ViewBag.IsDelete = objuserroles.IsDelete;
            ViewBag.IsExport = objuserroles.IsExport;
            ViewBag.IsAdd = objuserroles.IsAdd;

            ViewBag.mid = mid;

            return View();
        }
        [HttpGet]
        public ActionResult EnquiriesList(Int64 EventId = 0, string FromDate="", string ToDate="",string search = "", string SortColumn = "", string SortOrder = "DESC", int PageNo = 1, int PageItems = 25, bool IsEdit = false, bool IsView = false, bool IsDelete = false, bool IsExport = false, bool IsAdd = false, Int64 Mid = 0)
        {
            int Total = 0;
            string Sort = (SortColumn != "" ? SortColumn + " " + SortOrder : "InsertedTime DESC");
            List<Entities.Enquiries> lstEnquiries = new List<Entities.Enquiries>();
            try
            {
                lstEnquiries = _Enquiry.GetEnquiriesListByVariable(EventId, FromDate, ToDate, HttpUtility.UrlDecode(search.Trim()), Sort, PageNo, PageItems, ref Total);
               
            }
            catch 
            {
                TempData["messageType"] = "error";
                TempData["message"] = "Failed Transaction";
                //TempData["message"] = "<div class=\"alert alert-danger alert-dismissable\">Failed transaction.</div>";
            }

            ViewBag.total = Total;
            ViewBag.items = PageItems;
            ViewBag.pageno = PageNo;
            ViewBag.sortcolumn = SortColumn;
            ViewBag.sortorder = SortOrder.ToLower();
            ViewBag.lstEnquiries =lstEnquiries;
            ViewBag.EventId = EventId;
            ViewBag.IsEdit = IsEdit;
            ViewBag.IsView = IsView;
            ViewBag.IsDelete = IsDelete;
            ViewBag.IsExport = IsExport;
            ViewBag.IsAdd = IsAdd;
            ViewBag.Mid = Mid;
            return View();
        }

        public JsonResult DeleteEnquiry(Int64 EnquiryId)
        {
            string str = "";
            try
            {
                Int64 _status = _Enquiry.DeleteEnquiry(EnquiryId);
                if (_status == 1)
                {
                    return Json(new
                    {
                        ok = true,
                        messageType = "success",
                        message = "Record Deleted successfully"
                    });
                   
                }
                else
                {
                    return Json(new
                    {
                        ok = false,
                        messageType = "error",
                        message = "Failed Deleting the Record"
                    });
                   
                }
            }
            catch 
            {
                return Json(new
                {
                    ok = false,
                    messageType = "error",
                    message = "Failed transaction."
                });
              
            }
        }

        public ActionResult ViewEnquiry(Int64 EnquiryId, Int64 mid = 0)
        {
            Entities.Enquiries objEnquiriy = new Entities.Enquiries();
            int status = 0;
            try
            {
                objEnquiriy = _Enquiry.GetEnquirysById(EnquiryId, ref status);

                if (objEnquiriy == null)
                {
                    TempData["messageType"] = "error";
                    TempData["message"] = "Sorry, failed processing your request.";
                    return RedirectToAction("Index", "Enquiry", new { mid = mid });
                }
                
            }
            catch
            {
                TempData["messageType"] = "error";
                TempData["message"] = "Sorry, failed processing your request.";
                return RedirectToAction("Index", "Enquiry", new { mid = mid });
            }

            ViewBag.objEnquiriy = objEnquiriy;
            ViewBag.mid = mid;
            return View();
        }


        [HttpPost]
        public JsonResult EnquiryStatus(Int64 EnquiryId)
        {
            string str = "";
            try
            {
                Int64 _status = _Enquiry.UpdateEnquiriesStatus(EnquiryId);
                if (_status == 1)
                {
                    return Json(new { ok = true, messageType = "success", message = "Updated Status Successfully" });

                }
                else
                {
                    return Json(new { ok = false, messageType = "error", message = "Failed updating status" });

                }
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, messageType = "error", message = ex.Message });
            }
        }

        #region export to excel

        #region export to excel

        [Authorize]
        [HttpGet]
        public IActionResult GetEnquiryExportFields()
        {
            try
            {
                DataTable dt = _DEnquiries.ExportToEnquiries(0, "", "", "", "InsertedTime DESC");

                var staticCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "EnquiryId", "Name", "Email", "Description", "Subject", "Phone", "PhoneNo", "Status", "IsActive",
            "Date", "InsertedTime", "PostedDate", "ChapterName", "Field1", "Field2"
        };
                var skipCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Rid", "EventId" };

                var list = new List<object>();
                foreach (DataColumn col in dt.Columns)
                {
                    if (skipCols.Contains(col.ColumnName)) continue;

                    bool isStatic = staticCols.Contains(col.ColumnName);
                    list.Add(new
                    {
                        fieldKey = col.ColumnName,
                        displayName = Regex.Replace(col.ColumnName, "(?<=[a-z])(?=[A-Z])", " "),
                        section = isStatic ? "Enquiry Details" : "Custom Fields"
                    });
                }

                // ---- Custom Fields (File type tappa migatavi, SP lo already unnavi skip) ----
                BLL.CustomFields _customFields = new BLL.CustomFields();
                var form = _customFields.GetFormByCode(AWS_AssociationToolCore.Entities.CustomFieldFormCodes.Enquiries);
                if (form != null)
                {
                    var fields = _customFields.GetCustomFieldsByForm(form.FormId, onlyActive: true);
                    foreach (var f in fields)
                    {
                        if (f.FieldType == "File") continue;
                        if (dt.Columns.Contains((string)f.FieldLabel)) continue; // already SP nunchi vasthundi

                        list.Add(new
                        {
                            fieldKey = "CF_" + f.CustomFieldId,
                            displayName = f.FieldLabel,   // label property peru vere aithe ikkada marchandi
                            section = "Custom Fields"
                        });
                    }
                }

                return Json(new { ok = true, data = list });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        [Authorize]
        public IActionResult EnquiriesExporttoExcel(
            long eventId = 0,
            string fromDate = "",
            string toDate = "",
            string search = "",
            string sortColumn = "InsertedTime",
            string sortOrder = "DESC",
            string SelectedFields = "")
        {
            try
            {
                string sort = !string.IsNullOrEmpty(sortColumn) ? $"{sortColumn} {sortOrder}" : "";

                DataTable dtUni = _DEnquiries.ExportToEnquiries(eventId, fromDate, toDate, search, sort);

                // ---- Custom fields load (File type skip, SP lo already unnavi skip) ----
                BLL.CustomFields _customFields = new BLL.CustomFields();
                var form = _customFields.GetFormByCode(AWS_AssociationToolCore.Entities.CustomFieldFormCodes.Enquiries);
                var cfMap = new Dictionary<string, dynamic>(StringComparer.OrdinalIgnoreCase); // "CF_id" -> field
                if (form != null)
                {
                    foreach (var f in _customFields.GetCustomFieldsByForm(form.FormId, onlyActive: true))
                    {
                        if (f.FieldType == "File") continue;
                        if (dtUni.Columns.Contains((string)f.FieldLabel)) continue;
                        cfMap["CF_" + f.CustomFieldId] = f;
                    }
                }

                // Record id column (custom values lookup kosam)
                string idCol = dtUni.Columns.Contains("EnquiryId") ? "EnquiryId"
                             : dtUni.Columns.Contains("Id") ? "Id"
                             : null;

                var skipCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Rid", "EventId" };
                List<string> columnsToCopy;

                if (string.IsNullOrWhiteSpace(SelectedFields))
                {
                    // All DB columns + all custom fields
                    columnsToCopy = dtUni.Columns.Cast<DataColumn>()
                        .Select(c => c.ColumnName)
                        .Where(n => !skipCols.Contains(n))
                        .ToList();
                    columnsToCopy.AddRange(cfMap.Keys);
                }
                else
                {
                    columnsToCopy = SelectedFields
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(k => k.Trim())
                        .Where(k => k != "" && (dtUni.Columns.Contains(k) || cfMap.ContainsKey(k)))
                        .ToList();
                }

                if (columnsToCopy.Count == 0)
                {
                    TempData["messageType"] = "error";
                    TempData["message"] = "No fields found to export.";
                    return RedirectToAction("Index");
                }

                bool needCustom = idCol != null && columnsToCopy.Any(k => cfMap.ContainsKey(k));
                var valuesCache = new Dictionary<long, Dictionary<long, string>>();

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Enquiries-Export");

                    // Header row
                    for (int c = 0; c < columnsToCopy.Count; c++)
                    {
                        string key = columnsToCopy[c];
                        var cell = worksheet.Cells[1, c + 1];
                        cell.Value = cfMap.ContainsKey(key)
                            ? (string)cfMap[key].FieldLabel
                            : Regex.Replace(key, "(?<=[a-z])(?=[A-Z])", " ");
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(173, 216, 230));
                    }

                    // Data rows
                    for (int r = 0; r < dtUni.Rows.Count; r++)
                    {
                        Dictionary<long, string> cfValues = null;
                        if (needCustom)
                        {
                            long recordId = 0;
                            long.TryParse(dtUni.Rows[r][idCol].ToString(), out recordId);
                            if (!valuesCache.TryGetValue(recordId, out cfValues))
                            {
                                cfValues = _customFields.GetValuesForRecord(form.FormId, recordId)
                                           ?? new Dictionary<long, string>();
                                valuesCache[recordId] = cfValues;
                            }
                        }

                        for (int c = 0; c < columnsToCopy.Count; c++)
                        {
                            string key = columnsToCopy[c];
                            var cell = worksheet.Cells[r + 2, c + 1];

                            if (cfMap.ContainsKey(key))
                            {
                                string val = null;
                                cfValues?.TryGetValue((long)cfMap[key].CustomFieldId, out val);
                                cell.Value = val ?? "";
                            }
                            else
                            {
                                var val = dtUni.Rows[r][key];
                                cell.Value = val == DBNull.Value ? "" : val;
                                if (val is DateTime)
                                {
                                    cell.Style.Numberformat.Format = "yyyy-MM-dd HH:mm";
                                }
                            }
                        }
                    }

                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    var content = package.GetAsByteArray();
                    string fileName = $"Enquiries-Export-{DateTime.UtcNow:dd-MM-yyyy}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                TempData["messageType"] = "error";
                TempData["message"] = ex.Message;
                return RedirectToAction("Index");
            }
        }
        #endregion
        [Authorize]
        #region export to excel
        public IActionResult EnquiriesExporttoExcelbkp(
    long eventId = 0,
    string fromDate = "",
    string toDate = "",
    string search = "",
    string sortColumn = "InsertedTime",
    string sortOrder = "DESC")
        {
            try
            {
                string sort = !string.IsNullOrEmpty(sortColumn)
                                ? $"{sortColumn} {sortOrder}"
                                : "";

                DataTable dtUni = _DEnquiries.ExportToEnquiries(
                    eventId,
                    fromDate,
                    toDate,
                    search,
                    sort
                );

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Enquiries-Export");

                    // Load DataTable headers
                    for (int col = 0; col < dtUni.Columns.Count; col++)
                    {
                        worksheet.Cells[1, col + 1].Value = dtUni.Columns[col].ColumnName;
                        // Style header row
                        worksheet.Cells[1, col + 1].Style.Font.Bold = true;
                        worksheet.Cells[1, col + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(
                            System.Drawing.Color.FromArgb(173, 216, 230)); // light blue
                    }

                    // Load DataTable rows
                    for (int row = 0; row < dtUni.Rows.Count; row++)
                    {
                        for (int col = 0; col < dtUni.Columns.Count; col++)
                        {
                            worksheet.Cells[row + 2, col + 1].Value = dtUni.Rows[row][col];
                        }
                    }

                    // Auto-fit columns
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    var content = package.GetAsByteArray();
                    string fileName = $"Enquiries-Export-{DateTime.UtcNow:dd-MM-yyyy}.xlsx";

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName
                    );
                }
            }
            catch (Exception ex)
            {
                TempData["messageType"] = "error";
                TempData["message"] = ex.Message;
                return RedirectToAction("Index");
            }
        }
        #endregion
        #endregion

        public class AuthorizeAttribute : ActionFilterAttribute
        {
            BLL.Users _user = new BLL.Users();
            BLL.Roles _Roles = new BLL.Roles();
            private object FormsAuthentication;
            public override void OnActionExecuting(ActionExecutingContext filterContext)
            {
                string userRole = null;
                int status = 0;

                // ✅ Get ClaimsPrincipal — replaces HttpCookie + FormsAuthentication.Decrypt
                var user = filterContext.HttpContext.User;

                if (user?.Identity != null && user.Identity.IsAuthenticated)
                {
                    // ✅ Read role from Claims — replaces authTicket.UserData
                    userRole = user.FindFirst(ClaimTypes.Role)?.Value;

                    // ✅ Read email from Claims — replaces authTicket.Name
                    string emailFromClaim = user.FindFirst(ClaimTypes.Email)?.Value;

                    if (!string.IsNullOrEmpty(emailFromClaim))
                    {
                        // Get user from DB
                        var objuser = _user.GetAdminUsersGetByEmail(
                            emailFromClaim, ref status);

                        // Inside AuthorizeAttribute, where objuser != null:
                        if (objuser != null)
                        {
                            filterContext.HttpContext.Session.SetString("UserName", objuser.UserName ?? "");
                            filterContext.HttpContext.Session.SetString("UserId", objuser.UserId.ToString());
                            filterContext.HttpContext.Session.SetString("UserEmail", emailFromClaim ?? ""); // ✅ ADD THIS
                            filterContext.HttpContext.Session.SetString("chapterid", objuser.ChapterId.ToString());
                            filterContext.HttpContext.Session.SetString("userrole", userRole ?? "");
                        }
                    }
                }

                // ✅ If no role — redirect to LogOn
                //    replaces new UrlHelper(filterContext.RequestContext)
                if (string.IsNullOrEmpty(userRole))
                {
                    filterContext.Result = new RedirectToActionResult(
                        "LogOn", "Account", new { area = "Admin" });
                    return;
                }

                // ✅ Get all allowed roles from DB
                int roleStatus = 0;
                List<Entities.Roles> lstRoles = _Roles.GetRolesList(ref roleStatus);

                // Build flat list of allowed role names
                List<string> allowedRoles = lstRoles
                    .Select(r => r.RoleName.Trim())
                    .ToList();

                // Split user's roles (comma separated) and check
                var userRoles = userRole
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim())
                    .ToList();

                bool isAuthorized = userRoles.Any(r => allowedRoles.Contains(r));

                if (!isAuthorized)
                {
                    // ✅ Redirect to Unauthorized — replaces UrlHelper + RedirectResult
                    filterContext.Result = new RedirectToActionResult(
                        "Unauthorized", "Account", new { area = "Admin" });
                    return;
                }

                base.OnActionExecuting(filterContext);
            }
        }
    }
}
