using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.Log;

namespace Kesco.Lib.Web.Controls.V4
{
    public class MenuSimple: V4Control
    {
        public List<MenuSimpleItem> MenuItems;
        public MenuSimple() {
            MenuItems = new List<MenuSimpleItem>();
        }

        private string CurrentMenuId {get; set;}

        public override void RenderControl(TextWriter w)
        {
            if (V4Page.V4Request == null) return;
            CurrentMenuId = V4Page.V4Request.QueryString["menuId"];

            if (MenuItems.Count == 0) return;

            w.Write("<div id=\"pageHeader\" class=\"v4pageHeader\" style=\"overflow:visible !important; z-index:9999;\">");

            w.Write("<div>");

            w.Write($"<ul class='menu' id='{ID}'>");

            MenuItems.ForEach(item => {

                var url = "";
                var onclick = "";
                GetItemParams(item, ref url, ref onclick);

                w.Write("<li class='parentmenu'>");


                if (!string.IsNullOrEmpty(item.Img))
                {
                    w.Write($"<a {url} {onclick} class='menulink '>");
                    w.Write($"<img src='{item.Img}'>&nbsp;");
                }
                else
                    w.Write($"<a {url} {onclick} class='menulink menulinkfixheight'>");
                
                w.Write(item.Caption);
                w.Write("</a>");
                    RenderChildItems(w, item);
                w.Write("</li>");
            });

            w.Write("</ul>");

            foreach (var b in V4Page.MenuButtons)
            {
                V4Page.V4Controls.Add(b);
                b.RenderControl(w);
                b.PropertyChanged.Clear();
            }

            if (!string.IsNullOrEmpty(V4Page.HelpUrl))
            {
                var btnHelp = new Button
                {
                    ID = "btnHelp",
                    Text = "",
                    Title = Resx.GetString("lblHelp"),
                    Width = 28,
                    Height = 22,
                    IconJQueryUI = ButtonIconsEnum.Help,
                    OnClick = string.Format("v4_openHelp('{0}');", V4Page.IDPage),
                    Style = "float: right; margin-right: 11px;"
                };
                btnHelp.RenderControl(w);
                btnHelp.PropertyChanged.Clear();
            }

            if (!V4Page.LikeId.IsNullEmptyOrZero())
            {
                var btnLike = new LikeDislike
                {
                    ID = "btnLike",
                    V4Page = V4Page,
                    LikeId = V4Page.LikeId,
                    Style = "float: right; margin-right: 11px; margin-top: 3px; cursor: pointer;"
                };


                V4Page.V4Controls.Add(btnLike);
                btnLike.RenderControl(w);
            }


            w.Write("</div>");
            w.Write("<div style=\"clear: both; line-height: 0; height: 0;\">&nbsp;</div>");
            w.Write("</div>");

            var menuHtml = $"<script>v4_menu.init('{ID}', 'menuhover'); </script>";
            w.Write(menuHtml);

        }
               

        private void RenderChildItems(TextWriter w, MenuSimpleItem rootItem)
        {
            if (rootItem.MenuItems.Count == 0) return;
            w.Write("<ul>");

            rootItem.MenuItems.ForEach(item =>
            {
                var url = "";
                var onclick = "";
                GetItemParams(item, ref url, ref onclick);

                w.Write("<li class='childmenu'>");

                w.Write($"<a {url} {onclick} class='{(item.MenuItems.Count > 0 ? "sub" : "")}'>");

                if (CurrentMenuId == item.Caption.GetHashCode().ToString())
                    w.Write($"<span style='position:absolute; left: 0px; top: 7px; height: 5px;  width: 5px;  background-color: green;  border-radius: 50%;  display: inline-block;'></span>");

                if (!string.IsNullOrEmpty(item.Img))
                    w.Write($"<span class='sImg' style='display: inline-block;'><img class='iImg' src='{item.Img}'/></span>&nbsp;");

                w.Write(item.Caption);

                if (item.MenuItems.Count > 0) w.Write("<img style='float:right;padding-top:4px;' src='/styles/popup.gif'/>");
                w.Write("</a>");

                RenderChildItems(w, item);

                w.Write("</li>");
            });
            
            w.Write("</ul>");
        }

        private void GetItemParams(MenuSimpleItem item, ref string url, ref string onclick)
        {
            url = "";
            onclick = "";

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

    public class MenuSimpleItem
    {
        public string Id { get; set; }
        public string Caption { get; set; }
        public string Href { get; set; }
        public string Img { get; set; }


        public List<MenuSimpleItem> MenuItems;
        public MenuSimpleItem()
        {
            MenuItems = new List<MenuSimpleItem>();
        }
    }

}
