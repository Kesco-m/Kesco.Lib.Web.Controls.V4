using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.BaseExtention.BindModels;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.Entities;
using Kesco.Lib.Web.Comet;

namespace Kesco.Lib.Web.Controls.V4.Common
{
    /// <summary>
    ///     Базовый класс для стриниц сущностей
    ///     Все страницы, работающие с функционалом V4 и сущностями(Entities) наследуют от него.
    ///     Является абстрактным, т.к. содерщит свойства, которые обязан переопределить на странице
    /// </summary>
    public abstract class EntityPage : Page
    {
        /// <summary>
        ///     Коллекция кнопок меню
        /// </summary>
        private readonly List<Button> _menuButtons;

        /// <summary>
        /// Элемент управления для контроля совместной работы над сущностью
        /// </summary>
        protected Comet _cometUsers;

        /// <summary>
        ///     Конструктор
        /// </summary>
        protected EntityPage()
        {
            _menuButtons = new List<Button>();
        }

        /// <summary>
        ///     сущность страницы
        /// </summary>
        public Entity Entity;

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            EntityFieldInit();
        }

        protected virtual void EntityFieldInit()
        {
            if (Entity != null)
            {
                foreach (var field in Entity.GetType().GetFields())
                {
                    if (field.FieldType.Name != "BinderValue") continue;
                    var method = field.FieldType.GetMethod("ValueChangedEvent_Invoke", new Type[] { typeof(string), typeof(string) });
                    if (method != null)
                    {
                        method.Invoke(((BinderValue)field.GetValue(Entity)), new object[] { ((BinderValue)field.GetValue(Entity)).Value, "" });
                    }
                }
            }
        }

        /// <summary>
        ///     Id сущности (если нужно int значение используй ItemId)
        /// </summary>
        public string EntityId
        {
            get { return ItemId.ToString(); }
            set { ItemId = value.ToInt(); }
        }

        /// <summary>
        /// Название сущности (свойство добавлено для поддержания единого подхода, можно использовать ItemName)
        /// </summary>
        public string EntityName
        {
            get { return ItemName; }
            set { ItemName = value; }
        }

        /// <summary>
        /// Переадресация страницы на указанную
        /// </summary>
        /// <param name="path">Путь для переадресации</param>
        /// <param name="withQueryParams">Добавлять к пути параметры текущей страницы</param>
        public void V4Redirect(string path, bool withQueryParams = true)
        {
            V4Dispose(true);
            Response.Redirect(path + (withQueryParams ? Request.Url.Query : ""), false);
        }

        /// <summary>
        ///     Освобождение ресурсов занятых страницей. Блокирует объект Application на время операции
        /// </summary>
        public override void V4Dispose(bool redirect = false)
        {
            if (_cometUsers != null)
                _cometUsers.DisposeComet();

            base.V4Dispose(redirect);
        }

        /// <summary>
        ///     Отрисовка контрола для совместной работы
        /// </summary>
        protected void RenderCometControl(StringWriter w)
        {
            _cometUsers = new Comet(this)
            {
                ID = "v4_cometUsersList",
                HtmlID = "v4_cometUsersList"
            };

            V4Controls.Add(_cometUsers);
            _cometUsers.RenderControl(w);
        }

        /// <summary>
        ///     Добавление кнопок в меню
        /// </summary>
        /// <remarks>
        ///     В качестве параметра может получать:
        ///     одиночный объект,
        ///     объекты через запятую,
        ///     массив объектов Button
        /// </remarks>
        /// <param name="buttons">объект контрола button</param>
        public void AddMenuButton(params Button[] buttons)
        {
            _menuButtons.AddRange(buttons);
        }

        /// <summary>
        ///     Очистить коллекцию кнопок
        /// </summary>
        public void ClearMenuButtons()
        {
            _menuButtons.Clear();
        }

