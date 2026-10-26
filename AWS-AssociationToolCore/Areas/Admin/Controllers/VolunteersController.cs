using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace AWS_AssociationToolCore.Areas.Admin.Controllers
{
    //[Models.SessionClass.PermitAccess(Roles = "SuperAdmin,Volunteers,ChapterAdmin,SiteAdmin,Administrator,DeveloperAdmin,")]
    [Area("Admin")]

    public class VolunteersController : Controller
    {
        BLL.Volunteers _Volunteers = new BLL.Volunteers();
        BLL.VolunteerCategories _VolunteerCategory = new BLL.VolunteerCategories();
        List<Entities.VolunteerCategories> lstVolunteerCategory = new List<Entities.VolunteerCategories>();
        BLL.Events _Events = new BLL.Events();
        BLL.MailTemplates _MailTemplates = new BLL.MailTemplates();
        AWS_AssociationToolCore.BLL.AppInfo _appinfo = new BLL.AppInfo();
        DAL.Volunteers _DVolunteers = new DAL.Volunteers();
        BLL.Chapters _Chapters = new BLL.Chapters();
        List<Entities.Chapters> lstChapters = new List<Entities.Chapters>();
        BLL.AdminMenuItems _adminmenu = new BLL.AdminMenuItems();
        BLL.SendMail _sendmail = new BLL.SendMail();
        BLL.Users _users = new BLL.Users();


        #region Volunteers

        [Authorize]
        public ActionResult Index(Int64 EventId = 0, Int64 ChapterId = 0, Int64 mid = 0)
        {
            long userId = 0;
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userIdStr))
                long.TryParse(userIdStr, out userId);


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


            List<Entities.Chapters> lstChapters = new();
            List<Entities.PageDetails> lstPageDetails = new();
            List<Entities.MenuItems> lstMenuItems = new();
            Entities.UserRoles objuserroles = new();
            int status = 0;
            int _qstatus = 0;

            try
            {
                lstChapters = _Chapters.GetChaptersList(ref status);

                // ✅ FIXED — pass chapterId (Int64) not chapterValue (string)
                ViewBag.catlist = _VolunteerCategory.GetVolunteerCategoriesList(chapterId, ref _qstatus);
                ViewBag.eventlist = _Events.GetEventsListall(ref _qstatus);

                objuserroles = _users.GetRoleDetialsById(userId, mid, ref status);

                HttpContext.Session.SetString("IsEdit", objuserroles.IsEdit.ToString());
                HttpContext.Session.SetString("IsView", objuserroles.IsView.ToString());
                HttpContext.Session.SetString("IsDelete", objuserroles.IsDelete.ToString());
                HttpContext.Session.SetString("IsExport", objuserroles.IsExport.ToString());
                HttpContext.Session.SetString("IsAdd", objuserroles.IsAdd.ToString());

                ViewBag.IsEdit = objuserroles.IsEdit;
                ViewBag.IsView = objuserroles.IsView;
                ViewBag.IsDelete = objuserroles.IsDelete;
                ViewBag.IsExport = objuserroles.IsExport;
                ViewBag.IsAdd = objuserroles.IsAdd;
                ViewBag.mid = mid;
            }
            catch
            {
                status = -1;
            }

            ViewBag.ChapterId = chapterId;  // ✅ use parsed long
            ViewBag.EventId = EventId;
            ViewBag.lstChapters = lstChapters;
            ViewBag.lstPageDetails = lstPageDetails;
            ViewBag.lstMenuItems = lstMenuItems;
            ViewBag.Mid = mid;

            return View();
        }



        [Authorize]
        public ActionResult VolunteersList(Int64 VolunteerCategoryId = 0, Int64 EventId = 0, string Search = "", string SortColumn = "UpdatedDate", string SortOrder = "Desc", int PageNo = 1, int Items = 25, bool IsEdit = false, bool IsView = false, bool IsDelete = false, bool IsExport = false, bool IsAdd = false, Int64 Mid = 0, Int64 ChapterId = 0)
        {
            string Sort = (SortColumn != "" ? SortColumn + " " + SortOrder : "");
            int Total = 0;
            List<Entities.Volunteers> lstVolunteers = new List<Entities.Volunteers>();

            try
            {
                lstVolunteers = _Volunteers.GetVolunteersListByVariable(VolunteerCategoryId, EventId, Search, Sort, PageNo, Items, ChapterId, ref Total);

            }
            catch
            {
                TempData["message"] = "<div class=\"alert alert-danger alert-dismissable\">Failed transaction.</div>";
            }
            ViewBag.total = Total;
            ViewBag.pageno = PageNo;
            ViewBag.items = Items;
            ViewBag.lstVolunteers = lstVolunteers;
            ViewBag.sortcolumn = SortColumn;
            ViewBag.sortorder = SortOrder.ToLower();
            ViewBag.IsEdit = IsEdit;
            ViewBag.IsView = IsView;
            ViewBag.IsDelete = IsDelete;
            ViewBag.IsExport = IsExport;
            ViewBag.IsAdd = IsAdd;
            ViewBag.Mid = Mid;
            ViewBag.ChapterId = ChapterId;
            return View();
        }

        [Authorize]
        public ActionResult AddVolunteer(Int64 VolunteerCategoryId = 0, Int64 EventId = 0, Int64 mid = 0, Int64 CountryID = 0, Int64 ChapterId = 0)
        {
            int _qstatus = 0;
            int status = 0;
            int Total = 0;
            int status1 = 0;
            List<Entities.country> lstCountries = new List<Entities.country>();

            ChapterId = Convert.ToInt64(HttpContext.Session.GetString("chapterid"));

            //ChapterId = Convert.ToInt64(chapteridsCookie.Value);
            try
            {
                List<Entities.VolunteerCategories> lstVolunteerCategory = _VolunteerCategory.GetVolunteerCategoriesList(ChapterId, ref _qstatus);
                List<Entities.Events> lstEvents = _Events.GetEventsListall(ref status);
                lstChapters = _Chapters.GetChaptersList(ref Total);
                lstCountries = _Volunteers.GetCountriesList(ref status1);

                if (_qstatus == 1 || status == 1)
                {
                    ViewBag.lstVolunteerCategory = lstVolunteerCategory;
                    ViewBag.lstEvents = lstEvents;
                    ViewBag.lstChapters = lstChapters;
                    ViewBag.status = _qstatus;
                    ViewBag.ChapterId = ChapterId;
                    ViewBag.lstCountries = lstCountries;
                }
                else
                {
                    TempData["message"] = "<div class=\"alert alert-danger alert-dismissable\">Failed transaction.</div>";
                    return RedirectToAction("Index", "VideoGallery", new { mid = mid });
                }
            }
            catch (Exception ex)
            {
                //TempData["message"] = "<div class=\"alert alert-danger alert-dismissable\">Failed transaction.</div>";
                TempData["message"] = "<div class=\"error closable\">" + ex.Message + " +Failed Transaction</div>";
            }
            ViewBag.VolunteerCategoryId = VolunteerCategoryId;
            ViewBag.EventId = EventId;
            ViewBag.mid = mid;

            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult AddVolunteer(Entities.Volunteers objVolunteers, Int64 mid = 0)
        {
            BLL.CustomFields _customFields = new BLL.CustomFields();

            try
            {
                int status = 0;
                Entities.AppInfo objAppInfo = _appinfo.GetAppInfoDetails(ref status);
                string timezone = objAppInfo.TimeZones;
                objVolunteers.UpdatedBy = HttpContext.User.Identity.Name.ToString();
                objVolunteers.InsertedBy = HttpContext.User.Identity.Name.ToString();
                objVolunteers.UpdatedDate = AWS_AssociationToolCore.BLL.Common.GetTimeZoneAdjustedDateTime();

                objVolunteers.IsActive = true;
                Int64 _status = _Volunteers.InsertVolunteers(objVolunteers);

                if (objAppInfo.CustomFieldStatus == "Yes")
                {
                    if (_status == 1 || _status == 2)
                    {
                        var form = _customFields.GetFormByCode(AWS_AssociationToolCore.Entities.CustomFieldFormCodes.Volunteer);
                        if (form != null)
                        {
                            var fields = _customFields.GetCustomFieldsByForm(form.FormId, onlyActive: true);
                            var postedValues = new Dictionary<string, string>();

                            // Existing saved values for this record, keyed by CustomFieldId — same source
                            // the edit view's Model.Values dictionary is built from. Needed so a File
                            // field with "no new file selected" doesn't get wiped out on update.
                            Dictionary<long, string> existingValues = null;
                            if (_status == 2) // only relevant on update; nothing exists yet on insert
                            {
                                existingValues = _customFields.GetValuesForRecord(form.FormId, objVolunteers.VolunteerId);
                            }

                            foreach (var f in fields)
                            {
                                if (f.FieldType == "File")
                                {
                                    var uploadedFile = Request.Form.Files[f.FieldName];

                                    if (uploadedFile != null && uploadedFile.Length > 0)
                                    {
                                        // New file uploaded — save it and store the new filename
                                        string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(uploadedFile.FileName)}";
                                        string uploadDir = Path.Combine(objAppInfo.UploadPath, "CustomFields");
                                        string savePath = Path.Combine(uploadDir, fileName);

                                        Directory.CreateDirectory(uploadDir);

                                        using (var stream = new FileStream(savePath, FileMode.Create))
                                        {
                                            uploadedFile.CopyTo(stream);
                                        }

                                        postedValues[f.FieldName] = fileName;
                                    }
                                    else
                                    {
                                        // No new file selected — keep whatever is already saved
                                        // instead of overwriting it with an empty value.
                                        string existing = null;
                                        existingValues?.TryGetValue(f.CustomFieldId, out existing);
                                        postedValues[f.FieldName] = existing ?? string.Empty;
                                    }
                                }
                                else if (Request.Form.ContainsKey(f.FieldName))
                                {
                                    postedValues[f.FieldName] = Request.Form[f.FieldName].ToString();
                                }
                            }

                            _customFields.ValidateAndSaveValues(form.FormId, objVolunteers.VolunteerId, fields, postedValues);
                        }
                    }
                }
                // ------------------------------------------------------------------
                if (_status == 1)
                {
                    TempData["messageType"] = "success";
                    TempData["message"] = "Record Inserted Successfully";
                    return RedirectToAction("Index", "Volunteers", new { mid = mid });
                }
                if (_status == 2)
                {
                    TempData["messageType"] = "success";
                    TempData["message"] = "Changes has been Updated Successfully";
                    return RedirectToAction("Index", "Volunteers", new { VolunteerId = objVolunteers.VolunteerId, mid = mid });
                }
            }
            catch
            {
                TempData["messageType"] = "error";
                TempData["message"] = "Failed Transaction";
                return RedirectToAction("Index", "Volunteers", new { mid = mid });
            }
            return View();
        }

        [Authorize]
        public ActionResult EditVolunteer(Int64 VolunteerId = 0, Int64 VolunteerCategoryId = 0, Int64 mid = 0, Int64 CountryID = 0, Int64 ChapterId = 0)
        {
            try
            {
                int status1 = 0;
                int _qstatus = 0;
                long chapterId = 0;

     

                 ChapterId = Convert.ToInt64( HttpContext.Session.GetString("chapterid"));
                //Session["chapterid"] = chapteridsCookie.Value;

                //ChapterId = Convert.ToInt64(chapteridsCookie.Value);
                Entities.Volunteers _objVolunteers = _Volunteers.GetVolunteerInfoById(VolunteerId, ref _qstatus);
                int qstatus = 0;
                List<Entities.VolunteerCategories> lstVolunteerCategory = _VolunteerCategory.GetVolunteerCategoriesList(ChapterId, ref qstatus);
                List<Entities.country> lstCountries = new List<Entities.country>();
                int status = 0;
                int Total = 0;
                List<Entities.Events> lstEvents = _Events.GetEventsListall(ref status);
                lstChapters = _Chapters.GetChaptersList(ref Total);
                lstCountries = _Volunteers.GetCountriesList(ref status1);

                if (_qstatus == 1 && (qstatus == 1 || status == 1))
                {
                    ViewBag.objVolunteers = _objVolunteers;
                    ViewBag.VolunteerCategoryId = VolunteerCategoryId;
                    ViewBag.lstVolunteerCategory = lstVolunteerCategory;
                    ViewBag.lstEvents = lstEvents;
                    ViewBag.lstChapters = lstChapters;
                    ViewBag.lstCountries = lstCountries;
                    ViewBag.ChapterId = ChapterId;
                    ViewBag.status = _qstatus;
                    ViewBag.mid = mid;
                }
                else
                {
                    TempData["messageType"] = "warning";
                    TempData["message"] = "Failed transaction.";
                    return RedirectToAction("Index", "Volunteers", new { mid = mid });
                }
            }
            catch
            {
                TempData["messageType"] = "warning";
                TempData["message"] = "Failed transaction.";
                return RedirectToAction("Index", "Volunteers", new { mid = mid });
            }
            return View();
        }

        [Authorize]
        public ActionResult ViewVolunteer(Int64 VolunteerId = 0, Int64 mid = 0)
        {
            int _qstatus = 0;
            Entities.Volunteers _objVolunteers = new Entities.Volunteers();
            try
            {
                _objVolunteers = _Volunteers.GetVolunteerInfoById(VolunteerId, ref _qstatus);

                if (_qstatus != 1)
                {
                    TempData["messageType"] = "error";
                    TempData["message"] = "Failed transaction.";
                    return RedirectToAction("Index", "Volunteers", new { mid = mid });
                }
            }
            catch
            {
                TempData["messageType"] = "error";
                TempData["message"] = "Failed transaction.";
                return RedirectToAction("Index", "Volunteers", new { mid = mid });
            }

            ViewBag.objVolunteers = _objVolunteers;
            ViewBag.status = _qstatus;
            ViewBag.mid = mid;
            return View();
        }

        [Authorize]
        [HttpPost]
        public JsonResult DeleteVolunteer(Int64 VolunteerId)
        {
            try
            {
                Int64 _status = _Volunteers.DeleteVolunteer(VolunteerId);
                if (_status == 1)
                {
                    return Json(new { ok = true, messageType = "success", message = "Record Deleted Successfully" });
                }
                else
                {
                    return Json(new { ok = false, messageType = "warning", message = "Failed deleting page" });
                }
            }
            catch
            {
                return Json(new { ok = false, messageType = "error", message = "Failed transaction." });
            }
        }

        [HttpPost]
        [Authorize]
        public JsonResult Volunteerstatus(Int64 VolunteerId)
        {
            int status = 0;
            try
            {
                Int64 _status = _Volunteers.UpdateVolunteerstatus(VolunteerId);
                if (_status == 1)
                {
                    int status1 = 0;
                    Entities.AppInfo objAppInfo = _appinfo.GetAppInfoDetails(ref status1);
                    Entities.MailTemplates objTemplates = _MailTemplates.GetMailTemplateById("Confirm Volunteer Registration", 0, ref status);
                    if (objTemplates != null)
                    {
                        Entities.Volunteers objVolunteers = _Volunteers.GetVolunteerInfoById(VolunteerId, ref status);
                        StringBuilder body = new StringBuilder();
                        if (objVolunteers.IsActive == true)
                        {
                            body.Append(objTemplates.Description);
                            body.Replace("[usersiteurl]", objAppInfo.BaseUrl);
                            body.Replace("[FBUrl]", objAppInfo.FacebookUrl);
                            body.Replace("[TWUrl]", objAppInfo.TwitterUrl);
                            body.Replace("[YUrl]", objAppInfo.YoutubeUrl);
                            body.Replace("[GUrl]", objAppInfo.SupportEmail);
                            body.Replace("[TPhone]", objAppInfo.CompanyPhone);
                            body.Replace("[TEmail]", objAppInfo.CompanyEmail);
                            body.Replace("[SiteName]", objAppInfo.SiteName);
                            body.Replace("[USERNAME]", BLL.Common.UppercaseFirst(objVolunteers.FirstName + "" + objVolunteers.LastName));
                            //body.Replace("[EVENTNAME]", BLL.Common.UppercaseFirst(objVolunteers.VolunteerCategoryId));
                            _sendmail.SendMailSendinbrevo(objVolunteers.Email, objTemplates.Subject, body.ToString());
                        }
                    }
                    return Json(new { ok = true, messageType = "success", message = "Updated Status Successfully" });
                }
                else
                {
                    return Json(new { ok = false, messageType = "error", message = "Failed updating status" });
                }
            }
            catch
            {
                return Json(new { ok = false, messageType = "error", message = "Failed transaction." });
            }
        }

        [Authorize]
        [HttpPost]
        public JsonResult CheckVolunteerEmailAvailability(string Email)
        {
            int status = 0;
            try
            {
                Entities.Volunteers objVolunteers = _Volunteers.VolunteerValidationByEmail(Email, ref status);
                bool data = (objVolunteers.VolunteerId != 0 ? false : true);

                return Json(new { ok = true, data = data, message = "" });
            }
            catch
            {
                return Json(new { ok = false, message = "<div class=\"alert alert-danger alert-dismissable\">Failed transaction.</div>" });
            }
        }

        [Authorize]
        [HttpPost]
        public JsonResult CheckEmailAvailability(string Email, Int64 VolunteerId)
        {
            int status = 0;
            try
            {
                Entities.Volunteers objVolunteers = _Volunteers.VolunteerValidationByEmail(Email, ref status);
                bool data = (objVolunteers.VolunteerId == VolunteerId || objVolunteers.VolunteerId == 0 ? true : false);

                return Json(new { ok = true, data = data, message = "" });
            }
            catch
            {
                return Json(new { ok = false, message = "<div class=\"alert alert-danger alert-dismissable\">Failed transaction.</div>" });
            }
        }

        #endregion

        #region export to excel
        [Authorize]
        [HttpGet]
        public IActionResult GetVolunteerExportFields(Int64 ChapterId = 0)
        {
            try
            {
                DataTable dt = _DVolunteers.ExportToVolunteers("", 0, 0, "UpdatedDate DESC", ChapterId);

                var skipCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Rid" };

                var list = new List<object>();
                foreach (DataColumn col in dt.Columns)
                {
                    if (skipCols.Contains(col.ColumnName)) continue;

                    list.Add(new
                    {
                        fieldKey = col.ColumnName,
                        displayName = Regex.Replace(col.ColumnName, "(?<=[a-z])(?=[A-Z])", " "),
                        section = "Volunteer Details"
                    });
                }

                // ---- Custom Fields (File type tappa migatavi) ----
                BLL.CustomFields _customFields = new BLL.CustomFields();
                var form = _customFields.GetFormByCode(AWS_AssociationToolCore.Entities.CustomFieldFormCodes.Volunteer);
                if (form != null)
                {
                    var fields = _customFields.GetCustomFieldsByForm(form.FormId, onlyActive: true);
                    foreach (var f in fields)
                    {
                        if (f.FieldType == "File") continue;

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

        public IActionResult VolunteersExporttoExcel(
            string Search = "",
            Int64 VolunteerCategoryId = 0,
            Int64 EventId = 0,
            string SortColumn = "UpdatedDate",
            string SortOrder = "DESC",
            Int64 chapterId = 0,
            string SelectedFields = "")
        {
            try
            {
                string Sort = !string.IsNullOrEmpty(SortColumn) ? $"{SortColumn} {SortOrder}" : "";

                DataTable dtUni = _DVolunteers.ExportToVolunteers(
                    Search, VolunteerCategoryId, EventId, Sort, chapterId);

                if (dtUni == null || dtUni.Rows.Count == 0)
                {
                    TempData["messageType"] = "error";
                    TempData["message"] = "No records found to export.";
                    return RedirectToAction("Index");
                }

                // ---- Custom fields load (File type skip) ----
                BLL.CustomFields _customFields = new BLL.CustomFields();
                var form = _customFields.GetFormByCode(AWS_AssociationToolCore.Entities.CustomFieldFormCodes.Volunteer);
                var cfMap = new Dictionary<string, dynamic>(StringComparer.OrdinalIgnoreCase); // "CF_id" -> field
                if (form != null)
                {
                    foreach (var f in _customFields.GetCustomFieldsByForm(form.FormId, onlyActive: true))
                    {
                        if (f.FieldType == "File") continue;
                        cfMap["CF_" + f.CustomFieldId] = f;
                    }
                }

                var skipCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Rid" };
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

                // Custom field values: volunteer ki okasari matrame DB call (cache)
                bool needCustom = columnsToCopy.Any(k => cfMap.ContainsKey(k));
                var valuesCache = new Dictionary<long, Dictionary<long, string>>();

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Volunteers-Export");

                    // Headers
                    for (int c = 0; c < columnsToCopy.Count; c++)
                    {
                        string key = columnsToCopy[c];
                        worksheet.Cells[1, c + 1].Value = cfMap.ContainsKey(key) ? (string)cfMap[key].FieldLabel : key;
                        worksheet.Cells[1, c + 1].Style.Font.Bold = true;
                        worksheet.Cells[1, c + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[1, c + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(173, 216, 230));
                        worksheet.Cells[1, c + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    }

                    // Data rows
                    for (int row = 0; row < dtUni.Rows.Count; row++)
                    {
                        long volunteerId = 0;
                        Dictionary<long, string> cfValues = null;
                        if (needCustom)
                        {
                            long.TryParse(dtUni.Rows[row]["VolunteerId"].ToString(), out volunteerId);
                            if (!valuesCache.TryGetValue(volunteerId, out cfValues))
                            {
                                cfValues = _customFields.GetValuesForRecord(form.FormId, volunteerId)
                                           ?? new Dictionary<long, string>();
                                valuesCache[volunteerId] = cfValues;
                            }
                        }

                        for (int c = 0; c < columnsToCopy.Count; c++)
                        {
                            string key = columnsToCopy[c];

                            if (cfMap.ContainsKey(key))
                            {
                                string val = null;
                                cfValues?.TryGetValue((long)cfMap[key].CustomFieldId, out val);
                                worksheet.Cells[row + 2, c + 1].Value = val ?? "";
                            }
                            else
                            {
                                var cellValue = dtUni.Rows[row][key];
                                worksheet.Cells[row + 2, c + 1].Value = cellValue == DBNull.Value
                                    ? ""
                                    : cellValue.ToString();
                            }
                        }
                    }

                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    var content = package.GetAsByteArray();
                    string fileName = $"Volunteers-Export-{DateTime.UtcNow:dd-MM-yyyy}.xlsx";
                    string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    return File(content, contentType, fileName);
                }
            }
            catch (Exception ex)
            {
                TempData["messageType"] = "error";
                TempData["message"] = "Export failed: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
        [Authorize]
        public ActionResult GetStateByCountryID(Int64 CountryID = 0)
        {
            string str = "";
            string message = "";
            BLL.Volunteers _Volunteers = new BLL.Volunteers();

            try
            {
                int status = 0;
                List<AWS_AssociationToolCore.Entities.State> lstcountry = _Volunteers.GetStatesByCountryID(CountryID, ref status);
                if (status == 1)
                {
                    return Json(new { ok = true, data = lstcountry });
                }
                else
                {
                    message = "error";
                    str = "<div class=\"alert alert-danger alert-dismissable\">Failed transaction</div>";
                    return Json(new { ok = false, data = str });
                }
            }
            catch (Exception ex)
            {
                str = "<div class=\"alert alert-danger alert-dismissable\">" + ex.Message + "</div>";
                message = "error";
                return Json(new { ok = true, data = str });
            }
        }

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
