using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Mvc;

namespace AWS_AssociationToolCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : Controller
    {
      
        public ActionResult Index()
        {
            return View();
        }

    }
}
