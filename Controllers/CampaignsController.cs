using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Webtracking.Models;

namespace Webtracking.Controllers
{
    public class CampaignsController : Controller
    {
        private readonly ILogger<CampaignsController> _logger;

        public CampaignsController(ILogger<CampaignsController> logger)
        {
            _logger = logger;
        }

        public IActionResult Edit()
        {
            return View(new Database.Campaign());
        }

        public IActionResult Details()
        {
            return View(new Database.Campaign());
        }

        public IActionResult Index()
        {
            return View(Database.Campaign.GetListWStats());
        }

        /*
        public IActionResult Create()
        {
            return View(new Database.Campaign());
        }*/

        public IActionResult Create(Database.Campaign newCampaign)
        {
            if(string.IsNullOrEmpty(newCampaign.Name))
                return View(newCampaign);
            else
            {
                if(newCampaign.Save())
                    return RedirectToAction("Index");
                else
                    return View(newCampaign);
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
