using System;
using System.Linq;
using System.Text;
using System.Web;
using Kesco.Lib.Web.Comet;
using Kesco.Lib.Web.Controls.V4.Common;
using System.Collections.Generic;

namespace Kesco.Lib.Web.Controls.V4.Handlers
{
    public class CometServerHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.Write("<html>");
            context.Response.Write("<head>");
            context.Response.Write("<title>Активные соединения</title>");
            context.Response.Write("<link href='/Styles/Kesco.V4/CSS/Kesco.V4.css' rel='stylesheet' type='text/css'/>");
            context.Response.Write("</head>");
            context.Response.Write("<body>");

            var sb = new StringBuilder();
            var list = CometServer.Connections.AsEnumerable().OrderByDescending(o => o.Start);
            
            sb.Append("<table class='grid'>");
            sb.Append("<tr class='gridHeader'>");
            sb.Append("<td>Пользователь</td>");
            sb.Append("<td>Идентификатор страницы</td>");
            sb.Append("<td>Страница</td>");
            sb.Append("<td>Код объекта</td>");
            sb.Append("<td>Что делает</td>");
            sb.Append("<td>Актуализация сессии</td>");
            sb.Append("</tr>");
            foreach (var state in list)
            {
                var page = context.Application[state.ClientGuid] as Page;

                sb.Append("<tr>");

                sb.Append("<td>");
                sb.Append(page == null ? "<пусто>" : page.CurrentUser.FIO);
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append(state.ClientGuid);
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append(page == null ? "<пусто>" : page.AppRelativeVirtualPath);
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append(state.Id);
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append(state.IsEditable ? "Редактирует" : "Просматривает");
                sb.Append("</td>");

                sb.Append("<td>");
                sb.Append(state.Start.ToString("dd.MM.yyyy HH:mm:ss"));
                sb.Append("</td>");

                sb.Append("</tr>");
            }

            var pageCount = 0;
            var nullObjCount = 0;
            for (var i = context.Application.Keys.Count - 1; i >= 0; i--)
            {
                var key = context.Application.GetKey(i);
                if (context.Application[key] == null)
                    nullObjCount++;
                else if (context.Application[key] is Page)
                    pageCount++;
            }

            sb.Append("</table>");

            sb.Append("</br>");
            sb.Append("<span>Объектов в Application: <strong>");
            sb.Append(context.Application.Count);
            sb.Append("</strong></span>");

            sb.Append("</br>");
            sb.Append("<span>Объектов в Application являющиеся v4Page: <strong>");
            sb.Append(pageCount);
            sb.Append("</strong></span>");

            sb.Append("</br>");
            sb.Append("<span>Объектов в Application содержащим null: <strong>");
            sb.Append(nullObjCount);
            sb.Append("</strong></span>");

            if (context.Application.AllKeys.Contains("Error"))
            {
                // эти ошибки ловятся в Global.asax в методе Application_Error
                sb.Append("</br>");
                sb.Append("</br>");
                sb.Append("<div>В Application обнаружен объект ошибки:</div>");
                var exObj = context.Application["Error"] as Exception;
                if (exObj != null)
                {
                    sb.Append(exObj.Message);
                    sb.Append(Environment.NewLine);
                    sb.Append(exObj.StackTrace);
                }
            }

            context.Response.Write(sb);
            context.Response.Write("</body></html>");
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}