using AWS_AssociationToolCore.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace AWS_AssociationToolCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CustomFieldsController : Controller
    {
        BLL.CustomFields _customFields = new BLL.CustomFields();
        BLL.Users _users = new BLL.Users();

        //private string CurrentUser => Context.Session.GetString("username") ?? "System";
        private string CurrentUser => HttpContext.Session.GetString("username") ?? "System";

        [Authorize]
        public ActionResult Index(long formId = 0, Int64 mid = 0)
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
            List<Entities.Chapters> lstChapters = new();
            List<Entities.PageDetails> lstPageDetails = new();
            List<Entities.MenuItems> lstMenuItems = new();
            Entities.UserRoles objuserroles = new();

            int status = 0;
            int CategoryLevel = 0;

            try
            {

                int qStatus = 0;
                ViewBag.Forms = _customFields.GetForms();

                objuserroles = _users.GetRoleDetialsById(userId, mid, ref status);

                // ✅ Session (Core way)
                HttpContext.Session.SetString("IsEdit", objuserroles.IsEdit.ToString());
                HttpContext.Session.SetString("IsView", objuserroles.IsView.ToString());
                HttpContext.Session.SetString("IsDelete", objuserroles.IsDelete.ToString());
                HttpContext.Session.SetString("IsExport", objuserroles.IsExport.ToString());
                HttpContext.Session.SetString("IsAdd", objuserroles.IsAdd.ToString());

                // ✅ ViewBag (optional)
                ViewBag.IsEdit = objuserroles.IsEdit;
                ViewBag.IsView = objuserroles.IsView;
                ViewBag.IsDelete = objuserroles.IsDelete;
                ViewBag.IsExport = objuserroles.IsExport;
                ViewBag.IsAdd = objuserroles.IsAdd;
                ViewBag.mid = mid;

            }
            catch (Exception ex)
            {
                TempData["messageType"] = "error";
                TempData["message"] = "Failed to load custom fields. " + ex.Message;
            }

            ViewBag.ChapterId = chapterId;


            ViewBag.lstPageDetails = lstPageDetails;
            ViewBag.lstMenuItems = lstMenuItems;
            ViewBag.Mid = mid;
            ViewBag.SelectedFormId = formId;

            return View();
        }

        [Authorize]
        public ActionResult List(long formId = 0, string Search = "", string SortColumn = "DisplayOrder", string SortOrder = "Asc",
            int PageNo = 1, int Items = 20, bool IsEdit = false, bool IsView = false, bool IsDelete = false, bool IsAdd = false, Int64 Mid = 0)
        {
            string Sort = (SortColumn != "" ? SortColumn + " " + SortOrder : "");
            int Total = 0;
            List<Entities.CustomFields> lstCustomFields = new List<Entities.CustomFields>();

            try
            {
                // Requires a paged BLL method, analogous to BLL.Volunteers.GetVolunteersListByVariable:
                //   List<Entities.CustomFields> GetCustomFieldsListByVariable(long formId, string search, string sort, int pageNo, int items, ref int total)
                lstCustomFields = _customFields.GetCustomFieldsListByVariable(formId, Search, Sort, PageNo, Items, ref Total);
            }
            catch (Exception ex)
            {
                TempData["message"] = "<div class=\"alert alert-danger alert-dismissable\">" + ex.Message + "</div>";
            }

            ViewBag.total = Total;
            ViewBag.pageno = PageNo;
            ViewBag.items = Items;
            ViewBag.lstCustomFields = lstCustomFields;
            ViewBag.sortcolumn = SortColumn;
            ViewBag.sortorder = SortOrder.ToLower();
            ViewBag.IsEdit = IsEdit;
            ViewBag.IsView = IsView;
            ViewBag.IsDelete = IsDelete;
            ViewBag.IsAdd = IsAdd;
            ViewBag.Mid = Mid;
            return View();
        }

        // GET: /Admin/CustomFields/Create
        [Authorize]
        public ActionResult Create()
        {
            ViewBag.Forms = _customFields.GetForms();
            ViewBag.FieldTypes = FieldTypeList();
            return View(new Entities.CustomFields { IsActive = true, DisplayOrder = 0 });
        }

        // POST: /Admin/CustomFields/Create
        [Authorize]
        [HttpPost]
        public ActionResult Create(Entities.CustomFields model)
        {
            var result = _customFields.SaveCustomField(model, CurrentUser);
            TempData["messageType"] = result.Success ? "success" : "error";
            TempData["message"] = result.Message;

            if (result.Success)
                return RedirectToAction("Index", new { formId = model.FormId });

            ViewBag.Forms = _customFields.GetForms();
            ViewBag.FieldTypes = FieldTypeList();
            return View(model);
        }

        // GET: /Admin/CustomFields/Edit/5
        public ActionResult Edit(long id)
        {
            var field = _customFields.GetCustomFieldById(id);
            if (field == null)
            {
                TempData["messageType"] = "error";
                TempData["message"] = "Custom field not found.";
                return RedirectToAction("Index");
            }
            ViewBag.Forms = _customFields.GetForms();
            ViewBag.FieldTypes = FieldTypeList();
            return View(field);
        }

        // POST: /Admin/CustomFields/Edit
        [Authorize]
        [HttpPost]
        public ActionResult Edit(Entities.CustomFields model)
        {
            var result = _customFields.SaveCustomField(model, CurrentUser);
            TempData["messageType"] = result.Success ? "success" : "error";
            TempData["message"] = result.Message;

            if (result.Success)
                return RedirectToAction("Index", new { formId = model.FormId });

            ViewBag.Forms = _customFields.GetForms();
            ViewBag.FieldTypes = FieldTypeList();
            return View(model);
        }

        // POST: /Admin/CustomFields/Delete  (ajax)
        [HttpPost]
        //public JsonResult Delete(long id)
        //{
        //    var result = _customFields.DeleteCustomField(id);
        //    return Json(new { ok = result.Success, message = result.Message });
        //}

        [Authorize]
        [HttpPost]
        public JsonResult Delete(long id)
        {
            var result = _customFields.DeleteCustomField(id);
            return Json(new { messageType = result.Success ? "success" : "error", message = result.Message });
        }

        // POST: /Admin/CustomFields/UpdateStatus (ajax - Activate/Deactivate toggle)
        //[HttpPost]
        //public JsonResult UpdateStatus(long id, bool isActive)
        //{
        //    var result = _customFields.UpdateStatus(id, isActive, CurrentUser);
        //    return Json(new { ok = result.Success, message = result.Message });
        //}




        // POST: /Admin/CustomFields/UpdateDisplayOrder (ajax)

        [Authorize]
        [HttpPost]
        public JsonResult UpdateStatus(long id, bool isActive)
        {
            var result = _customFields.UpdateStatus(id, isActive, CurrentUser);
            return Json(new { messageType = result.Success ? "success" : "error", message = result.Message });
        }



        [Authorize]
        [HttpPost]
        public JsonResult UpdateDisplayOrder(long id, int displayOrder)
        {
            var result = _customFields.UpdateDisplayOrder(id, displayOrder);
            return Json(new { ok = result.Success, message = result.Message });
        }

        /* ---------------- Options ---------------- */

        // GET: /Admin/CustomFields/ManageOptions/5
        public ActionResult ManageOptions(long id)
        {
            var field = _customFields.GetCustomFieldById(id);
            if (field == null)
            {
                TempData["messageType"] = "error";
                TempData["message"] = "Custom field not found.";
                return RedirectToAction("Index");
            }
            return View(field);
        }

        // POST: /Admin/CustomFields/SaveOptions (ajax)
        [HttpPost]
        //public JsonResult SaveOptions(CustomFieldOptions model)
        //{
        //    var result = _customFields.SaveOption(model, CurrentUser);
        //    var options = _customFields.GetOptionsByFieldId(model.CustomFieldId, onlyActive: false);
        //    return Json(new { ok = result.Success, message = result.Message, options });
        //}

        // POST: /Admin/CustomFields/SaveOptions (ajax)
        [Authorize]
        [HttpPost]
        public JsonResult SaveOptions(CustomFieldOptions model)
        {
            var result = _customFields.SaveOption(model, CurrentUser);
            var options = _customFields.GetOptionsByFieldId(model.CustomFieldId, onlyActive: false)
                .OrderBy(o => o.DisplayOrder)
                .Select(o => new
                {
                    optionId = o.OptionId,
                    optionText = o.OptionText,
                    optionValue = o.OptionValue,
                    displayOrder = o.DisplayOrder,
                    isActive = o.IsActive
                });
            return Json(new { ok = result.Success, message = result.Message, options });
        }




        //// POST: /Admin/CustomFields/DeleteOption (ajax)
        //[HttpPost]
        //public JsonResult DeleteOption(long optionId, long customFieldId)
        //{
        //    var result = _customFields.DeleteOption(optionId);
        //    var options = _customFields.GetOptionsByFieldId(customFieldId, onlyActive: false);
        //    return Json(new { ok = result.Success, message = result.Message, options });
        //}


        // POST: /Admin/CustomFields/DeleteOption (ajax)
        [Authorize]
        [HttpPost]
        public JsonResult DeleteOption(long optionId, long customFieldId)
        {
            var result = _customFields.DeleteOption(optionId);
            var options = _customFields.GetOptionsByFieldId(customFieldId, onlyActive: false)
                .OrderBy(o => o.DisplayOrder)
                .Select(o => new
                {
                    optionId = o.OptionId,
                    optionText = o.OptionText,
                    optionValue = o.OptionValue,
                    displayOrder = o.DisplayOrder,
                    isActive = o.IsActive
                });
            return Json(new { ok = result.Success, message = result.Message, options });
        }




        // POST: /Admin/CustomFields/UpdateOptionStatus (ajax)
        [Authorize]
        [HttpPost]
        public JsonResult UpdateOptionStatus(long optionId, bool isActive)
        {
            var result = _customFields.UpdateOptionStatus(optionId, isActive);
            return Json(new { ok = result.Success, message = result.Message });
        }

        // POST: /Admin/CustomFields/UpdateOptionOrder (ajax)
        [Authorize]
        [HttpPost]
        public JsonResult UpdateOptionOrder(long optionId, int displayOrder)
        {
            var result = _customFields.UpdateOptionOrder(optionId, displayOrder);
            return Json(new { ok = result.Success, message = result.Message });
        }

        /* ---------------- Conditions ---------------- */

        // GET: /Admin/CustomFields/ManageConditions/5   (formId)
        public ActionResult ManageConditions(long formId)
        {
            ViewBag.Form = _customFields.GetForms().FirstOrDefault(f => f.FormId == formId);
            ViewBag.FieldsInForm = _customFields.GetCustomFieldsByForm(formId, onlyActive: false);
            ViewBag.Operators = new[] { "Equals", "NotEquals", "Contains", "NotContains", "GreaterThan", "LessThan", "IsEmpty", "IsNotEmpty" };
            ViewBag.Actions = new[] { "Show", "Hide", "Enable", "Disable", "Require", "Optional", "UpdateValue" };
            var conditions = _customFields.GetConditionsByForm(formId, onlyActive: false);
            return View(conditions);
        }

        // POST: /Admin/CustomFields/SaveCondition (ajax)


        [Authorize]
        [HttpPost]
        public JsonResult SaveCondition(CustomFieldConditions model)
        {
            var result = _customFields.SaveCondition(model, CurrentUser);
            var conditions = _customFields.GetConditionsByForm(model.FormId, onlyActive: false);
            return Json(new
            {
                ok = result.Success,
                messageType = result.Success ? "success" : "error",
                message = result.Message,
                conditions
            });
        }

        [HttpPost]
        public JsonResult DeleteCondition(long conditionId, long formId)
        {
            var result = _customFields.DeleteCondition(conditionId);
            var conditions = _customFields.GetConditionsByForm(formId, onlyActive: false);
            return Json(new
            {
                ok = result.Success,
                messageType = result.Success ? "success" : "error",
                message = result.Message,
                conditions
            });
        }














        //[HttpPost]
        //public JsonResult SaveCondition(CustomFieldConditions model)
        //{
        //    var result = _customFields.SaveCondition(model, CurrentUser);
        //    var conditions = _customFields.GetConditionsByForm(model.FormId, onlyActive: false);
        //    return Json(new { ok = result.Success, message = result.Message, conditions });
        //}

        //// POST: /Admin/CustomFields/DeleteCondition (ajax)
        //[HttpPost]
        //public JsonResult DeleteCondition(long conditionId, long formId)
        //{
        //    var result = _customFields.DeleteCondition(conditionId);
        //    var conditions = _customFields.GetConditionsByForm(formId, onlyActive: false);
        //    return Json(new { ok = result.Success, message = result.Message, conditions });
        //}

        /* ---------------- User-side / any-form dynamic loading ---------------- */

        /// <summary>
        /// Called via AJAX (or server-side from another controller/view) to fetch the
        /// full render package for a form: fields (with options) + conditions + any
        /// existing values for recordId. Use this from the User-side forms, e.g.:
        ///   $.post('/Admin/CustomFields/GetFieldsByForm', { formCode: 'VOLUNTEER', recordId: 0 }, ...)
        /// Kept on this controller (rather than duplicated per user-side controller) so
        /// admin and user side always read the same field/condition definitions.
        /// </summary>
        [HttpPost]
        [HttpGet]
        public JsonResult GetFieldsByForm(string formCode, long recordId = 0)
        {
            var model = _customFields.GetDynamicForm(formCode, recordId);
            return Json(new
            {
                ok = true,
                formId = model.FormId,
                fields = model.Fields,
                conditions = model.Conditions,
                values = model.Values.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value)
            });
        }
        [Authorize]
        private List<SelectListItem> FieldTypeList()
        {
            string[] types =
            {
                "Textbox", "Textarea", "Dropdown", "Radio", "Checkbox", "Date",
                "Time", "Number", "Email", "Phone", "File"
            };
            return types.Select(t => new SelectListItem { Text = t, Value = t }).ToList();
        }
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
