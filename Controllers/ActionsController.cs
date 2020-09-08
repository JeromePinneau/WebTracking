using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Webtracking.Controllers
{
    public class ActionsController : Controller
    {
        private readonly ILogger<CampaignsController> _logger;

        public ActionsController(ILogger<CampaignsController> logger)
        {
            _logger = logger;
        }

        // GET: ActionsController
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult o(int id, string key)
        {
            try
            {
                return Redirect(Database.Campaign.GetTrackedLink(id, key));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                throw ex;
            }
        }

        public ActionResult op(string id, string key)
        {
            try 
            { 
                Database.Campaign.LogOpener(key, id);
                Database.Campaign oCampaign = new Database.Campaign(new Guid(id));
                return Redirect(string.Format("https://{0}/ii.png", oCampaign.Domain));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                throw ex;
            }
        }

        public ActionResult Unsubscribe(string id, string key)
        {
            try
            {
                Database.Campaign.LogUnsubscribe(key, id);
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw ex;
            }
            
        }
    }
}
