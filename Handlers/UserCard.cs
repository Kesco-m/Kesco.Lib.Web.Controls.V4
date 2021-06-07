using System;
using System.Configuration;
using System.Net;
using System.Web;
using Kesco.Lib.Log;
using Kesco.Lib.Web.Controls.V4.RS;
using Kesco.Lib.Web.Settings;

namespace Kesco.Lib.Web.Controls.V4.Handlers
{
    public class UserCard : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            byte[] result;
            string encoding;
            string mimeType;
            string extension;
            string[] streamIDs = null;
            Warning[] warnings = null;

            var uId = 0;
            if (context.Request.QueryString["uId"] != null) uId = int.Parse(context.Request.QueryString["uId"]);

            var rs = new ReportExecutionService
            {
                Url = ConfigurationManager.AppSettings["URI_report_srv"],
                Credentials = CredentialCache.DefaultCredentials
            };

            var parameters = new ParameterValue[1];
            if (uId != 0)
            {
                parameters[0] = new ParameterValue {Name = "КодСотрудника", Value = uId.ToString()};
            }

            try
            {
                var devInfo =
                    string.Format(
                        @"<DeviceInfo><DpiX>300</DpiX><DpiY>300</DpiY><ColorDepth>32</ColorDepth><OutputFormat>PDF</OutputFormat><StreamRoot>" +
                        Config.styles + "</StreamRoot></DeviceInfo>");

                var execInfo = new ExecutionInfo();
                var execHeader = new ExecutionHeader();

                rs.ExecutionHeaderValue = execHeader;

                execInfo = rs.LoadReport("/INVENTORY/карточка", null);

                rs.SetExecutionParameters(parameters, "en-us");

                try
                {
                    result = rs.Render("PDF", devInfo, out extension, out encoding, out mimeType, out warnings, out streamIDs);
                }
                catch (Exception ex)
                {
                    Logger.WriteEx(ex);
                    throw ex;
                }

                context.Response.Clear();
                context.Response.Buffer = true;
                context.Response.AddHeader("Content-Disposition", "inline; filename=Report_" + uId + ".pdf");
                context.Response.ContentType = "application/octet-stream";
                context.Response.BinaryWrite(result);
                context.Response.Flush();
            }

            catch (Exception e)
            {
                context.Response.Write(e.Message);
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
