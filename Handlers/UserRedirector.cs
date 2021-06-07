using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using Kesco.Lib.DALC;
using Kesco.Lib.Entities;
using Kesco.Lib.Log;
using Kesco.Lib.Web.Settings;

namespace Kesco.Lib.Web.Controls.V4.Handlers
{
    public class UserRedirector : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var id = context.Request.QueryString["id"];
            if (string.IsNullOrEmpty(id)) return;

            try
            {
                var sqlParams = new Dictionary<string, object>();
                sqlParams.Add("@КодСотрудника", id);

                var dt = DBManager.GetData(SQLQueries.SELECT_КодЛицаПоКодуСотрудника, Config.DS_person, CommandType.Text, sqlParams);

                var qs = context.Request.QueryString;
                var qps = qs.AllKeys.ToDictionary(k => k.ToLower(), k => qs[k]);

                
                if (dt != null && dt.Rows.Count > 0 && !dt.Rows[0]["КодЛица"].Equals(DBNull.Value))
                {
                    qps["id"] = dt.Rows[0]["КодЛица"].ToString();
                    if (!qps.ContainsKey("hideoldver"))
                        qps.Add("hideoldver", "false");                        
                }
                else
                {
                    qps.Remove("id");
                    if (!qps.ContainsKey("employeeid"))
                        qps.Add("employeeid", id);
                }

                var url = Config.person_form;

                if (url.Contains("?"))
                    url += "&";
                else
                    url += "?";

                url += CreateQueryString(qps);

                context.Response.Redirect(url, false);

            }
            catch (SqlException ex)
            {
                Logger.WriteEx(new DetailedException(ex.Message, ex));
            }
            catch (Exception ex)
            {
                string sContext = "Context:\n";
                foreach (string key in context.Request.Headers.AllKeys)
                    sContext += String.Format("[{0}]->[{1}]\n", key, context.Request.Headers[key]);

                Logger.WriteEx(new DetailedException(ex.Message, ex, sContext));
            }
        }

        public string CreateQueryString(Dictionary<string, string> parameters)
        {
            return string.Join("&", parameters.Select(kvp =>
               string.Format("{0}={1}", kvp.Key, HttpUtility.UrlEncode(kvp.Value))));
        }

        public  Dictionary<string, string> ParseQueryString(string queryString)
        {
            var nvc = HttpUtility.ParseQueryString(queryString);
            return nvc.AllKeys.ToDictionary(x => x, x => nvc[x]);
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
