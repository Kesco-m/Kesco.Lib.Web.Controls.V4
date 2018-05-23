using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;
using System.Text.RegularExpressions;
using Kesco.Lib.Entities.Documents.EF.Trade;

namespace Kesco.Lib.Web.Controls.V4.Handlers
{
    public  class HtmlContentHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            string _id = context.Request.QueryString["id"];
            string _rpt = context.Request.QueryString["rpt"];
            string _orient = context.Request.QueryString["orientation"];

            if (string.IsNullOrEmpty(_orient))
            {
                TextWriter w = context.Response.Output;

                w.Write(@"<html>
	<head>
		<title>Просмотр текста документа...</title>
		<style>
			.spnRq {text-indent:0px; border:0; }
			.spn { text-indent:25px }
		</style>
		<script language=javascript>
		<!--
			window.attachEvent('onload',function(){window.focus();});
			window.name = ""report"";
		//-->
		</script>
	</head>");

                w.Write("<body oncontextmenu='return false;'>");

                if ((_id == null) || (!Regex.IsMatch(_id, "^\\d+$", RegexOptions.IgnoreCase)) || int.Parse(_id) <= 0)
                    w.Write("<font color='red'>Ошибка</font>!<br>Не указан код документа.");
                else
                {
                    Entities.Documents.EF.Applications.Vacation d = new Entities.Documents.EF.Applications.Vacation(_id);
                 
                    if (d.Unavailable) w.Write("<font color='red'>Ошибка</font>!<br>Нет доступа к документу с кодом #{0}", _id);
                    else if (d.DataUnavailable)
                        w.Write(
                            "<font color='red'>Ошибка</font>!<br>У документа с кодом #{0} нет электоронной формы.",
                            _id);
                    else
                    {
                        if (_rpt == null) _rpt = "2";
                        w.Write(_rpt.Equals("2") ? d.GetText_Full() : d.GetText());
                    }

                }

                w.Write("</body>");
                w.Write("</html>");
            }
           
            context.Response.CacheControl = "no-cache";

        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