        /// <summary>
        ///     Сформировать кнопки меню
        /// </summary>
        public void RenderButtons(StringWriter w)
        {
            w.Write("<div id=\"pageHeader\" class=\"ui-widget-header ui-corner-all\" style=\"z-index:9999 {0}\">", _menuButtons.Count==0? ";height:23px":"");

            foreach (var b in _menuButtons)
            {
                V4Controls.Add(b);
                b.RenderControl(w);
                b.PropertyChanged.Clear();
            }

            if (_menuButtons.Count > 0)
            {
                if (ItemId > 0) // блокируем на вермя показа
                    RenderCometControl(w);
            }

            if (!string.IsNullOrEmpty(LogoImage))
            {
                w.Write("<img src=\"{0}\" style=\"float: left; margin-left: 2px; border: 0; height: 23px;\">", LogoImage);
            }

            if (!string.IsNullOrEmpty(HelpUrl))
            {
                var btnHelp = new Button
                {
                    ID = "btnHelp",
                    V4Page = this,
                    Text = "",
                    Title = Resx.GetString("lblHelp"),
                    Width = 27,
                    Height = 22,
                    IconJQueryUI = ButtonIconsEnum.Help,
                    OnClick = string.Format("v4_openHelp('{0}');", IDPage),
                    Style = "float: right; margin-right: 11px;"
                };

                V4Controls.Add(btnHelp);
                btnHelp.RenderControl(w);
                btnHelp.PropertyChanged.Clear();
            }

            if (LikeId != 0)
            {
                var btnLike = new LikeDislike
                {
                    ID = "btnLike",
                    V4Page = this,
                    LikeId = LikeId,
                    InterfaceVersion = InterfaceVersion,
                    Style = "float: right; margin-right: 11px; margin-top: 3px; cursor: pointer;"
                };

                V4Controls.Add(btnLike);
                btnLike.RenderControl(w);
            }

            w.WriteLine(@"</div>");
        }

        /// <summary>
        ///     Обновить кнопки меню
        /// </summary>
        public void RefreshMenuButtons()
        {
            using (var w = new StringWriter())
            {
                RenderButtons(w);
                JS.Write("gi('pageHeader').innerHTML={0};", HttpUtility.JavaScriptStringEncode(w.ToString(), true));
            }
        }

        /// <summary>
        ///     Возвращает разметку контрола label для отображения локальной даты
        /// </summary>
        /// <param name="dateTime">Дата и время для отображения</param>
        /// <param name="formatDateTime">Формат отображения даты</param>
        /// <param name="attributes">Атрибуты для отображаемого label</param>
        public string GetHtmlDateTimeLabel(DateTime dateTime, string formatDateTime = null, string attributes = null)
        {
            var formatedDateTime = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            return String.Format("<label class=localDT {0} {2}>{1}</label>",
                !String.IsNullOrEmpty(formatDateTime) ? String.Format("dtformat='{0}'", formatDateTime) : "",
                formatedDateTime, !String.IsNullOrEmpty(attributes) ? attributes : "");
        }

        /// <summary>
        ///     Возвращает разметку контрола label для отображения локальной даты
        /// </summary>
        /// <param name="dateTime">Дата и время для отображения</param>
        /// <param name="withoutTime">Отображать дату с/без даты</param>
        /// <param name="attributes">Атрибуты для отображаемого label</param>
        public string GetHtmlDateTimeLabel(DateTime dateTime, bool withoutTime, string attributes = null)
        {
            var formatedDateTime = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            return String.Format("<label class=localDT {0} {2}>{1}</label>", withoutTime ? "dtformat=dd.mm.yyyy" : "",
                formatedDateTime, !String.IsNullOrEmpty(attributes) ? attributes : "");
        }

        /// <summary>
        /// Переопределяемый метод присвоения идентификатора сущности
        /// </summary>
        /// <param name="id">Новый идентификатор сущности</param>
        /// <param name="command">Команда на присвоение</param>
        protected virtual void SetIdEntity(string id, string command)
        {
        }

        public override void ProcessRequest()
        {
            base.ProcessRequest();
            if (V4Request.Params.Count == 0)
            {
                return;
            }
            var key = V4Request.Params.Keys[1];
            switch (key)
            {
                case "setIdEntity":
                    SetIdEntity(V4Request.Params["setIdEntity"], V4Request.Params["command"]);
                    break;
            }
        }

        public virtual void CloseJQueryDialogForm(string idContainer)
        {
        }
    }
}