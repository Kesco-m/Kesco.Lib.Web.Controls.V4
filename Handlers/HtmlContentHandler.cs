using System.Text.RegularExpressions;
using System.Web;
using Kesco.Lib.Entities.Documents.EF.Applications;

namespace Kesco.Lib.Web.Controls.V4.Handlers
{
    public class HtmlContentHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var _id = context.Request.QueryString["id"];
            var _rpt = context.Request.QueryString["rpt"];
            var _orient = context.Request.QueryString["orientation"];

            if (string.IsNullOrEmpty(_orient))
            {
                var w = context.Response.Output;

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

                if (_id == null || !Regex.IsMatch(_id, "^\\d+$", RegexOptions.IgnoreCase) || int.Parse(_id) <= 0)
                {
                    w.Write("<font color='red'>Ошибка</font>!<br>Не указан код документа.");
                }
                else
                {
                    var d = new Vacation(_id);

                    if (d.Unavailable)
                    {
                        w.Write("<font color='red'>Ошибка</font>!<br>Нет доступа к документу с кодом #{0}", _id);
                    }
                    else if (d.DataUnavailable)
                    {
                        w.Write(
                            "<font color='red'>Ошибка</font>!<br>У документа с кодом #{0} нет электоронной формы.",
                            _id);
                    }
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

        public bool IsReusable => false;
    }
}