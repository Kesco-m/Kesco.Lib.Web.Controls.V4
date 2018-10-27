using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kesco.Lib.BaseExtention.Enums.Controls;

namespace Kesco.Lib.Web.Controls.V4
{
    public class Menu : V4Control
    {
        /// <summary>
        ///     Конструктор по умолчанию
        /// </summary>
        public Menu()
        {
            MenusItems = new List<MenuTree>();
        }

        public List<MenuTree> MenusItems { get; set; }

        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        /// <param name="w">Поток</param>
        public override void RenderControl(TextWriter w)
        {
            var menuTop = String.Format(@"<div id=""pageHeader"" class='v4divHeader' style=""z-index:9999;""><div> ");
            var menuBottom = String.Format(@"</div></div>");
            var menuBody = "";

            menuBody = HtmlButtons(menuBody);

            var menuHtml = String.Format(@"{0}{1}{2}", menuTop, menuBody, menuBottom);

            menuHtml += "<script>activateMenuScriptPart();</script>";
            menuHtml += "<style>.ui-icon-menu-person { background-position: -147px -96px; padding-left: 2px; }" +
                        ".ui-icon-menu-home { background-position: 0 -114px; padding-left: 2px; }" +
                        ".ui-icon-menu-person { background-position: -144px -98px; padding-left: 2px;}" +
                        ".ui-icon-menu-search { background-position: -160px -114px; padding-left: 2px;}" +
                        ".ui-icon-menu-copy { background-position: -176px -130px; padding-left: 2px;}" +
                        ".ui-icon-menu-print { background-position: -160px -98px; padding-left: 2px;}" +
                        ".ui-icon-menu-trash { background-position: -176px -98px; padding-left: 2px;}</style>";
            w.Write(menuHtml);
        }

        private string HtmlButtons(string menuBody)
        {
            foreach (var item in MenusItems.OrderBy(t => t.Order))
            {
                menuBody +=
                    String.Format(
                        @"{1}<div class='v4firstMenuButtonDiv' id='div{0}_0' style='display: inline-block;'>",
                        item.ItemID,
                        item.BeforeButtonSeparator
                            ? String.Format(
                                @"<img src='/styles/TabsBGVertical.gif' alt='' class='v4MenuSeparator'></img>")
                            : "");
                //menuBody += String.Format(@"<button class='v4menuButton {7}' id='btn{0}' {1} {2}>{6}{5}{3}{4}</button>",
                menuBody += String.Format(@"<button class='{7}' id='btn{0}' {1} {2}>{6}{5}{3}{4}</button>",
                    item.ItemID + "_0",
                    !String.IsNullOrEmpty(item.Style)
                        ? "style='" + item.Style + "'"
                        : " style='width:" + item.ButtonWidth + "px;'",
                    ReturnActionHtml(item),
                    String.Format(@"<label {1}>{0}</label>", item.NameRUS,
                        !String.IsNullOrEmpty(item.Image) || !String.IsNullOrEmpty(item.ImageFromCollection)
                            ? "class='v4menuButtonTextWithImg'"
                            : ""),
                    item.ItemsList.Count != 0 ? "<img src='/styles/ScrollDownEnabled.gif' class='v4menuArrowDown'>" : "",
                    !String.IsNullOrEmpty(item.Image)
                        ? String.Format(@"<img class='v4menuButtonImage' src='/styles/{0}'>", item.Image)
                        : "",
                    !String.IsNullOrEmpty(item.ImageFromCollection)
                        ? String.Format(@"<span class='{0}' class='v4menuArrowDown'></span>", item.ImageFromCollection)
                        : "",
                    //item.ItemsList.Count != 0 ? "v4menuButtonFirstLineOpener" : ""
                    ""
                    );

                var itemsMenuBody = "";
                if (item.ItemsList.Count != 0)
                {
                    itemsMenuBody +=
                        String.Format(
                            @"<table class='v4menuTable' style='position: absolute; display: none; z-index: 5001;'>");
                    foreach (var innerItems in item.ItemsList.OrderBy(t => t.Order))
                    {
                        itemsMenuBody += HtmlButtonsBottom(innerItems);
                    }
                    itemsMenuBody += String.Format(@"</table>");
                }
                menuBody += itemsMenuBody;
                menuBody += String.Format(@"</div>{0}",
                    item.AfterButtonSeparator
                        ? String.Format(@"<img src='/styles/TabsBGVertical.gif' class='v4MenuSeparator' alt=''></img>")
                        : "");
            }
            return menuBody;
        }

