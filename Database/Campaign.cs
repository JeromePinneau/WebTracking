using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;

namespace Webtracking.Database
{

    public class Campaign
    {
        public string _id;

        public DateTime CreationDate { get; set; }
        public DateTime UpdatedDate { get; set; }

        [StringLength(45)]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Name requisites")]
        [Display(Name = "Campaign name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Number of sent emails requisites")]
        [Display(Name = "Emails sent")]
        public int EmailSent { get; set; }

        [StringLength(255)]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Domain name requisites to track email")]
        [Display(Name = "Domain name")]
        public string Domain { get; set; }

        [StringLength(255)]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Dynamic field is requisites")]
        [Display(Name = "Dynamic field for email")]
        public string DynamicField { get; set; }

        public static List<Campaign> GetAll()
        {
            MySqlCommand oCom = new MySqlCommand("SELECT * FROM campaign ORDER BY CreationDate DESC", DataBase.Connection, null);
            MySqlDataReader oReader = oCom.ExecuteReader();
            List<Campaign> oList = new List<Campaign>();
            while (oReader.Read())
            {
                oList.Add(new Campaign() { CreationDate = Convert.ToDateTime(oReader["CreationDate"]), UpdatedDate = Convert.ToDateTime(oReader["UpdatedDate"])
                , Domain = oReader["Domain"].ToString(), DynamicField = oReader["DynamicField"].ToString(), EmailSent = Convert.ToInt32(oReader["EmailSent"]), Name = oReader["Name"].ToString()});
            }
            oReader.Close();
            return oList;
        }

        public static List<Models.CampaignList> GetListWStats()
        {
            MySqlCommand oCom = new MySqlCommand("SELECT * FROM campaign ORDER BY CreationDate DESC limit 25", DataBase.Connection, null);
            MySqlDataReader oReader = oCom.ExecuteReader();
            List<Models.CampaignList> oList = new List<Models.CampaignList>();
            while (oReader.Read())
            {
                Models.CampaignList newInstance = new Models.CampaignList(){ _id = oReader["_id"].ToString(), Name = oReader["Name"].ToString()};
                string subRequest = string.Format("SELECT c._id,c.EmailSent, COUNT(z.IsOpener) as Openers, COUNT(z2.IsClicker) as Clickers, COUNT(z2.IsUnsubscribe) as Unsubscribe  FROM campaign c left outer join z{0} z on z.IdCampaign = c._id and z.IsOpener = 1 left outer join z{0} z1 on z1.IdCampaign = c._id and z1.IsClicker = 1 left outer join z{0} z2 on z2.IdCampaign = c._id and z2.IsUnsubscribe = 1 WHERE c._id = @_id", oReader["Name"].ToString());
                MySqlCommand oCom2 = new MySqlCommand(subRequest, Database.DataBase.subConnection,null);
                oCom2.Parameters.Add("_id", MySqlDbType.VarChar);
                oCom2.Parameters["_id"].Value = oReader["_id"].ToString();
                MySqlDataReader oReader2 = oCom2.ExecuteReader(System.Data.CommandBehavior.SingleRow);
                if (oReader2.Read())
                {
                    newInstance.Openers = Convert.ToInt32(oReader2["Openers"]);
                    newInstance.OpenersRate = (Convert.ToInt32(oReader["EmailSent"])>0)?(Convert.ToInt32(oReader2["Openers"]) / Convert.ToInt32(oReader["EmailSent"])) * 100:0;
                    newInstance.Clickers = Convert.ToInt32(oReader2["Clickers"]);
                    newInstance.OpenersRate = (Convert.ToInt32(oReader2["Openers"])>0)?(Convert.ToInt32(oReader2["Clickers"]) / Convert.ToInt32(oReader2["Openers"])) * 100:0;
                    newInstance.Unsubscriptions = Convert.ToInt32(oReader2["Unsubscribe"]);
                    newInstance.UnsubscriptionsRate = (Convert.ToInt32(oReader2["Clickers"])>0)?(Convert.ToInt32(oReader2["Unsubscribe"]) / Convert.ToInt32(oReader2["Clickers"])) * 100:0;
                    newInstance.EmailSent = Convert.ToInt32(oReader["EmailSent"]);
                }
                else
                {
                    newInstance.Openers = 0;
                    newInstance.Clickers = 0;
                    newInstance.Unsubscriptions = 0;
                    newInstance.EmailSent = Convert.ToInt32(oReader["EmailSent"]);
                }
                oReader2.Close();
                oList.Add(newInstance);
            }
            Database.DataBase.subConnection.Close();
            oReader.Close();
            return oList;
        }

