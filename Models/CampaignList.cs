using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Webtracking.Models
{
    public class CampaignList
    {
        public string _id { get; set; }
        public string Name { get; set; }
        public int Openers { get; set; }
        public float OpenersRate { get; set; }
        public int Clickers { get; set; }
        public float ClickersRate { get; set; }
        public int Unsubscriptions { get; set; }
        public float UnsubscriptionsRate { get; set; }
        public int EmailSent { get; set; }
    }
}
