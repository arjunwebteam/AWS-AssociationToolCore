using Microsoft.AspNetCore.Mvc;
using AWS_AssociationToolCore.Entities;

namespace AWS_AssociationToolCore.ViewComponents
{
    /// <summary>
    /// Drop this into any existing Razor form (User or Admin side) to render the
    /// custom fields configured for a form, with existing values pre-populated
    /// when editing a record.
    ///
    /// Usage in an existing view, e.g. AddVolunteer.cshtml / EditVolunteer.cshtml:
    ///   @await Component.InvokeAsync("DynamicCustomFields", new { formCode = "VOLUNTEER", recordId = Model.VolunteerId, theme = "userFrontEnd" })
    ///
    /// `theme` selects which view renders the fields, so the same BLL/DAL/data
    /// never has to be duplicated just because two pages use different CSS:
    ///   "admin"        -> Default.cshtml        (Bootstrap form-control / form-select — matches EditVolunteer.cshtml)
    ///   "userFrontEnd" -> UserFrontEnd.cshtml    (fieldset/legend/input-box/cus-checkbox — matches the public AddVolunteer.cshtml)
    ///
    /// Then include ~/wwwroot/js/dynamic-custom-fields.js on the page (once) and,
    /// on submit, the existing controller action should call:
    ///   BLL.CustomFields.ValidateAndSaveValues(formId, recordId, fields, Request.Form-as-dictionary)
    /// (see README_Integration.md for the exact snippet).
    /// </summary>
    public class DynamicCustomFieldsViewComponent : ViewComponent
    {
        private readonly BLL.CustomFields _customFields = new BLL.CustomFields();

        public IViewComponentResult Invoke(string formCode, long recordId = 0, string theme = "admin")
        {
            DynamicFormViewModel model = _customFields.GetDynamicForm(formCode, recordId);
            string viewName = theme == "userFrontEnd" ? "UserFrontEnd" : "Default";
            return View(viewName, model);
        }
    }
}