        public bool Save()
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(Name, "^[0-9A-Za-z ]+$"))
                throw new Exception("Campaign's name is not valid!");
            bool insertMode = false;
            bool success = false;
            MySqlCommand oCom = new MySqlCommand(String.Empty,DataBase.Connection, null);
            try
            {
                if (string.IsNullOrEmpty(_id))
                {
                    insertMode = true;
                    _id = Guid.NewGuid().ToString();
                }
                oCom.Parameters.Add("_id", MySqlDbType.String);
                oCom.Parameters.Add("Name", MySqlDbType.String);
                oCom.Parameters.Add("EmailSent", MySqlDbType.Int64);
                oCom.Parameters.Add("Domain", MySqlDbType.String);
                oCom.Parameters.Add("DynamicField", MySqlDbType.String);
                oCom.Parameters["_id"].Value = _id;
                oCom.Parameters["Name"].Value = Name;
                oCom.Parameters["EmailSent"].Value = EmailSent;
                oCom.Parameters["Domain"].Value = Domain;
                oCom.Parameters["DynamicField"].Value = DynamicField;
                if (insertMode)
                {
                    oCom.Parameters.Add("CreationDate", MySqlDbType.DateTime);
                    oCom.Parameters["CreationDate"].Value = DateTime.Now;

                    oCom.CommandText = "INSERT INTO campaign (_id,Name,EmailSent,Domain,DynamicField,CreationDate,UpdatedDate) VALUES (@_id,@Name, @EmailSent, @Domain, @DynamicField, @CreationDate, @CreationDate);";
                    oCom.ExecuteNonQuery();
                    try {
                        oCom.CommandText = string.Format("CREATE TABLE `z{0}` ( `Receipient` VARCHAR(300) NOT NULL, `IdCampaign` VARCHAR(300) NOT NULL, `IsOpener` BIT NULL DEFAULT 0, `IsClicker` BIT NULL DEFAULT 0, `IsHardBounce` BIT NULL DEFAULT 0, `IsUnsubscribe` BIT NULL DEFAULT 0, `FirstOpenerDate` DATETIME NULL, `FirstClickerDate` DATETIME NULL, `FirstUnsubscriptionDate` DATETIME NULL, PRIMARY KEY(`Receipient`, IdCampaign));", Name);
                        oCom.ExecuteNonQuery();
                    }
                    catch
                    {
                        //TODO : Manage rollback previous actions
                    }
                    try
                    {
                        oCom.CommandText = string.Format("CREATE TABLE `z{0}link` (`Receipient` VARCHAR(300) NOT NULL, `IdCampaign` VARCHAR(300) NOT NULL, `IdLink` INT NOT NULL,`NbClick` INT NULL DEFAULT 0,`FirstClickDate` BIT NULL DEFAULT 0, PRIMARY KEY(`Receipient`, `IdLink`)); ", Name);
                        oCom.ExecuteNonQuery();
                    }
                    catch
                    {
                        //TODO : Manage rollback previous actions
                    }
                }
                else
                {
                    oCom.Parameters.Add("UpdatedDate", MySqlDbType.DateTime);
                    oCom.Parameters["UpdatedDate"].Value = DateTime.Now;

                    oCom.CommandText = "UPDATE campaign SET Name=@Name,EmailSent=@EmailSent, UpdatedDate=@UpdatedDate WHERE _id=@_id;";
                    oCom.ExecuteNonQuery();
                }
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
            }
            finally
            {
                oCom.Dispose();
                oCom = null;
            }
            return success;
        }

        /// <summary>
        /// Creation of new campaign
        /// </summary>
        /// <param name="name">Campaign's name</param>
        /// <param name="emailSent">Number of email sent on this campaign</param>
        /// <returns></returns>
        public static Campaign Create(string name, int emailSent)
        {
            Campaign oCamp = new Campaign() { Name = name, EmailSent = emailSent };
            oCamp.Save();
            return oCamp;
        }

        public static string TrackContent(string body, string domain, string dynamicEmailField, bool saveBat, string campaign)
        {
            string newcontent = body;
            // Save Original body
            if (saveBat)
            {
                StreamWriter oWriter3 = new StreamWriter(System.IO.Path.Combine(Middleware.GlobalSetting.GetSettings().MapathBat, string.Format("{0}.html", campaign)), false, System.Text.Encoding.UTF8);
                oWriter3.Write(body);
                oWriter3.Close();
                oWriter3.Dispose();
            }
            // Track content
            Regex oreg = new Regex(@"href\s*=\s*(?:""(?<1>[^""]*)""|(?<1>\S+))");
            MatchCollection Result = oreg.Matches(body);
            if (Result.Count > 0) // no need if no link
            {
                // track links
                System.Collections.Hashtable oNewLink = new System.Collections.Hashtable();
                foreach (Match oMatsh in Result)
                {

                    string ext = oMatsh.Value.Substring(oMatsh.Value.Length - 4, 4).ToLower();
                    if ((ext != ".jpg") && (ext != ".gif") && (ext != ".jepg") && (ext != ".png") && (oMatsh.Value.IndexOf("mailto") < 0) && (oMatsh.Value.IndexOf("file") < 0))
                    {
                        string oUrlTempo = Removehref(oMatsh.Value);
                        string newlink = string.Format("http://{0}/{1}/{2}/{3}/", domain, Guid.NewGuid().ToString("d").Substring(1, Middleware.GlobalSetting.GetSettings().CharsOnelink), dynamicEmailField, TrackLink(campaign, oMatsh.Value).ToString());
                        newcontent.Replace(oUrlTempo, newlink);
                        oNewLink.Add(oUrlTempo, newlink);
                    }
                }
            }
            return newcontent;
        }

        private static int TrackLink(string campaign, string finalLink)
        {
            int oReturn = 0;
            MySqlCommand oCom = new MySqlCommand(String.Empty, DataBase.Connection, null);
            try
            {
                oCom.Parameters.Add("campaign", MySqlDbType.String);
                oCom.Parameters.Add("oldLink", MySqlDbType.String);
                oCom.Parameters["campaign"].Value = campaign;
                oCom.Parameters["oldLink"].Value = finalLink;
                oCom.CommandText = "INSERT INTO link (campaign,Link) VALUES (@campaign,@oldLink);SELECT LAST_INSERT_ID();";
                oReturn = Convert.ToInt32(oCom.ExecuteScalar());
            }
            catch (Exception ex)
            {

            }
            finally
            {
                oCom.Dispose();
                oCom = null;
            }
            return oReturn;
        }

        protected static string Removehref(string oString)
        {
            try
            {
                int oIndex = oString.IndexOf("http");
                if (oString.Substring(oString.Length - 1, 1) == "\"")
                    return oString.Substring(oIndex, oString.Length - (oIndex) - 1);
                else
                    return oString.Substring(oIndex, oString.Length - (oIndex));
            }
            catch { return oString; }
        }

    }
}