        private string HtmlButtonsBottom(MenuTree item)
        {
            var menuBody = "";

            menuBody +=
                String.Format(
                    @"<tr class='v4menuTRopener'><td>{6}<button class='v4menuButton  {7}' id='btn{0}' {1} {2}>{5}{4} {3}</button>",
                    item.ItemID + "_0",
                    !String.IsNullOrEmpty(item.Style)
                        ? "style='" + item.Style + "'"
                        : " style='width:" + item.ButtonWidth + "px; text-align:left;'",
                    ReturnActionHtml(item),
                    String.Format(@"<label {1} >{0}</label>", item.NameRUS,
                        !String.IsNullOrEmpty(item.Image) || !String.IsNullOrEmpty(item.ImageFromCollection)
                            ? "class='v4menuButtonTextWithImg'"
                            : ""),
                    !String.IsNullOrEmpty(item.Image)
                        ? String.Format(@"<img class='v4menuButtonImage' src='/styles/{0}'>", item.Image)
                        : "",
                    !String.IsNullOrEmpty(item.ImageFromCollection)
                        ? String.Format(@"<span class='{0}' style='position:absolute'></span>", item.ImageFromCollection)
                        : "",
                    item.BeforeButtonSeparator
                        ? String.Format(
                            @"<img src='/styles/TabsBGHorisontal.gif' alt='' class='v4MenuSeparatorHorisont'></img>")
                        : "",
                    item.ItemsList.Count == 0 ? "v4menuButtonLineOpener" : "");

            if (item.ItemsList.Count != 0)
            {
                menuBody += String.Format(@"<td id='dt{0}' style='display:none; position: absolute;'>",
                    item.ItemID + "_0");
                menuBody += String.Format(@"<table class='v4menuTable'>");

                menuBody = item.ItemsList.Aggregate(menuBody,
                    (current, innerItem) => current + HtmlButtonsBottom(innerItem));

                menuBody += String.Format(@"</table>");
                menuBody += String.Format(@"{0}</td>",
                    item.AfterButtonSeparator
                        ? String.Format(
                            @"<img src='/styles/TabsBGHorisontal.gif' alt='' class='v4MenuSeparatorHorisont'></img>")
                        : "");
            }


            menuBody += String.Format(@"</td></tr>");
            return menuBody;
        }

        private string ReturnActionHtml(MenuTree Item)
        {
            switch (Item.ActionType)
            {
                case MenuButtonActionType.UrlAction:
                    return
                        String.Format(
                            "onclick='v4_windowOpen(\"{0}\", \"{1}\", \"menubar={2},location={3},resizable={4},scrollbars={5},status={6},height={7},width={8}\"); return false;'",
                            Item.Action.Url,
                            Item.ItemID,
                            Item.Action.Menubar ? "1" : "0",
                            Item.Action.Location ? "1" : "0",
                            Item.Action.Resizable ? "1" : "0",
                            Item.Action.Scrollbars ? "1" : "0",
                            Item.Action.Status ? "1" : "0",
                            Item.Action.Height,
                            Item.Action.Width);
                case MenuButtonActionType.CmdAction:
                    var cmdParametrs = "";
                    foreach (var param in Item.Action.CmdParams)
                        cmdParametrs = cmdParametrs + string.Format("\"{0}\", \"{1}\",", param.Key, param.Value);

                    if (cmdParametrs != "") cmdParametrs = cmdParametrs.Remove(cmdParametrs.Length - 1, 1);
                    return String.Format("onclick='cmd(\"cmd\", \"{0}\"{1})'", Item.Action.CmdActionName,
                        !String.IsNullOrEmpty(cmdParametrs) ? "," + cmdParametrs : "");
                case MenuButtonActionType.JSAction:
                    return String.Format("onclick='{0}'", Item.Action.Url);
                case MenuButtonActionType.None:
                    return "";
            }
            return null;
        }

        /// <summary>
        ///     Отправка клиенту скрипта с изменениями контрола
        /// </summary>
        public override void Flush()
        {
        }

        public class MenuActionParametrs
        {
            public MenuActionParametrs()
            {
                OpenNewWindow = true;
                Menubar = true;
                Location = true;
                Resizable = true;
                Scrollbars = true;
                Status = true;
                Height = 500;
                Width = 600;
                CmdParams = new Dictionary<string, string>();
            }

            public string Url { get; set; }
            public bool OpenNewWindow { get; set; }
            public bool Menubar { get; set; }
            public bool Location { get; set; }
            public bool Resizable { get; set; }
            public bool Scrollbars { get; set; }
            public bool Status { get; set; }
            public int Height { get; set; }
            public int Width { get; set; }
            public string CmdActionName { get; set; }
            public Dictionary<String, String> CmdParams { get; set; }
        }

        public class MenuTree
        {
            public MenuTree()
            {
                ItemsList = new List<MenuTree>();
                ActionType = MenuButtonActionType.None;
                Action = new MenuActionParametrs();
                ButtonWidth = 100;
            }

            public string ItemID { get; set; }
            public string NameRUS { get; set; }
            public string Style { get; set; }
            public int ButtonWidth { get; set; }
            public string Image { get; set; }
            public string ImageFromCollection { get; set; }
            public int Order { get; set; }
            public bool BeforeButtonSeparator { get; set; }
            public bool AfterButtonSeparator { get; set; }
            public MenuButtonActionType ActionType { get; set; }
            public MenuActionParametrs Action { get; set; }
            public List<MenuTree> ItemsList { get; set; }
        }
    }
}