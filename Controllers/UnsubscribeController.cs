using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Webtracking.Controllers
{
    public class UnsubscribeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
