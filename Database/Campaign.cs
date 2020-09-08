using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;
using Org.BouncyCastle.Asn1.Crmf;
using System.Data;
using Microsoft.Extensions.Logging;

namespace Webtracking.Database
{
    public class Campaign
    {
        public string _id { get; set; }

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
        [Display(Name = "Email tag")]
        public string DynamicField { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Original BAT")]
        public string OriginalBat { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Tracked BAT")]
        public string TrackedBat { get; set; }

        /// <summary>
        /// Default constructor to create new campaign
        /// </summary>
        public Campaign(){}

        /// <summary>
        /// Get instance from database by Id
        /// </summary>
        /// <param name="oid">Guid of the campaign</param>
        public Campaign(Guid oid)
        {
            MySqlDataReader oReader = null;
            try
            {
                MySqlCommand oCom = new MySqlCommand(String.Empty, DataBase.Connection, null);
                oCom.Parameters.Add("_id", MySqlDbType.String);
                oCom.Parameters["_id"].Value = oid.ToString();
                oCom.CommandText = "SELECT * FROM campaign WHERE _id = @_id;";                
                oReader = oCom.ExecuteReader(CommandBehavior.SingleRow);
                if (oReader.Read())
                {
                    this._id = oReader["_id"].ToString();
                    this.Name = oReader["Name"].ToString();
                    this.EmailSent = Convert.ToInt32(oReader["EmailSent"]);
                    this.Domain = oReader["Domain"].ToString();
                    this.DynamicField = oReader["DynamicField"].ToString();
                    this.CreationDate = Convert.ToDateTime(oReader["CreationDate"]);
                    this.UpdatedDate = Convert.ToDateTime(oReader["UpdatedDate"]);
                    this.OriginalBat = oReader["OriginalBat"].ToString();
                    this.TrackedBat = oReader["TrackedBat"].ToString();
                }
                oReader.Close();
            }
            catch (Exception ex)
            {
                if (oReader != null)
                    oReader.Close();
                throw ex;
            }
            finally { 
            
            }
        }

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
                string subRequest = string.Format("SELECT c._id,c.EmailSent, SUM(z.IsOpener) as Openers, SUM(z.IsClicker) as Clickers, SUM(z.IsUnsubscribe) as Unsubscribe  FROM campaign c left outer join z{0} z on z.IdCampaign = c._id and z.IsOpener = 1 WHERE c._id = @_id GROUP BY c._id,c.EmailSent;", oReader["Name"].ToString());
                MySqlCommand oCom2 = new MySqlCommand(subRequest, Database.DataBase.Connection,null);
                oCom2.Parameters.Add("_id", MySqlDbType.VarChar);
                oCom2.Parameters["_id"].Value = oReader["_id"].ToString();
                MySqlDataReader oReader2 = oCom2.ExecuteReader(System.Data.CommandBehavior.SingleRow);
                if (oReader2.Read())
                {
                    int NbSent = Convert.ToInt32(oReader["EmailSent"]);
                    newInstance.Openers = (oReader2["Openers"] == System.DBNull.Value)?0:Convert.ToInt32(oReader2["Openers"]);
                    newInstance.OpenersRate = (NbSent > 0)?(((float)newInstance.Openers / (float)NbSent) * 100):300;
                    newInstance.Clickers = (oReader2["Clickers"] == System.DBNull.Value) ? 0 : Convert.ToInt32(oReader2["Clickers"]);
                    newInstance.ClickersRate = (newInstance.Openers > 0)?(((float)newInstance.Clickers / (float)newInstance.Openers) * 100):0;
                    newInstance.Unsubscriptions = (oReader2["Unsubscribe"] == System.DBNull.Value) ? 0 : Convert.ToInt32(oReader2["Unsubscribe"]);
                    newInstance.UnsubscriptionsRate = (newInstance.Clickers > 0)?(((float)newInstance.Unsubscriptions / (float)newInstance.Clickers) * 100):0;
                    newInstance.EmailSent = NbSent;
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
            oReader.Close();
            return oList;
        }

        /// <summary>
        /// Object serialisation in the database (Update or Insert if _id is null)
        /// </summary>
        /// <returns>true if sucess</returns>
        public bool Save()
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(Name, "^[0-9A-Za-z ]+$"))
                throw new Exception("Campaign's name is not valid!");

            //TODO : Do a check if the "name" is available for this _id (because the campaign's Name need to be unique, there is already an unique index on it in the database.

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
                oCom.Parameters.Add("OriginalBat", MySqlDbType.LongText);
                oCom.Parameters.Add("TrackedBat", MySqlDbType.LongText);
                oCom.Parameters.Add("UpdatedDate", MySqlDbType.DateTime);
                oCom.Parameters["_id"].Value = _id;
                oCom.Parameters["Name"].Value = Name;
                oCom.Parameters["EmailSent"].Value = EmailSent;
                oCom.Parameters["Domain"].Value = Domain;
                oCom.Parameters["DynamicField"].Value = DynamicField;
                oCom.Parameters["OriginalBat"].Value = OriginalBat;
                oCom.Parameters["TrackedBat"].Value = TrackedBat;
                if (insertMode)
                {
                    oCom.Parameters.Add("CreationDate", MySqlDbType.DateTime);
                    oCom.Parameters["CreationDate"].Value = DateTime.Now;

                    oCom.CommandText = "INSERT INTO campaign (_id,Name,EmailSent,Domain,DynamicField,CreationDate,UpdatedDate, OriginalBat, TrackedBat) VALUES (@_id,@Name, @EmailSent, @Domain, @DynamicField, @CreationDate, @CreationDate, @OriginalBat, @TrackedBat);";
                    oCom.ExecuteNonQuery();
                    try {
                        oCom.CommandText = string.Format("CREATE TABLE `z{0}` ( `Receipient` VARCHAR(300) NOT NULL, `IdCampaign` VARCHAR(50) NOT NULL, `IsOpener` BIT NULL DEFAULT 0, `IsClicker` BIT NULL DEFAULT 0, `IsHardBounce` BIT NULL DEFAULT 0, `IsUnsubscribe` BIT NULL DEFAULT 0, `FirstOpenerDate` DATETIME NULL, `FirstClickerDate` DATETIME NULL, `FirstUnsubscriptionDate` DATETIME NULL, PRIMARY KEY(`Receipient`, IdCampaign));", Name);
                        oCom.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        //TODO : Manage rollback previous actions (remove campaign)
                        throw ex;
                    }
                    try
                    {
                        oCom.CommandText = string.Format("CREATE TABLE `z{0}link` (`Receipient` VARCHAR(300) NOT NULL, `IdCampaign` VARCHAR(50) NOT NULL, `IdLink` INT NOT NULL,`NbClick` INT NULL DEFAULT 0,`FirstClickDate` datetime NULL, PRIMARY KEY(`Receipient`, `IdLink`)); ", Name);
                        oCom.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        //TODO : Manage rollback previous actions (remove zTable and campaign)
                        throw ex;
                    }
                }
                else
                {
                    oCom.Parameters["UpdatedDate"].Value = DateTime.Now;

                    oCom.CommandText = "UPDATE campaign SET Name=@Name,EmailSent=@EmailSent, UpdatedDate=@UpdatedDate, Domain=@Domain, DynamicField=@DynamicField, OriginalBat=@OriginalBat, TrackedBat=@TrackedBat WHERE _id=@_id;";
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

        public bool Remove()
        {
            bool success = false;
            MySqlCommand oCom = new MySqlCommand(String.Empty, DataBase.Connection, null);
            try
            {
                oCom.Parameters.Add("_id", MySqlDbType.String);
                oCom.Parameters["_id"].Value = _id;
                oCom.CommandText = "DELETE FROM campaign WHERE _id=@_id;";
                oCom.ExecuteNonQuery();
                oCom.CommandText = "DELETE FROM link WHERE IdCampaign=@_id;";
                oCom.ExecuteNonQuery();
                oCom.CommandText = string.Format("DROP TABLE `z{0}`;", this.Name);
                oCom.ExecuteNonQuery();
                oCom.CommandText = string.Format("DROP TABLE `z{0}link`;", this.Name);
                oCom.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                success = false;
            }
            finally
            {
                oCom.Dispose();
            }
            return success;
        }

        public static bool LogOpener(string oReceipient, string oIdCampaign)
        {
            bool success = true;
            MySqlCommand oCom = new MySqlCommand(String.Empty, DataBase.Connection, null);
            Campaign oCampaign = new Campaign(new Guid(oIdCampaign));
            try
            {
                oCom.Parameters.Add("Receipient", MySqlDbType.String);
                oCom.Parameters["Receipient"].Value = oReceipient;
                oCom.Parameters.Add("IdCampaign", MySqlDbType.String);
                oCom.Parameters["IdCampaign"].Value = oIdCampaign;
                oCom.CommandText = string.Format("SELECT count(*) FROM z{0} WHERE Receipient=@Receipient AND IdCampaign=@IdCampaign", oCampaign.Name);
                if (Convert.ToInt32(oCom.ExecuteScalar()) == 0)
                {
                    MySqlCommand oCom2 = new MySqlCommand(string.Format("INSERT INTO z{0} (Receipient,IdCampaign,IsOpener,IsClicker,IsHardBounce,IsUnsubscribe,FirstOpenerDate,FirstClickerDate,FirstUnsubscriptionDate)" +
                        " VALUES (@Receipient,@IdCampaign,1,0,0,0,NOW(),null,null);", oCampaign.Name), DataBase.Connection, null);

                    oCom2.Parameters.Add("Receipient", MySqlDbType.String);
                    oCom2.Parameters["Receipient"].Value = oReceipient;
                    oCom2.Parameters.Add("IdCampaign", MySqlDbType.String);
                    oCom2.Parameters["IdCampaign"].Value = oIdCampaign;
                    oCom2.ExecuteNonQuery();
                    oCom2.Dispose();
                }
            }
            catch(Exception ex)
            {
                success = false;
            }
            finally
            {
                oCom.Dispose();
            }
            return success;
        }

        public static bool LogClicker(string oReceipient, string oIdCampaign)
        {
            bool success = true;
            MySqlCommand oCom = new MySqlCommand(String.Empty, DataBase.Connection, null);
            Campaign oCampaign = new Campaign(new Guid(oIdCampaign));
            // log opener (To click, it's necessary to open
            LogOpener(oReceipient, oCampaign._id.ToString());
            try
            {
                oCom.Parameters.Add("Receipient", MySqlDbType.String);
                oCom.Parameters["Receipient"].Value = oReceipient;
                oCom.Parameters.Add("IdCampaign", MySqlDbType.String);
                oCom.Parameters["IdCampaign"].Value = oIdCampaign;
                oCom.CommandText = string.Format("SELECT count(*) FROM z{0} WHERE Receipient=@Receipient AND IdCampaign=@IdCampaign AND IsClicker=1", oCampaign.Name);
                if (Convert.ToInt32(oCom.ExecuteScalar()) == 0)
                {
                    oCom.CommandText = string.Format("UPDATE z{0} SET IsClicker=1, FirstClickerDate=NOW() WHERE Receipient=@Receipient AND IdCampaign=@IdCampaign", oCampaign.Name);
                    oCom.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                success = false;
            }
            finally
            {
                oCom.Dispose();
            }
            return success;
        }

        public static bool LogUnsubscribe(string oReceipient, string oIdCampaign=null)
        {
            bool success = true;
            MySqlCommand oCom = new MySqlCommand(String.Empty, DataBase.Connection, null);
            Campaign oCampaign = new Campaign(new Guid(oIdCampaign));
            // log opener (To click, it's necessary to open
            LogOpener(oReceipient, oCampaign._id.ToString());
            try
            {
                oCom.Parameters.Add("Receipient", MySqlDbType.String);
                oCom.Parameters["Receipient"].Value = oReceipient;
                oCom.Parameters.Add("IdCampaign", MySqlDbType.String);
                oCom.Parameters["IdCampaign"].Value = oIdCampaign;
                oCom.CommandText = string.Format("SELECT count(*) FROM z{0} WHERE Receipient=@Receipient AND IdCampaign=@IdCampaign AND IsUnsubscribe=1", oCampaign.Name);
                if (Convert.ToInt32(oCom.ExecuteScalar()) == 0)
                {
                    oCom.CommandText = string.Format("UPDATE z{0} SET IsUnsubscribe=1, FirstUnsubscriptionDate=NOW() WHERE Receipient=@Receipient AND IdCampaign=@IdCampaign", oCampaign.Name);
                    oCom.ExecuteNonQuery();
                    oCom.CommandText = "INSERT INTO unsubscriptions (Receipient, IdCampaign, Timestamp) VALUES (@Receipient,@IdCampaign,NOW())";
                    oCom.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                success = false;
            }
            finally
            {
                oCom.Dispose();
            }
            return success;
        }

        public static string GetTrackedLink(int id, string Receipient)
        {
            string returnedlink = string.Empty;
            MySqlCommand oCom = new MySqlCommand(String.Empty, DataBase.Connection, null);
            try
            {
                oCom.Parameters.Add("_id", MySqlDbType.Int32);
                oCom.Parameters["_id"].Value = id;
                oCom.CommandText = "SELECT IdCampaign, Link FROM link WHERE _id=@_id;";
                MySqlDataReader oReader = oCom.ExecuteReader(CommandBehavior.SingleRow);
                if (oReader.Read())
                {
                    returnedlink = oReader["Link"].ToString();
                    // save the track.
                    Campaign oCampaign = new Campaign(new Guid(oReader["IdCampaign"].ToString()));
                    LogClicker(Receipient, oCampaign._id.ToString());
                    // Find if this opener is already loged or not.
                    MySqlCommand oCom2 = new MySqlCommand(string.Format("SELECT count(*) FROM z{0}link WHERE Receipient=@Receipient AND IdLink=@IdLink AND IdCampaign=@IdCampaign;", oCampaign.Name), DataBase.Connection, null);
                    oCom2.Parameters.Add("IdLink", MySqlDbType.Int32);
                    oCom2.Parameters["IdLink"].Value = id;
                    oCom2.Parameters.Add("IdCampaign", MySqlDbType.String);
                    oCom2.Parameters["IdCampaign"].Value = oCampaign._id.ToString();
                    oCom2.Parameters.Add("Receipient", MySqlDbType.String);
                    oCom2.Parameters["Receipient"].Value = Receipient;
                    if (Convert.ToInt32(oCom2.ExecuteScalar()) == 0)
                    {
                        oCom2.CommandText = string.Format("INSERT INTO z{0}link (Receipient,IdCampaign,IdLink,NbClick,FirstClickDate) VALUES (@Receipient,@IdCampaign,@IdLink,1,NOW());", oCampaign.Name);
                        oCom2.ExecuteNonQuery();
                    }
                    else
                    {
                        oCom2.CommandText = string.Format("SELECT NbClick FROM z{0}link WHERE Receipient=@Receipient AND IdLink=@IdLink AND IdCampaign=@IdCampaign;", oCampaign.Name);
                        int Cpt = Convert.ToInt32(oCom2.ExecuteScalar());
                        oCom2.Parameters.Add("NbClick", MySqlDbType.Int32);
                        oCom2.Parameters["NbClick"].Value = (Cpt+1);
                        oCom2.CommandText = string.Format("UPDATE z{0}link SET NbClick=@NbClick WHERE Receipient=@Receipient AND IdLink=@IdLink AND IdCampaign=@IdCampaign;", oCampaign.Name);
                        oCom2.ExecuteNonQuery();
                    }
                }
                else
                    throw new Exception("Fatal error: Link not found!");
            }
            catch (Exception ex)
            {
            }
            finally
            {
                oCom.Dispose();
            }
            return returnedlink;
        }

        public static string TrackContent(string body, string domain, string dynamicEmailField, bool saveBat, string campaign, string oIdCampaign)
        {
            Campaign oCampaign = new Campaign(new Guid(oIdCampaign));
            string newcontent = body;
            // Track content
            Regex oreg = new Regex(@"href\s*=\s*(?:""(?<1>[^""]*)""|(?<1>\S+))");
            MatchCollection Result = oreg.Matches(body);
            if (Result.Count > 0) // no need if no link
            {
                // track links
                System.Collections.Hashtable oNewLink = new System.Collections.Hashtable();
                foreach (Match oMatsh in Result)
                {
                    string oUrlTempo = Removehref(oMatsh.Value); string ext = oMatsh.Value.Substring(oMatsh.Value.Length - 4, 4).ToLower();
                    if ((ext != ".jpg") && (ext != ".gif") && (ext != ".jepg") && (ext != ".png") && (oMatsh.Value.IndexOf("mailto") < 0) && (oMatsh.Value.IndexOf("file") < 0))
                    {
                        if (!oNewLink.ContainsKey(oUrlTempo))
                            oNewLink.Add(oUrlTempo, null);
                    }
                }
                foreach(string okey in oNewLink.Keys)
                {
                    string newlink = string.Format("https://{0}/Actions/o/{2}/?key={1}", domain,dynamicEmailField, TrackLink(oIdCampaign, okey).ToString());
                    newcontent = newcontent.Replace(okey, newlink);
                }
            }
            string endcontent = string.Empty;
            int endbodyposition = newcontent.ToLower().IndexOf("</body>");
            // Add img before /body or at the end
            string TagImg = string.Format("<img src=\"https://{0}/Actions/op/{1}/?key={2}\" />", oCampaign.Domain, oIdCampaign, dynamicEmailField);
            string TagUnsubscribe = string.Format("<div>To unsubscribe and no longer receive our emails, please use <a href=\"https://{0}/Actions/Unsubscribe/{1}/?key={2}\" > this link</a>.</div>", oCampaign.Domain, oIdCampaign, dynamicEmailField);
            if (endbodyposition > 0)
            {
                endcontent = newcontent.Substring(0, endbodyposition-1);
                endcontent = string.Concat(endcontent, TagUnsubscribe, TagImg, newcontent.Substring(endbodyposition, (newcontent.Length - (endbodyposition))));
            }
            else
                endcontent = string.Concat(newcontent, TagUnsubscribe, TagImg);
            return endcontent;
        }

        private static int TrackLink(string campaign, string finalLink)
        {
            int oReturn = 0;
            MySqlCommand oCom = new MySqlCommand(String.Empty, DataBase.Connection, null);
            try
            {
                oCom.Parameters.Add("IdCampaign", MySqlDbType.String);
                oCom.Parameters.Add("oldLink", MySqlDbType.String);
                oCom.Parameters["IdCampaign"].Value = campaign;
                oCom.Parameters["oldLink"].Value = finalLink;
                oCom.CommandText = "INSERT INTO link (IdCampaign,Link) VALUES (@IdCampaign,@oldLink);SELECT LAST_INSERT_ID();";
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
