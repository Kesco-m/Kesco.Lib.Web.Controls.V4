using Kesco.Lib.Log;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Контекстное меню
    /// </summary>
    public class CommandMenu : V4Control
    {
        protected string CurrentMenuId { get; set; }

        /// <summary>
        ///     Список пунктов контекстного меню
        /// </summary>
        public List<MenuSimpleItem> MenuItems;

        /// <summary>
        ///     Конструктор контекстного меню
        /// </summary>
        public CommandMenu()
        {
            MenuItems = new List<MenuSimpleItem>();
        }

        /// <summary>
        ///     Метод отрисовки контекстного меню
        /// </summary>
        public override void RenderControl(TextWriter w)
        {
            if (V4Page.V4Request == null) return;
            CurrentMenuId = V4Page.V4Request.QueryString["menuId"];

            if (MenuItems.Count == 0) return;

            w.Write("<div>");
            w.Write($"<ul class='menu' id='{ID}'>");
            w.Write("<ul>");

            MenuItems.ForEach(item =>
            {
                GetItemParams(item, out string url, out string onclick);

                w.Write("<li class='childmenu'>");

                w.Write($"<a {url} {onclick} class='{(item.MenuItems.Count > 0 ? "sub" : "")}'>");

                if (CurrentMenuId == item.Caption.GetHashCode().ToString())
                    w.Write($"<span style='position:absolute; left: 0px; top: 7px; height: 5px;  width: 5px;  background-color: green;  border-radius: 50%;  display: inline-block;'></span>");

                if (!string.IsNullOrEmpty(item.Img))
                    w.Write($"<span class='sImg' style='display: inline-block;'><img class='iImg' src='{item.Img}'/></span>&nbsp;");

                w.Write(item.Caption);

                if (item.MenuItems.Count > 0) w.Write("<img style='float:right;padding-top:4px;' src='/styles/popup.gif'/>");
                w.Write("</a>");

                w.Write("</li>");
            });
            w.Write("</ul>");
            w.Write("</ul>");
            w.Write("</div>");
            w.Write("<div style=\"clear: both; line-height: 0; height: 0;\">&nbsp;</div>");

            var menuHtml = $"<script>v4_menu.init('{ID}', 'menuhover'); </script>";
            w.Write(menuHtml);
        }

        /// <summary>
        ///     Получить параметры пункта контекстного меню
        /// </summary>
        /// <param name="item">Пункт меню</param>
        /// <param name="url">URL перехода</param>
        /// <param name="onclick">Обработчик нажатия</param>
        protected void GetItemParams(MenuSimpleItem item, out string url, out string onclick)
        {
            url = string.Empty;
            onclick = string.Empty;

            if (string.IsNullOrEmpty(item.Href)) return;

            var pattern_URI = "(@URI_\\w+)";

            var h = item.Href;
            var m = Regex.Match(h, pattern_URI, RegexOptions.IgnoreCase);

            if (m.Success)
            {
                for (var i = 0; i < m.Groups.Count; i++)
                {
                    var uri = ConfigurationManager.AppSettings[m.Groups[i].Value.Replace("@", "")];
                    if (string.IsNullOrEmpty(uri))
                    {
                        Logger.WriteEx(new LogicalException($"Не удалось определить значение параметра {m.Groups[i].Value.Replace("@", "")} из web.config", h, Assembly.GetExecutingAssembly().GetName(), Priority.ExternalError));
                        return;
                    }
                    h = h.Replace(m.Groups[i].Value, uri);
                }
            }

            if (h.IndexOf("javascript", StringComparison.CurrentCultureIgnoreCase) > -1)
            {
                onclick = "onclick=\"" + h + "\"";
                url = "href='javascript: void(0);'";
            }
            else
            {
                url = $"href='{h}'";
            }
        }
    }
}