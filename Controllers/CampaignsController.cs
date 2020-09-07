using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Webtracking.Database;
using Webtracking.Models;

namespace Webtracking.Controllers
{
    public class CampaignsController : Controller
    {
        //TODO: manage the "Edit" a button of a campaign in the controler and the campaign management view
        //TODO: Create the view "details" to consult the statistics of a campaign (the view exist but not implemented).

        private readonly ILogger<CampaignsController> _logger;

        public CampaignsController(ILogger<CampaignsController> logger)
        {
            _logger = logger;
        }

        public IActionResult Edit(string id)
        {
            return View(new Database.Campaign(new Guid(id)));
        }

        public IActionResult Delete(string id)
        {
            Campaign oCamp = new Campaign(new Guid(id));
            oCamp.Remove();
            return RedirectToAction("Index"); 
        }

        [HttpPost]
        public IActionResult Edit(Database.Campaign oCampaign)
        {
            return View(oCampaign);
        }

        public IActionResult Details(string id)
        {
            return View(new Database.Campaign(new Guid(id)));
        }

        public IActionResult Index()
        {
            return View(Database.Campaign.GetListWStats());
        }

        [HttpGet]
        public IActionResult Tracker(string? id)
        {
            return View(new Database.Campaign(new Guid(id)));
        }

        [HttpPost]
        public IActionResult Tracker(string? id, Database.Campaign model)
        {
            if (!String.IsNullOrEmpty(model.OriginalBat))
            {
                model.TrackedBat = Campaign.TrackContent(model.OriginalBat, model.Domain, model.DynamicField, true, model.Name, model._id);
                model.Save();
            }
            return RedirectToAction("Tracker", new { id = model._id });
        }

        public IActionResult Create(Database.Campaign newCampaign)
        {
            if (string.IsNullOrEmpty(newCampaign.Name))
            {
                newCampaign.Domain = this.Request.HttpContext.Request.Host.ToString();
                return View(newCampaign);
            }
            else
            {
                if (newCampaign.Save())
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
