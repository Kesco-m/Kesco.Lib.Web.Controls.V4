using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.UI;
using System.Xml;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.BaseExtention.Enums;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.DALC;
using Kesco.Lib.Entities;
using Kesco.Lib.Entities.Corporate;
using Kesco.Lib.Entities.Documents;
using Kesco.Lib.Localization;
using Kesco.Lib.Log;
using Kesco.Lib.Web.Controls.V4.Common.DocumentPage;

using Kesco.Lib.Web.Controls.V4.Renderer;
using Kesco.Lib.Web.Settings;
using Kesco.Lib.Web.SignalR;

namespace Kesco.Lib.Web.Controls.V4.Common
{
    /// <summary>
    ///     Словарь контролов
    /// </summary>
    public class V4ControlsDict : Dictionary<string, V4Control>
    {
        /// <summary>
        ///     Метод добавления нового контрола в словарь
        /// </summary>
        /// <param name="value">контрол</param>
        public void Add(V4Control value)
        {
            if (!ContainsKey(value.HtmlID)) Add(value.HtmlID, value);
        }

        /// <summary>
        ///     Позволяет добавлять перечисление через запятую или массив
        /// </summary>
        /// <example>
        ///     V4Controls.AddRange(btnSave, btnClose, btnHelp, ...);
        /// </example>
        public void AddRange(params V4Control[] values)
        {
            foreach (var value in values)
                if (!ContainsKey(value.HtmlID))
                    Add(value.HtmlID, value);
            //else
            //{
            //    throw new ArgumentException("Контрол с HtmlID: " + value.HtmlID + " уже существует в коллекции, добавление не возможно.");
            //}
        }
    }

    public class V4PageObj
    {
        public Type Type { get; set; }
        public Entity Object { get; set; }
    }

    /// <summary>
    ///     Базовый класс станиц V4.
    ///     Все страницы, работающие с функционалом V4 наследуют от него.
    ///     Является абстрактным, т.к. содерщит свойства, которые обязан переопределить на странице
    /// </summary>
    public abstract class Page : System.Web.UI.Page
    {
        private string _itemName;

        /// <summary>
        ///     URL страницы
        /// </summary>
        protected string CurrentUrl { get; set; }

        /// <summary>
        ///     Делегат рендеринга части страницы
        /// </summary>
        /// <param name="w"></param>
        public delegate void RenderHtmlBlock(HtmlTextWriter w);

        /// <summary>
        ///     Делегат рендеринга части страницы с параметром
        /// </summary>
        /// <param name="w"></param>
        /// <param name="param"></param>
        public delegate void RenderHtmlBlockWithParam(TextWriter w, object param);

        /// <summary>
        ///     Параметры строки запроса, полученные при первом запросе GET
        /// </summary>
        protected NameValueCollection CurrentQS;

        /// <summary>
        ///     Коллекция атрибутов
        /// </summary>
        public NameValueCollection El = new NameValueCollection();

        /// <summary>
        ///     Коллекция XML атрибутов
        /// </summary>
        public NameValueCollection ElXML = new NameValueCollection();

        /// <summary>
        ///     Коллекция HTML блоков, используется для подготовки скрипта и обновления на клиенте.
        /// </summary>
        public NameValueCollection HTMLBlock = new NameValueCollection();

        public bool IsKescoRun = false;

        public bool IsSilverLight = true;

        public bool IsAbort = false;

        /// <summary>
        ///     Запись скриптов клиенту
        /// </summary>
        public TextWriter JS = new StringWriter();

        /// <summary>
        ///     Массив "слушателей", используется для пейджинга грида
        /// </summary>
        public ArrayList Listeners = new ArrayList();

        /// <summary>
        ///     Коллекция кнопок меню
        /// </summary>
        protected internal List<Button> MenuButtons;

        /// <summary>
        ///     Получение единственного экземпляра PageEventManager
        /// </summary>
        public PageEventManager PageEventManager = PageEventManager.GetInstatnce();

        /// <summary>
        ///     Локализация
        /// </summary>
        public ResourceManager Resx = Resources.Resx;
                

        /// <summary>
        ///     Словарь контролов
        /// </summary>
        public V4ControlsDict V4Controls = new V4ControlsDict();

        /// <summary>
        ///     Конструктор
        /// </summary>
        protected Page()
        {
            Data = new Dictionary<string, object>();
            JsScripts = new Dictionary<string, string>();
            CssScripts = new List<string>();
            LastUpdate = DateTime.Now;
            CreateTime = DateTime.Now;
            EnableViewState = false;
            CurrentUser = new Employee(true);
            MenuButtons = new List<Button>();
            IsSignal = true;
        }

        /// <summary>
        ///     Родительская страница для которой текущая открыта в модальном режиме
        /// </summary>
        public virtual Page ParentPage { get; set; }

        /// <summary>
        ///     Признак постбека
        /// </summary>
        public bool V4IsPostBack { get; set; }

        /// <summary>
        ///     ID вызвавшего источника, берется из строки адреса
        /// </summary>
        public string ReturnId { get; set; }

        /// <summary>
        ///     ID клиента
        /// </summary>
        public int ClId { get; set; }

        /// <summary>
        ///     ID сущности
        /// </summary>
        public int ItemId { get; set; }

        /// <summary>
        ///     ID родительского grid
        /// </summary>
        public string ParentGridId { get; set; }
        public string ParentTreeViewId { get; set; }

        /// <summary>
        ///     ID родительского окна
        /// </summary>
        public string ParentPageId { get; set; }
        
        /// <summary>
        ///     ID диалога
        /// </summary>
        public string DialogId { get; set; }

        /// <summary>
        ///     Название формы сущности
        /// </summary>
        public string ItemName
        {
            get
            {
                if (_itemName == null)
                {
                    if (string.IsNullOrEmpty(AppRelativeVirtualPath)) return string.Empty;
                    var match = Regex.Match(AppRelativeVirtualPath, RegexPattern.FileName,
                        RegexOptions.IgnoreCase);
                    _itemName = match.Success ? match.Value : AppRelativeVirtualPath;
                }
                return _itemName;
            }
            set { _itemName = value; }
        }

        /// <summary>
        /// Название сущности
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        ///     Признак возможности редактирования сущности
        /// </summary>
        public bool IsEditable { get; set; }

        //public delegate void ChangedEventHandler();
        //public event ChangedEventHandler Changed;

        /// <summary>
        ///     ID страницы (GUID)
        /// </summary>
        public string IDPage { get; set; }


        /// <summary>
        ///     ID post request (GUID)
        /// </summary>
        public string IDPostRequest { get; set; }


        /// <summary>
        ///     Дата и время последнего обращения к странице
        /// </summary>
        public DateTime LastUpdate { get; set; }

        /// <summary>
        ///     ID контрола на который необходимо установить фокус
        /// </summary>
        public string FocusControl { get; set; }

        /// <summary>
        ///     Дополнительная клиентская команда (для выполнения нескольких команд в одном запросе)
        /// </summary>
        public string ExternalCmd { get; set; }

        /// <summary>
        ///     Признак запроса типа POST
        /// </summary>
        public bool PostRequest { get; set; }

        /// <summary>
        ///     ID пользователя
        /// </summary>
        public object UserId { get; set; }

        /// <summary>
        ///     Пользователь
        /// </summary>
        public Employee CurrentUser { get; set; }

        /// <summary>
        ///     русская локализация клиента
        /// </summary>
        public bool IsRusLocal => CurrentUser.Language == "ru";

        /// <summary>
        ///     английская локализация клиента
        /// </summary>
        public bool IsEngLocal => CurrentUser.Language == "en";

        /// <summary>
        ///     эстонская локализация клиента
        /// </summary>
        public bool IsEstLocal => CurrentUser.Language == "et";

        /// <summary>
        ///     Признак работы лонгпулинга
        /// </summary>
        public bool IsSignal { get; set; }

        /// <summary>
        ///     Тайтл страницы
        /// </summary>
        public new string Title
        {
            get { return base.Title; }
            set
            {
                base.Title = value;
                //JS.Write("alert('Не реализована передача на клиента');");
            }
        }

        /// <summary>
        ///     Коллекция данных
        /// </summary>
        public Dictionary<string, object> Data { get; }

        /// <summary>
        ///     Установка/снятие для всех контролов на странице свойства "отключено"
        /// </summary>
        public bool ControlsIsDisabled
        {
            set
            {
                if (value != ControlsIsDisabled)
                    foreach (var c in V4Controls.Values)
                        if (c.IsDisabled != value)
                        {
                            c.SetPropertyChanged("IsDisabled");
                            c.IsDisabled = value;
                        }

                El["d"] = value ? "1" : "0";
            }
            get { return El["d"] == "1"; }
        }

        /// <summary>
        ///     Установка/снятие для всех контролов на странице свойства "только чтение"
        /// </summary>
        public bool IsReadOnly
        {
            set
            {
                if (value != IsReadOnly)
                    foreach (var c in V4Controls.Values)
                        if (c.IsReadOnly != value)
                        {
                            c.SetPropertyChanged("IsReadOnly");
                            c.IsReadOnly = value;
                        }

                El["r"] = value ? "1" : "0";
            }
            get { return El["r"] == "1"; }
        }

        /// <summary>
        ///     Отключаем/Включаем ViewState (в текущей реализации ViewState отключен)
        /// </summary>
        public sealed override bool EnableViewState
        {
            get { return base.EnableViewState; }
            set { base.EnableViewState = value; }
        }

        ///// <summary>
        ///// Отрисовка заголовка страницы
        ///// </summary>
        ///// <returns>Возвращает поток текстового вывода</returns>
        //public HtmlTextWriter RenderHeader()
        //{
        //    var w = new HtmlTextWriter(new StringWriter());
        //    w.Write("<xml><f /></xml>");
        //    return w;
        //}

        /// <summary>
        ///     Коллекция скриптов
        /// </summary>
        public Dictionary<string, string> JsScripts { get; }

        /// <summary>
        ///     Коллекция стилей
        /// </summary>
        public List<string> CssScripts { get; }

        /// <summary>
        ///     Запрос
        /// </summary>
        public HttpRequest V4Request { get; set; }

        /// <summary>
        ///     Ответ
        /// </summary>
        public HttpResponse V4Response { get; set; }

        /// <summary>
        /// Время создания объекта Page
        /// </summary>
        public DateTime CreateTime { get; set; }

                
        /// <summary>
        ///     Ссылка на логотип страницы
        /// </summary>
        public string LogoImage { get; set; }

        /// <summary>
        ///     Абстрактное свойство, требующее обязательно переопределения на странице и устанавливающее ссылку на справку
        /// </summary>
        public abstract string HelpUrl { get; set; }

        /// <summary>
        ///     Идентификатор оценки интерфейса
        /// </summary>
        public string LikeId
        {
            get
            {
                var section = ConfigurationManager.GetSection("likeSettings");
                if (section == null) return string.Empty;
                var likes = (LikeItem[])section;
                if (likes.Length == 0) return string.Empty;
                var lk = likes.FirstOrDefault(x => x.FormName.Equals(ItemName, StringComparison.InvariantCultureIgnoreCase));
                return lk == null ? string.Empty : lk.LikeId;
            }
        }

        private List<V4PageObj> objList { get; set; }

        private List<V4PageObj> ObjList
        {
            get
            {
                if (objList != null) return objList;

                objList = new List<V4PageObj>();
                return objList;
            }
        }

        /// <summary>
        ///     Метод переадресации текущей страницы по условию
        /// </summary>
        protected virtual bool RedirectPageByCondition()
        {
            return false;
        }

        /// <summary>
        ///     Освобождение ресурсов занятых страницей. Блокирует объект Application на время операции
        /// </summary>
        public virtual void V4Dispose(bool redirect = false)
        {
            KescoHub.RemovePage(IDPage);
            var info = $"{DateTime.Now:dd.MM.yy HH:mm:ss} -> Страница [{IDPage}] штатно удалена из KescoHub";
            KescoHub.RefreshSignalViewInfo(new KescoHubTraceInfo { TraceInfo = info });
        }

        private bool IsBasicAuth()
        {
            var context = HttpContext.Current;
            if (context.Request == null) return false;
            if (context.Request.UserHostAddress == null) return false;

            var localAddr = context.Request.ServerVariables["LOCAL_ADDR"];
            if (localAddr == null) return false;

            if (context.Request.UserHostAddress.Equals(localAddr)) return false;

            var auth = context.Request.ServerVariables["HTTP_AUTHORIZATION"];

            if (auth != null && auth.Length > 0 && auth.Length > 11 &&
                !(auth.Substring(0, 6).Equals("Basic ") || auth.Substring(0, 11).Equals("Negotiate Y")))
                return true;

            return false;
        }

        /// <summary>
        ///     Отправка клиенту скрипта с изменениями контролов
        /// </summary>
        public void Flush()
        {

            RestoreCursor();

            var docXml = BuildXml();

            V4Response.ClearContent();

            if (docXml.InnerXml.Length > 0)
            {
                V4Response.ContentType = "text/xml";
                V4Response.Write(docXml.InnerXml);
            }

            //здесь только можно очистить свойство
            FocusControl = string.Empty;

            // освобождаем ресурсы от предыдущего объекта(иначе возможна утечка памяти)
            JS.Close();
            JS.Dispose();

            JS = new StringWriter();
        }

        /// <summary>
        ///     Добавление необходимых вывозов после формирования контента для передачи в поток
        /// </summary>
        private void SetScriptsAfterContent()
        {
            if (!string.IsNullOrEmpty(FocusControl))
                JS.Write("setTimeout(function () {var objF=gi('" + FocusControl + "'); if (objF) objF.focus(); $('#" + FocusControl + "').select();}, 10);");

            JS.Write("v4_setToolTip(); v4_setLocalDateTime();");
        }

        /// <summary>
        ///     Функция закрытия окна
        /// </summary>
        public void V4DropWindow()
        {
            if (ParentPage != null)
            {
                if (!ParentGridId.IsNullEmptyOrZero() && !DialogId.IsNullEmptyOrZero())
                {
                    if (ParentPage.V4Controls != null)
                    {
                        var objGrid = ParentPage.V4Controls[ParentGridId] as Grid.Grid;
                        objGrid?.CloseDialogForm(DialogId, IDPage);
                        objGrid?.RefreshGrid();
                    }
                }

                if (!ParentTreeViewId.IsNullEmptyOrZero())
                {
                    if (ParentPage.V4Controls != null)
                    {
                        var objTreeView = ParentPage.V4Controls[ParentTreeViewId] as TreeView.TreeView;
                        objTreeView?.CloseDialogForm(DialogId, IDPage);
                        //objTreeView?.RefreshTreeView();
                    }
                }

            }
            else
            {
                PageEventManager.SendEvent(this, "Close", null);
                JS.Write("v4_dropWindow();");
            }
        }

        /// <summary>
        ///     Обновление страницы с повторной загрузкой данных из БД
        /// </summary>
        /// <returns></returns>
        protected void RefreshPage()
        {
            if (this is EntityPage)
                TranslatePageEvent(new NameValueCollection { { "cmd", "RefreshForce" } });
            V4Navigate(CurrentUrl);
        }

        /// <summary>
        ///     Обновление Grid на родительской форме
        /// </summary>
        protected void RefreshParentPageGrid()
        {
            if (!ParentGridId.IsNullEmptyOrZero() && !DialogId.IsNullEmptyOrZero())
            {
                if (ParentPage.V4Controls != null)
                {
                    var objGrid = ParentPage.V4Controls[ParentGridId] as Grid.Grid;
                    objGrid?.RefreshGrid();
                }
            }
        }

        protected void SetCurrentUrlParams(Dictionary<string, object> parameters)
        {
            if (parameters.Count == 0) return;
            var ub = new UriBuilder(CurrentUrl);
            var qs = HttpUtility.ParseQueryString(ub.Query);
            foreach (var p in parameters)
                qs.Set(p.Key, p.Value.ToString());

            ub.Query = string.Join("&",
                qs.AllKeys.Select(a => a + "=" + HttpUtility.UrlEncode(qs[a])));
            CurrentUrl = ub.Uri.AbsoluteUri;
        }

        public void ShowMessageOnPage(string htmlMessage)
        {
            var message =
                string.Format(
                    "$(\"body\").prepend(\"<div class='v4Div-outer-container' id='v4DivErrorOuter'></div>\"); " +
                    "$(\"#v4DivErrorOuter\").append(\"<div class='v4Div-inner-container' id='v4DivErrorInner'></div>\");" +
                    "$(\"#v4DivErrorInner\").append(\"<div class='v4Div-centered-content' style='width:500px;' id='v4DivErrorContent'>" +
                    "{0}" +
                    "</div>\");", HttpUtility.JavaScriptStringEncode(htmlMessage));
            JS.Write(message);
        }

        public virtual void EditingStatusChanged()
        {
        }

        /// <summary>
        ///     Отрисовка страницы
        /// </summary>
        /// <param name="writer">Поток</param>
        protected override void Render(HtmlTextWriter writer)
        {
            if (!V4IsPostBack)
            {
                if (IsSignal)
                {
                    FocusControl = "";

                    // освобождаем ресурсы от предыдущего объекта(иначе возможна утечка памяти)
                    JS.Close();
                    JS.Dispose();

                    JS = new StringWriter();
                }

                base.Render(writer);
            }
        }

        /// <summary>
        ///     Добавление контролов в коллекцию
        /// </summary>
        /// <param name="root">корневой контрол</param>
        private void GetV4Controls(Control root)
        {
            if (root.HasControls())
                foreach (Control control in root.Controls)
                {
                    GetV4Controls(control);
                    var v4Control = control as V4Control;
                    if (v4Control != null && !string.IsNullOrEmpty(v4Control.HtmlID) &&
                        !V4Controls.Keys.Contains(v4Control.HtmlID))
                    {
                        var ctrl = v4Control;
                        V4Controls.Add(ctrl);
                    }
                }
        }

        /// <summary>
        ///     Регистрация скриптов на странице
        /// </summary>
        /// <param name="key">Ключ скрипта</param>
        /// <param name="url">Файл скрипта</param>
        /// <param name="fromStyles">Файл из стилей</param>
        public void RegisterScript(string key, string url, bool fromStyles, bool withDomain = false)
        {
            url += (url.Contains('?') ? "&" : "?") + "v=" + Config.styles_cache + (withDomain ? $"&domain={HttpUtility.UrlEncode(Config.domain)}" : "");
            var stylesPath = Config.styles_js;
            if (!fromStyles) {stylesPath = ""; }
            var script = $"<script src='{stylesPath}{url}' type='text/javascript'></script>";

            JsScripts[key] = script;
        }

        /// <summary>
        ///     Регистрация CSS
        /// </summary>
        /// <param name="url">URL к файлу CSS-стилей</param>
        /// <param name="fromStyles">URL к файлу CSS-стилей</param>
        public void RegisterCss(string url, bool fromStyles)
        {
            url += (url.Contains('?') ? "&" : "?") + "v=" + Config.styles_cache;
            var stylesPath = Config.styles_css;
            if (!fromStyles)
                stylesPath = ""; 
            if (!CssScripts.Contains(url)) CssScripts.Add($"{stylesPath}{url}");
        }

        /// <summary>
        ///     Обработчик события инициализации страницы
        /// </summary>
        /// <param name="e">Параметр события</param>
        protected override void OnInit(EventArgs e)
        {
            CurrentUrl = Request.Url.AbsoluteUri;
            var section = ConfigurationManager.GetSection("applicationParams");
            if (section != null)
            {
                var parameters = (ParamItem[])section;
                if (parameters.Length != 0)
                {
                    foreach (var param in parameters)
                    {
                        var sqlParams = new Dictionary<string, object>
                        {
                            {"@Параметр", param.Name},
                            {"@АктуальноеВремя", Convert.ToDateTime(param.ActualTime).ToUniversalTime()}
                        };
                        DBManager.ExecuteNonQuery(SQLQueries.UPDATE_Настройки, CommandType.Text, Config.DS_user, sqlParams);
                    }
                }
            }

            Response.Cache.SetNoStore();
            ReturnId = string.IsNullOrEmpty(Request.QueryString["return"]) ? "" : Request.QueryString["return"];
            PostRequest = Request.HttpMethod.Equals("POST");

            if (!string.IsNullOrEmpty(Request.QueryString["clid"]))
            {
                int clid;
                if (int.TryParse(Request.QueryString["clid"], out clid)) ClId = clid;
            }

            if (!PostRequest)
            {
                ItemId = Request["ID"].ToInt();

                if (!string.IsNullOrEmpty(Request.QueryString["title"])) Title = Request.QueryString["title"];

                CurrentQS = Request.QueryString;
            }

            if (!string.IsNullOrEmpty(Request["idpp"]))
            {
                ParentPageId = Request["idpp"];
            }

            if (!string.IsNullOrEmpty(Request["gridId"]))
            {
                ParentGridId = Request["gridId"];
            }

            if (!string.IsNullOrEmpty(Request["treeViewId"]))
            {
                ParentTreeViewId = Request["treeViewId"];
            }

            if (!string.IsNullOrEmpty(Request["dialogid"]))
            {
                DialogId = Request["dialogid"];
            }

            var page = KescoHub.GetPage(ParentPageId);

            if (page is DocPage)
                ParentPage = (DocPage) page;
            else if (page is EntityPage)
                ParentPage = (EntityPage)page;
            else
                ParentPage = (Page)page;

            base.OnInit(e);

            InitMenuButton();
        }

        private void RegisterScript()
        {
            var appRoot = GetWebAppRoot();
            RegisterCss($"jquery.qtip.min.css", true);
            RegisterCss($"jquery-ui.css", true);
            RegisterCss($"Kesco.css", true);

            RegisterScript("jquery","jquery-1.12.4.min.js", true);
            RegisterScript("jqueryui","jquery-ui.js", true);
            RegisterScript("dialogextend","jquery.dialogextend.min.js", true);
            RegisterScript("jquerycookie","jquery.cookie.js", true);
            RegisterScript("kesco", "kesco.js", true, true);
            RegisterScript("jqueryqtipmin","jquery.qtip.min.js",true);
            RegisterScript("jqueryvalidate","jquery.validate.min.js", true);
            RegisterScript("jquerymask","jquery.ui.mask.js", true);
            


            RegisterScript("v4","kesco.v4.js", true);

            if (!IsKescoRun && IsSilverLight)
            {
                RegisterScript("kesco.silver4js","kesco.silver4js.js", true);
                RegisterScript("silverlight","silverlight.js", true);
            }

            RegisterScript("SignalR","jquery.signalR-2.4.1.min.js", true);
            RegisterScript("Hub",$"{appRoot}/signalr/hubs", false);
            RegisterScript("kescosignalr","kesco.signalr.js", true);
                      
            RegisterScript("kescoqtip","Kesco.qtip.js", true);

            if (V4Controls.Values.Any(c => c.GetType().Name.Contains("DatePicker")) ||
                V4Controls.Values.Any(c => c.GetType().Name.Contains("PeriodTimePicker")) ||
                V4Controls.Values.Any(c => c.GetType().Name.Contains("Grid")) ||
                V4Controls.Values.Any(c => c.GetType().Name.Contains("TreeView"))
                )
                RegisterScript("datepicker","kesco.datepicker.js", true);

            if (V4Controls.Values.Any(c => c.GetType().Name.Contains("Grid")))
            {
                RegisterScript("gridfloatthead","jquery.floatThead.min.js", true);                
                RegisterScript("grid","kesco.grid.js", true);
            }

            if (V4Controls.Values.Any(c => c.GetType().Name.Contains("Menu")))
                RegisterScript("menu","kesco.menu.js", true);

            if (V4Controls.Values.Any(c => c.GetType().Name.Contains("TreeView")))
            {
                RegisterCss($"jquery-jstree.css", true);
                RegisterScript("treeview","kesco.treeView.js", true);
                RegisterScript("jstree","jstree.js", true);
            }

            RegisterScript("confirm", "Kesco.Confirm.js", true);
            RegisterScript("dialog","kesco.dialog.js", true);
            

            if (this is DocPage)
                RegisterScript("docpage","kesco.docpage.js", true);
        }

        public static string GetWebRoot()
        {
            string host = (HttpContext.Current.Request.Url.IsDefaultPort) ?
                HttpContext.Current.Request.Url.Host :
                HttpContext.Current.Request.Url.Authority;
            host = $"{HttpContext.Current.Request.Url.Scheme}://{host}";
            return host;
        }

        public static string GetWebAppRoot()
        {
            string host = (HttpContext.Current.Request.Url.IsDefaultPort) ?
                HttpContext.Current.Request.Url.Host :
                HttpContext.Current.Request.Url.Authority;
            host = $"{HttpContext.Current.Request.Url.Scheme}://{host}";
            if (HttpContext.Current.Request.ApplicationPath == "/")
                return host;
            else
                return host + HttpContext.Current.Request.ApplicationPath;
        }

        /// <summary>
        ///     Обработчик события загрузки страницы
        /// </summary>
        /// <param name="e">Параметр события</param>
        protected override void OnLoad(EventArgs e)
        {
            if (V4IsPostBack) return;
            GetV4Controls(this);

            RegisterScript();

            base.OnLoad(e);
                       

            if (!IsSignal)
            {
                Header.Controls.AddAt(0,
                    new LiteralControl(Environment.NewLine +
                                       "<script type='text/javascript'>v4_isSignal = false;</script>" +
                                       Environment.NewLine));
            }
            else
            {
                var script_index = 0;

                Header.Controls.AddAt(script_index++,
                    new LiteralControl(Environment.NewLine + @"<base target=""_self"">"));

                var sbCss = new StringBuilder();
                foreach (var s in CssScripts)
                {
                    sbCss.Append(Environment.NewLine);
                    sbCss.AppendFormat("<link href='{0}' rel='stylesheet' type='text/css'/>",
                        HttpUtility.JavaScriptStringEncode(s));
                }

                sbCss.Append(Environment.NewLine);
                Header.Controls.AddAt(script_index++, new LiteralControl(sbCss.ToString()));

                var sbScripts = new StringBuilder();
                foreach (var s in JsScripts)
                {
                    sbScripts.Append(s.Value);
                    sbScripts.Append(Environment.NewLine);
                }

                sbScripts.Append("<script type='text/javascript'>");
                sbScripts.Append(Environment.NewLine);
                sbScripts.AppendFormat("var v4_domain ='{0}';", Config.domain);
                sbScripts.Append(Environment.NewLine);
                sbScripts.AppendFormat("var kesco_ip = '{0}';", Request.ServerVariables["REMOTE_ADDR"]);
                sbScripts.Append(Environment.NewLine);
                sbScripts.Append("var v4_isChanged = false;");
                sbScripts.Append(Environment.NewLine);
                sbScripts.Append("var v4_isValidate = false;");
                sbScripts.Append(Environment.NewLine);
                sbScripts.AppendFormat("var v4_isEditable = {0};", IsEditable ? "true" : "false");
                sbScripts.Append(Environment.NewLine);
                sbScripts.AppendFormat("var v4_ItemId = '{0}';", ItemId);
                sbScripts.Append(Environment.NewLine);
                sbScripts.AppendFormat("var v4_ItemName = '{0}';", ItemName);
                sbScripts.Append(Environment.NewLine);
                sbScripts.AppendFormat("var v4_EntityName = '{0}';", EntityName);
                sbScripts.Append(Environment.NewLine);
                sbScripts.AppendFormat("var v4_userId = '{0}';", CurrentUser.Id);
                sbScripts.Append(Environment.NewLine);
                sbScripts.AppendFormat("var v4_userName = '{0}';", CurrentUser.FullName);
                sbScripts.Append(Environment.NewLine);
                sbScripts.AppendFormat("var v4_userNameLat = '{0}';", CurrentUser.FullNameEn);
                sbScripts.Append(Environment.NewLine);
                sbScripts.AppendFormat("var v4_userLang = '{0}';", CurrentUser.Language);
                sbScripts.Append(Environment.NewLine);
                sbScripts.AppendFormat("var v4_userForm = '{0}';", Config.user_form);
                sbScripts.Append(Environment.NewLine);
                sbScripts.AppendFormat("try {{Kesco.globals.settingsFormLocation = '{0}';}} catch(e){{}}", Config.settings_form_location);
                sbScripts.Append(Environment.NewLine);
                sbScripts.AppendFormat("try {{Kesco.globals.settingsFormLocationAdv = '{0}';}} catch(e){{}}", Config.settings_form_location_adv);
                sbScripts.Append(Environment.NewLine);
                sbScripts.AppendFormat("try {{Kesco.globals.version = '{0}';}} catch(e){{}}", Assembly.GetExecutingAssembly().GetName().Version.ToString());
                sbScripts.Append(Environment.NewLine);

                sbScripts.Append(Environment.NewLine);

                sbScripts.Append(Environment.NewLine);
                sbScripts.Append("</script>");
                sbScripts.Append(Environment.NewLine);

                //Эти скрипты добавляются в начало заголовка
                Header.Controls.AddAt(script_index++, new LiteralControl(sbScripts.ToString()));

                var sb = new StringBuilder(Environment.NewLine + "<script type='text/javascript'>");
                sb.Append(Environment.NewLine);
                sb.AppendFormat("var idp='{0}'; ", HttpUtility.JavaScriptStringEncode(IDPage));
                sb.Append(Environment.NewLine);

                if (!string.IsNullOrEmpty(FocusControl))
                {
                    sb.AppendFormat("setTimeout(function () {{ var objF=gi('{0}'); if (objF) objF.focus();  $('#{0}').select();}}, 100 );",
                        FocusControl);
                    sb.Append(Environment.NewLine);
                }

                sb.Append(Environment.NewLine);
                sb.Append("function v4_tooltipCaller() {");                                         sb.Append(Environment.NewLine);
                sb.Append("var dataId=$(this)[0].getAttribute('data-id');");                        sb.Append(Environment.NewLine);
                sb.Append("var callerType=$(this)[0].getAttribute('caller-type');");                sb.Append(Environment.NewLine);
                sb.Append("if(dataId==null || dataId=='') return '';");                             sb.Append(Environment.NewLine);
                sb.Append("if(callerType==null) callerType='';");                                   sb.Append(Environment.NewLine);
                sb.AppendFormat(
                    "return '{0}?cid=' + dataId + '&ctype=' + callerType + '&computerName=' + v4_clientName;",
                    Config.contacts);                                                               sb.Append(Environment.NewLine);
                sb.Append("}");                                                                     sb.Append(Environment.NewLine);


                sb.Append("function v4_startup(){");
                sb.Append(Environment.NewLine);
                sb.Append(JS);
                sb.Append("}");
                sb.Append(Environment.NewLine);
                sb.Append(
                    "v4_addLoadEvent(v4_startup); v4_addLoadEvent(v4_setLocalDateTime); v4_addLoadEvent(v4_setToolTip);");
                sb.Append(Environment.NewLine);
                sb.AppendFormat("v4_helpURL = '{0}';", HttpUtility.JavaScriptStringEncode(HelpUrl));
                sb.Append(Environment.NewLine);

                //Скрипты для кортролов select
                sb.AppendFormat(@"
                $(document).ready(function () {{

                $('.button_disabled').prop('disabled', true).addClass('ui-state-disabled');

                $('.v4si').bind({{
                    blur: function(event) {{
                        v4s_onBlur(event);
                    }},
                    keydown: function(event) {{
                        return v4s_keyDown(event);
                    }},
                    keyup: function(event) {{
                        return v4s_keyUp(event);
                    }}

                }});

                $('.v4sd').bind({{
                    selectstart: function(event) {{ return false }},
                    mousedown: function(event) {{ return false }}
                }});

                }});
                ");
                sb.Append(Environment.NewLine);

                sb.Append("</script>" + Environment.NewLine);

                //Эти скрипты добавляются в конец заголовка
                Header.Controls.Add(new LiteralControl(sb.ToString()));
            }

            var sbLang = new StringBuilder(Environment.NewLine + "<script type='text/javascript'>");
            sbLang.AppendFormat(@"page_clientLocalization = {{
                close_button:""{0}"",
                minimize_button:""{1}"",
                maximize_button:""{2}"",
                collapse_button:""{3}"", 
                restore_button:""{4}"" 
            }};",
                Resx.GetString("BtnClose"),
                Resx.GetString("btnMinimize"),
                Resx.GetString("btnMaximize"),
                Resx.GetString("btnCollapse"),
                Resx.GetString("btnRestore")
            );

            sbLang.Append(Environment.NewLine);
            sbLang.Append("</script>" + Environment.NewLine);
            Header.Controls.Add(new LiteralControl(sbLang.ToString()));
        }

        /// <summary>
        ///     Обработчик события перед закрытием страницы
        /// </summary>
        public virtual bool OnBeforeClose()
        {
            return true;
        }

        /// <summary>
        ///     Обработчик запроса к странице
        /// </summary>
        public virtual void ProcessRequest()
        {
            LastUpdate = DateTime.Now;
            if (!string.IsNullOrEmpty(ExternalCmd))
            {
                ProcessCommand(ExternalCmd, null);
                ExternalCmd = "";
            }

            if (V4Request.Params.Count == 0) return;
            var key = V4Request.Params.Keys[1];
            switch (key)
            {
                case "ctrl":
                    if (!V4Controls.ContainsKey(V4Request.Params["ctrl"]))
                        throw new Exception("Control is not found id:" + V4Request.Params["ctrl"] + " Page:" +
                                            V4Request.RawUrl);

                    V4Controls[V4Request.Params["ctrl"]].ProcessCommand(V4Request.Params);
                    var translateCtrlEvent = !(!string.IsNullOrEmpty(V4Request.Params["cmd"]) &&
                                               V4Request.Params["cmd"].Contains("popup"));
                    if ((ItemId != 0 || ItemId == 0 && V4Request.Params["ctrl"] == "btnLike") && translateCtrlEvent /* && V4Request.Params["ctrl"] != "CorrectableTtn"*/)
                        TranslateCtrlEvent(V4Request.Params);
                    break;

                case "page":
                    switch (V4Request.Params["page"])
                    {
                        case "close":
                            if (OnBeforeClose())
                            {
                                var nvc = new NameValueCollection { { "idp", IDPage } };
                                ProcessCommand("PageClose", nvc);
                                JS.Write("v4_closeWindow();");
                            }

                            break;
                        case "clientname":
                            JS.Write(DialerInterop.GetClientName(HttpContext.Current, IDPage));
                            break;
                    }

                    break;
                case "cmd":
                    ProcessCommand(V4Request.Params["cmd"], V4Request.Params);
                    break;
            }

            JS.Write("v4_isValidate={0};", V4ValidationBeforeExit().ToString(CultureInfo.InvariantCulture).ToLower());
        }


        /// <summary>
        ///     Процедура обработки клиентских запросов, вызывается с клиента либо синхронно, либо асинхронно
        /// </summary>
        /// <param name="cmd">Название команды</param>
        /// <param name="param">Коллекция параметров</param>
        protected virtual void ProcessCommand(string cmd, NameValueCollection param)
        {
            try
            {
               
                switch (cmd)
                {
                    case "Listener":
                        var listener =
                            (IClientCommandProcessor)
                            Listeners[int.Parse(param["ctrlId"].ToString(CultureInfo.InvariantCulture))];
                        listener.ProcessClientCommand(param);
                        break;
                    case "Refresh":
                        //var id = param["Ctrl"];
                        //if (!String.IsNullOrEmpty(id) && V4Controls.Keys.Contains(id))
                        //    RefreshHtmlBlock(id, V4Controls[id].RenderControl);
                        V4Navigate(Request.Url.PathAndQuery);
                        break;
                    case "PageClose":
                        V4Dispose();
                        break;
                    case "Help":
                        RenderOpenHelpPage();
                        break;
                    case "getResultFromPopUp":
                        DialogResult(param["dialogResult"]);
                        break;
                    // Показать текущий документ в Архиве документов
                    case "ShowInDocView":
                        if (IsKescoRun)
                            ShowDocumentInDocview(param["DocId"], false, param["openImage"] == "1");
                        else
                            OpenDoc(param["DocId"], false, param["openImage"] == "1");
                        break;
                }
            }
            catch (Exception ex)
            {
                var dex = new DetailedException(ex.Message, ex);
                Logger.WriteEx(dex);

                throw dex;
            }
        }

        /// <summary>
        ///     Базовая проверка заполненности обязательных полей на форме
        /// </summary>
        /// <returns>
        ///     Истина - проверка прошла успешно; false - не все обязательные поля заполнены и выводит сообщение о
        ///     необходимости заполнить поля
        /// </returns>
        public virtual bool V4Validation()
        {
            foreach (var ctrl in V4Controls.Values)
            {
                if (!ctrl.IsReadOnly && ctrl.IsRequired && ctrl.Value.Length == 0)
                {
                    var name = Thread.CurrentThread.CurrentCulture.Name;
                    ShowMessage(name.ToLower().StartsWith("ru")
                        ? "Пожалуйста заполните поля выделенные жёлтым цветом."
                        : "Please fill out yellow fields.");
                    return false;
                }

                if (!ctrl.Validation()) return false;
            }

            return true;
        }

        /// <summary>
        ///     Перейти на URL
        /// </summary>
        /// <remarks>
        ///     создает ссылку и кликает по ней(сделано из за того, что location.reload() отрабатывает по разному в разных
        ///     браузерах)
        /// </remarks>
        public void V4Navigate(string url)
        {
            V4Dispose();
            JS.Write(WebExtention.DynamicLink(url, false));
        }

        /// <summary>
        ///     Установить фокус на контрол по его ID
        /// </summary>
        /// <param name="controlId">HtmlID контрола</param>
        public virtual void V4SetFocus(string controlId)
        {
            if (!V4Controls.ContainsKey(controlId)) return;
            var control = V4Controls[controlId];
            control.Focus();
        }

        /// <summary>
        ///     Проверка на наличие нотифиций в статусе ошибка у полей на форме
        /// </summary>
        /// <returns>Истина - нотификаций в статусе ошибка нет; false - есть поля с нотификациями в статусе ошибка</returns>
        public virtual bool V4NtfValidation()
        {
            return V4Controls.Values.All(ctrl => !ctrl.NtfValid);
        }

        /// <summary>
        ///     Рендеринг надписей валидации
        /// </summary>
        /// <param name="w">Исходный поток для записи HTML - кода</param>
        /// <param name="ntf">Список надписей</param>
        public void RenderNtf(TextWriter w, List<Notification> ntf)
        {
            ntf.ForEach(n =>
            {
                var className = EnumAccessors.GetCssClassByNtfStatus(n.Status, n.SizeIsNtf);
                if (!string.IsNullOrEmpty(n.Description) && n.Status != NtfStatus.Empty)
                    className += " v4ContextHelp";
                if (!string.IsNullOrEmpty(n.CSSClass))
                    className += $" {n.CSSClass}";
                var dashSpace = n.DashSpace ? "- " : "";

                w.Write("<div {0} {1} {2}>",
                    string.IsNullOrEmpty(n.ContainerId) ? "" : $"id=\"{n.ContainerId}\"",
                    string.IsNullOrEmpty(className) ? "" : $"class=\"{className}\"",
                    string.IsNullOrEmpty(n.Description) || n.Status == NtfStatus.Empty
                        ? ""
                        : $"title=\"{HttpUtility.HtmlEncode(n.Description)}\""
                );

                if (n.Message.IndexOf("<ns>", StringComparison.Ordinal) != -1)
                {
                    var n4check = n.Message.Remove(0, 4);
                    n4check = dashSpace + n4check.Replace("<ns>", dashSpace);
                    w.Write(n4check);
                    return;
                }

                w.Write(dashSpace);

                w.Write(n.Message);
                w.Write("</div>");
            }
            );
        }

        /// <summary>
        ///     Рендеринг списка надписей в одну сроку
        /// </summary>
        /// <param name="w">Поток вывода</param>
        /// <param name="ntfs">список надписей</param>
        /// <param name="separator">разделитель</param>
        /// <param name="renderWithNewLine">начинать вывод с новой строки</param>
        public void RenderNtfInline(TextWriter w, List<Notification> ntfs, string separator, bool renderWithNewLine)
        {
            if (ntfs == null) return;
            var inx = 1;
            ntfs.ForEach(n =>
            {
                var className = EnumAccessors.GetCssClassByNtfStatus(n.Status, n.SizeIsNtf);
                if (!string.IsNullOrEmpty(n.Description) && n.Status != NtfStatus.Empty) className += " v4ContextHelp";
                w.Write(
                    "{0}<span {1} {2}>{3}</span>{4}",
                    renderWithNewLine && inx == 1 ? "<br/>" : "",
                    string.IsNullOrEmpty(className) ? "" : $"class=\"{className}\"",
                    string.IsNullOrEmpty(n.Description) || n.Status == NtfStatus.Empty
                        ? ""
                        : $"title=\"{HttpUtility.HtmlEncode(n.Description)}\"",
                    n.Message,
                    inx < ntfs.Count ? separator + " " : ""
                );
                inx++;
            });
        }

        /// <summary>
        ///     Обновить нотификацию у всех контролов
        /// </summary>
        public void RefreshNtf()
        {
            foreach (var c in V4Controls.Values)
                c.RenderNtf();
        }

        public void RefreshAllFieldBind()
        {
            foreach (var c in V4Controls.Values)
                c.RefreshFieldBind();
        }

        /// <summary>
        ///     Получение выбранных условий поиска в текстовом виде
        /// </summary>
        public string GetFilterText()
        {
            var result = "";
            foreach (var ctrl in V4Controls.Values)
            {
                var item = (from mi in ctrl.GetType().GetMembers()
                            where mi.MemberType.ToString() == "Method" && mi.Name.Contains("GetFilterClauseText")
                            select mi).SingleOrDefault();
                if (item != null)
                {
                    var target = ctrl.GetType()
                        .InvokeMember("GetFilterClauseText", BindingFlags.InvokeMethod, null, ctrl, null);
                    if (!string.IsNullOrEmpty(target.ToString())) result += target + "<br />";
                }
            }

            return result;
        }

        /// <summary>
        ///     Проверка запоненности обязательных поле перед закрытием страницы
        /// </summary>
        /// <returns>Истина - проверка прошла успешно; false - не все обязательные поля заполнены</returns>
        public bool V4ValidationBeforeExit()
        {
            foreach (var ctrl in V4Controls.Values)
            {
                if (!ctrl.IsReadOnly && ctrl.IsRequired && string.IsNullOrEmpty(ctrl.Value)) return false;
                if (!ctrl.Validation()) return false;
            }

            return true;
        }

        /// <summary>
        ///     Построитель XML-документа страницы
        /// </summary>
        /// <returns>Возвращает Xml-документ</returns>
        protected XmlDocument BuildXml()
        {
            var docXml = new XmlDocument();
            var root = docXml.CreateElement("f");
            docXml.AppendChild(root);

            foreach (var ctrl in V4Controls.Values)
            {
                ctrl.Flush();
                ctrl.RefreshRequired = false;
                ctrl.PropertyChanged.Clear();
            }

            if (El.Count > 0)
            {
                var tmpNode = docXml.CreateElement("el");
                foreach (string key in El.Keys)
                {
                    var ta = docXml.CreateAttribute(key);
                    ta.Value = El[key];
                    tmpNode.Attributes.Append(ta);
                }

                root.AppendChild(tmpNode);
            }

            if (ElXML.Count > 0)
                foreach (string key in ElXML.Keys)
                {
                    var tmpNodeXml = docXml.CreateElement("elXML");
                    var idAttr = docXml.CreateAttribute("i");
                    idAttr.Value = key;
                    tmpNodeXml.Attributes.Append(idAttr);
                    tmpNodeXml.InnerXml = ElXML[key];
                    root.AppendChild(tmpNodeXml);
                }

            if (HTMLBlock.Count > 0)
                foreach (string key in HTMLBlock.Keys)
                {
                    var n = docXml.CreateElement("v4html");
                    var a = docXml.CreateAttribute("i");
                    a.Value = key;
                    n.Attributes.Append(a);
                    n.AppendChild(docXml.CreateCDataSection(HTMLBlock[key]));
                    root.AppendChild(n);
                }

            var el = docXml.CreateElement("js");

            SetScriptsAfterContent();
            el.InnerText = JS.ToString();
            root.AppendChild(el);

            ElXML.Clear();
            HTMLBlock.Clear();

            return docXml;
        }

        /// <summary>
        ///     Обновление части страницы с параметром
        /// </summary>
        /// <param name="idElement">ID контейнера</param>
        /// <param name="renderHtmlBlockWithParam">Делегат</param>
        /// <param name="param">Параметр</param>
        public void RefreshHtmlBlock(string idElement, RenderHtmlBlockWithParam renderHtmlBlockWithParam, object param)
        {
            using (TextWriter w = new StringWriter())
            {
                renderHtmlBlockWithParam(w, param);
                HTMLBlock.Add(idElement, w.ToString());
            }
        }

        /// <summary>
        ///     Обновление части страницы
        /// </summary>
        /// <param name="idElement">ID контейнера</param>
        /// <param name="renderHtmlBlock">Делегат</param>
        public void RefreshHtmlBlock(string idElement, RenderHtmlBlock renderHtmlBlock)
        {
            var w = new HtmlTextWriter(new StringWriter());
            renderHtmlBlock(w);
            if (HTMLBlock[idElement] != null) HTMLBlock.Remove(idElement);
            HTMLBlock.Add(idElement, w.InnerWriter.ToString());
        }

        /// <summary>
        ///     Клиентский скрипт обновления контрола
        /// </summary>
        /// <param name="idControl">ID контрола</param>
        public void Refresh(string idControl)
        {
            JS.Write("v4_refresh('{0}');", idControl);
        }

        /// <summary>
        ///     Вывод диалогового окна сообщения
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        /// <param name="title">Заголовок окна</param>
        public void ShowMessage(string message, string title = "")
        {
            ShowMessage(message, title, MessageStatus.Information);
        }

        /// <summary>
        ///     Вывод диалогового окна сообщения
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        /// <param name="ctrlFocus">Контрол V4Control, на который надо перевести фокус после закрытия окна </param>
        /// <param name="title">Заголовок окна</param>
        public void ShowMessage(string message, V4Control ctrlFocus, string title = "")
        {
            if (string.IsNullOrEmpty(title)) title = Resx.GetString("alertMessage");
            var ctrlId = "";
            if (ctrlFocus != null)
                ctrlId = ctrlFocus.GetFocusControl();

            ShowMessage(message, title, MessageStatus.Information, ctrlId);
        }

        /// <summary>
        ///     Вывод диалогового окна сообщения
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        /// <param name="ctrlIdFocus">Контрол, на который надо перевести фокус после закрытия окна </param>
        /// <param name="title">Заголовок окна</param>
        public void ShowMessage(string message, string ctrlIdFocus, string title = "")
        {
            if (string.IsNullOrEmpty(title)) title = Resx.GetString("alertMessage");
            ShowMessage(message, title, MessageStatus.Information, ctrlIdFocus);
        }

        /// <summary>
        ///     Вывод диалогового окна сообщения
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        /// <param name="title">Заголовок окна</param>
        /// <param name="status">Статус сообщения</param>
        /// <param name="ctrlIdFocus">Идентификатор контрола, на который надо перевести фокус после закрытия окна</param>
        public void ShowMessage(string message, string title, MessageStatus status = MessageStatus.Information,
            string ctrlIdFocus = "")
        {
            ShowMessage(message, title, status, ctrlIdFocus, null, null);
        }

        /// <summary>
        ///     Вывод диалогового окна сообщения
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        /// <param name="title">Заголовок окна</param>
        /// <param name="status">Статус сообщения</param>
        /// <param name="ctrlIdFocus">Идентификатор контрола, на который надо перевести фокус после закрытия окна</param>
        /// <param name="width">Ширина окна</param>
        /// <param name="height">Высота окна</param>
        public void ShowMessage(string message, string title, MessageStatus status, string ctrlIdFocus, int? width,
            int? height, string scriptOk = "")
        {
            JS.Write("v4_showMessage(\"{0}\",\"{1}\",{2},\"{3}\",{4},{5},\"{6}\");",
                HttpUtility.JavaScriptStringEncode(message.Replace(Environment.NewLine, "<br>")),
                HttpUtility.JavaScriptStringEncode(title),
                (int)status,
                ctrlIdFocus,
                width == null ? "null" : width.ToString(),
                height == null ? "null" : height.ToString(),
                HttpUtility.JavaScriptStringEncode(scriptOk));
        }

        /// <summary>
        ///     Формирование клиентского скрипта для вызова диалога подтверждения
        /// </summary>
        /// <param name="callbackYes">Клиентский скрипт, который долже выполниться после подтверждения</param>
        /// <param name="advMessage">Дополнительный текст, который будет выведен после основного</param>
        /// <returns>Строка JS: сформированный вызов v4_showConfirm</returns>
        public string ShowConfirmDeleteGetJS(string callbackYes, string advMessage = "")
        {
            var jsStr = string.Format("v4_showConfirm('{0}', '{1}', '{2}', '{3}', {4}, 400);",
                Resx.GetString("CONFIRM_StdMessage") + (string.IsNullOrEmpty(advMessage) ? "<br>" + advMessage : ""),
                Resx.GetString("CONFIRM_StdTitle"),
                Resx.GetString("CONFIRM_StdCaptionYes"),
                Resx.GetString("CONFIRM_StdCaptionNo"),
                callbackYes);
            return jsStr;
        }


        /// <summary>
        ///     Вывод диалогового окна подтверждения
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        /// <param name="callbackYes">Клиентский скрипт, который долже выполниться после подтверждения</param>
        /// <param name="width">Ширина окна</param>
        public void ShowConfirm(string message, string callbackYes, int? width)
        {
            ShowConfirm(message, Resx.GetString("CONFIRM_StdTitle"), Resx.GetString("CONFIRM_StdCaptionYes"),
                Resx.GetString("CONFIRM_StdCaptionNo"), callbackYes, "", width);
        }

        /// <summary>
        ///     Вывод диалогового окна подтверждения
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        /// <param name="title">Заголовок окна</param>
        /// <param name="captionYes">Текст на кнопке подтверждения</param>
        /// <param name="captionNo">Текст на кнопке отмены</param>
        /// <param name="callbackYes">Клиентский скрипт, который долже выполниться после подтверждения</param>
        /// <param name="ctrlIdFocus">Идентиикатор контрола, на который необходимо установить фокус после подтверждения</param>
        /// <param name="width">Ширина окна</param>
        public void ShowConfirm(string message, string title, string captionYes, string captionNo, string callbackYes,
            string ctrlIdFocus, int? width)
        {
            ShowConfirm(message, title, captionYes, captionNo, callbackYes, ctrlIdFocus, 75, 75, width, null);
        }

        /// <summary>
        ///     Вывод диалогового окна подтверждения
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        /// <param name="title">Заголовок окна</param>
        /// <param name="captionYes">Текст на кнопке подтверждения</param>
        /// <param name="captionNo">Текст на кнопке отмены</param>
        /// <param name="callbackYes">Клиентский скрипт, который долже выполниться после подтверждения</param>
        /// <param name="ctrlIdFocus">Идентиикатор контрола, на который необходимо установить фокус после подтверждения</param>
        /// <param name="width">Ширина окна</param>
        public void ShowConfirm(string message, string title, string captionYes, string captionNo, string callbackYes,
            string callbackNo,
            string ctrlIdFocus, int? width)
        {
            ShowConfirm(message, title, captionYes, captionNo, callbackYes, callbackNo, ctrlIdFocus, 75, 75, width,
                null);
        }

        /// <summary>
        ///     Вывод диалогового окна подтверждения
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        /// <param name="title">Заголовок окна</param>
        /// <param name="captionYes">Текст на кнопке подтверждения</param>
        /// <param name="captionNo">Текст на кнопке отмены</param>
        /// <param name="callbackYes">Клиентский скрипт, который долже выполниться после подтверждения</param>
        /// <param name="ctrlIdFocus">Идентиикатор контрола, на который необходимо установить фокус после подтверждения</param>
        /// <param name="widthYes">Шинина кнопки подтверждения</param>
        /// <param name="widthNo">Ширина кнопки отмены</param>
        /// <param name="width">Ширина окна</param>
        /// <param name="height">Высота окна</param>
        public void ShowConfirm(string message, string title, string captionYes, string captionNo, string callbackYes,
            string ctrlIdFocus, int? widthYes, int? widthNo, int? width, int? height)
        {
            ShowConfirm(message, title, captionYes, captionNo, callbackYes, "", ctrlIdFocus, widthYes, widthNo, width,
                height);
        }

        /// <summary>
        ///     Вывод диалогового окна подтверждения
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        /// <param name="title">Заголовок окна</param>
        /// <param name="captionYes">Текст на кнопке подтверждения</param>
        /// <param name="captionNo">Текст на кнопке отмены</param>
        /// <param name="callbackYes">Клиентский скрипт, который долже выполниться после подтверждения</param>
        /// <param name="ctrlIdFocus">Идентиикатор контрола, на который необходимо установить фокус после подтверждения</param>
        /// <param name="width">Ширина окна</param>
        public void ShowConfirm(string message, string title, string captionYes, string captionNo, string callbackYes,
            string callbackNo,
            string ctrlIdFocus, int? widthYes, int? widthNo, int? width, int? height, bool setFocusYes = false, bool showButtonImg = true, string captionCancel = "", string callbackCancel = "")
        {
            JS.Write("v4_showConfirm(\"{0}\",\"{1}\",\"{2}\",\"{3}\",{4},{5},\"{6}\",\"{7}\",\"{8}\",{9},{10},{11},{12},\"{13}\",\"{14}\");",
                HttpUtility.JavaScriptStringEncode(message.Replace(Environment.NewLine, "<br>")),
                HttpUtility.JavaScriptStringEncode(title),
                HttpUtility.JavaScriptStringEncode(captionYes),
                HttpUtility.JavaScriptStringEncode(captionNo),
                widthYes == null ? "null" : widthYes.ToString(),
                widthNo == null ? "null" : widthNo.ToString(),
                HttpUtility.JavaScriptStringEncode(callbackYes),
                HttpUtility.JavaScriptStringEncode(callbackNo),
                HttpUtility.JavaScriptStringEncode(ctrlIdFocus),
                width == null ? "null" : width.ToString(),
                height == null ? "null" : height.ToString(),
                setFocusYes ? 1 : 0,
                showButtonImg ? 1 : 0,
                HttpUtility.JavaScriptStringEncode(captionCancel),
                HttpUtility.JavaScriptStringEncode(callbackCancel)
                );
        }

        /// <summary>
        ///     Вывод диалогового окна пересчета
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        /// <param name="title">Заголовок окна</param>
        /// <param name="caption1">Текст на кнопке подтверждения 1</param>
        /// <param name="caption2">Текст на кнопке подтверждения 2</param>
        /// <param name="caption3">Текст на кнопке подтверждения 3</param>
        /// <param name="caption4">Текст на кнопке подтверждения 4</param>
        /// <param name="callback1">Клиентский скрипт, который долже выполниться после нажатия на кнопку 1</param>
        /// <param name="callback2">Клиентский скрипт, который долже выполниться после нажатия на кнопку 2</param>
        /// <param name="callback3">Клиентский скрипт, который долже выполниться после нажатия на кнопку 3</param>
        /// <param name="callback4">Клиентский скрипт, который долже выполниться после нажатия на кнопку 4</param>
        /// <param name="ctrlIdFocus">Идентиикатор контрола, на который необходимо установить фокус после подтверждения</param>
        /// <param name="width">Ширина окна</param>
        public void ShowRecalc(string message, string title, string caption1, string caption2, string caption3,
            string caption4, string callback1, string callback2, string callback3, string callback4,
            string ctrlIdFocus, int? width)
        {
            ShowRecalc(message, title, caption1, caption2, caption3,
                caption4, callback1, callback2, callback3, callback4,
                ctrlIdFocus, 80, 80, 80, 80, width, null);
        }

        /// <summary>
        ///     Вывод диалогового окна пересчета
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        /// <param name="title">Заголовок окна</param>
        /// <param name="caption1">Текст на кнопке подтверждения 1</param>
        /// <param name="caption2">Текст на кнопке подтверждения 2</param>
        /// <param name="caption3">Текст на кнопке подтверждения 3</param>
        /// <param name="caption4">Текст на кнопке подтверждения 4</param>
        /// <param name="callback1">Клиентский скрипт, который долже выполниться после нажатия на кнопку 1</param>
        /// <param name="callback2">Клиентский скрипт, который долже выполниться после нажатия на кнопку 2</param>
        /// <param name="callback3">Клиентский скрипт, который долже выполниться после нажатия на кнопку 3</param>
        /// <param name="callback4">Клиентский скрипт, который долже выполниться после нажатия на кнопку 4</param>
        /// <param name="ctrlIdFocus">Идентиикатор контрола, на который необходимо установить фокус после подтверждения</param>
        /// <param name="width">Ширина окна</param>
        /// <param name="height">Высота окна</param>
        public void ShowRecalc(string message, string title, string caption1, string caption2, string caption3,
            string caption4, string callback1, string callback2, string callback3, string callback4,
            string ctrlIdFocus, int? width1, int? width2, int? width3, int? width4, int? width, int? height)
        {
            JS.Write(
                "v4_showRecalc(\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",{6},{7},{8},{9},\"{10}\",\"{11}\",\"{12}\",\"{13}\",\"{14}\",{15},{16});",
                HttpUtility.JavaScriptStringEncode(message.Replace(Environment.NewLine, "<br>")),
                HttpUtility.JavaScriptStringEncode(title),
                HttpUtility.JavaScriptStringEncode(caption1),
                HttpUtility.JavaScriptStringEncode(caption2),
                HttpUtility.JavaScriptStringEncode(caption3),
                HttpUtility.JavaScriptStringEncode(caption4),
                width1 == null ? "null" : width1.ToString(),
                width2 == null ? "null" : width2.ToString(),
                width3 == null ? "null" : width3.ToString(),
                width4 == null ? "null" : width4.ToString(),
                HttpUtility.JavaScriptStringEncode(callback1),
                HttpUtility.JavaScriptStringEncode(callback2),
                HttpUtility.JavaScriptStringEncode(callback3),
                HttpUtility.JavaScriptStringEncode(callback4),
                HttpUtility.JavaScriptStringEncode(ctrlIdFocus),
                width == null ? "null" : width.ToString(),
                height == null ? "null" : height.ToString());
        }

        /// <summary>
        ///     Связывание контролов
        /// </summary>
        /// <param name="obj">Источник данных</param>
        /// <param name="direction">Направление</param>
        /// <returns>true - были связаны 1 или более контролов</returns>
        public virtual bool BindEntity(object obj, BindingDirection direction)
        {
            var changed = false;
            foreach (var ctrl in V4Controls.Values)
            {
                if (ctrl.BindingField.Length == 0) continue;

                if (ctrl.BindSimple(obj, direction)) changed = true;
            }

            return changed;
        }

        /// <summary>
        ///     Добавить скрипт: Показать элемент
        /// </summary>
        /// <param name="idControl">ID элемента управления</param>
        public void DisplayCtrl(string idControl)
        {
            JS.Write("$('#{0}').show();", idControl);
        }

        /// <summary>
        ///     Добавить скрипт: Скрыть элемент
        /// </summary>
        /// <param name="idControl">ID элемента управления</param>
        public void HideCtrl(string idControl)
        {
            JS.Write("$('#{0}').hide();", idControl);
        }

        /// <summary>
        ///     Добавить скрипт: Показать элемент
        /// </summary>
        /// <param name="idControl">ID элемента управления</param>
        public void Display(string idControl)
        {
            JS.Write("di('{0}');", idControl);
        }

        /// <summary>
        ///     Добавить скрипт: Скрыть элемент
        /// </summary>
        /// <param name="idControl">ID элемента управления</param>
        public void Hide(string idControl)
        {
            JS.Write("hi('{0}');", idControl);
        }

        /// <summary>
        ///     Добавить скрипт: Показать элемент
        /// </summary>
        /// <param name="idControl">ID элемента управления</param>
        /// <param name="displayType">Тип отображения элемента</param>
        public void Display(string idControl, string displayType)
        {
            if (displayType == "none")
                Hide(idControl);
            else if (string.IsNullOrEmpty(displayType))
                Display(idControl);
            else
                JS.Write("di('{0}','{1}');", idControl, displayType);
        }

        /// <summary>
        ///     Добавить скрипт: Показать элемент
        /// </summary>
        /// <param name="idControl">ID элемента управления</param>
        /// <param name="isVisible">Маркер видимости элемента</param>
        public void Display(string idControl, bool isVisible)
        {
            if (isVisible)
                Display(idControl);
            else
                Hide(idControl);
        }

        /// <summary>
        ///     Отрисовка числа
        /// </summary>
        /// <param name="w">Поток</param>
        /// <param name="number">Число</param>
        /// <param name="scale">Шкала</param>
        public void RenderNumber(TextWriter w, string number, int scale)
        {
            var rndNumber = new NumberRenderer(scale, scale, " ");
            rndNumber.Render(w, number);
        }

        /// <summary>
        ///     Отрисовка числа
        /// </summary>
        /// <param name="w">Поток</param>
        /// <param name="number">Число</param>
        /// <param name="scale">Шкала</param>
        /// <param name="groupSeparator">Разделитель</param>
        public void RenderNumber(TextWriter w, string number, int scale, string groupSeparator)
        {
            var rndNumber = new NumberRenderer(scale, scale, groupSeparator);
            rndNumber.Render(w, number);
        }

        /// <summary>
        ///     Отрисовка числа
        /// </summary>
        /// <param name="w">Поток</param>
        /// <param name="number">Число</param>
        /// <param name="minScale">Минимальное значение</param>
        /// <param name="maxScale">Максимальное значение</param>
        /// <param name="groupSeparator">Разделитель</param>
        public void RenderNumber(TextWriter w, string number, int minScale, int maxScale, string groupSeparator)
        {
            var rndNumber = new NumberRenderer(minScale, maxScale, groupSeparator);
            rndNumber.Render(w, number);
        }

        
        /// <summary>
        ///     Восстановление курсора по-умолчанию после асинхронного вызова
        /// </summary>
        public void RestoreCursor()
        {
            if (V4IsPostBack)
                JS.Write("Kesco.wait(false);");
        }

        /// <summary>
        ///     Виртуальная процедура получения значений из ресурсного файла, в зависимости от нужд приложения
        /// </summary>
        protected virtual void ResxResourceSet()
        {
        }


        /// <summary>
        ///     Метод открытия справки по странице
        /// </summary>
        private void RenderOpenHelpPage()
        {
            JS.Write("v4_openHelp('{0}');", HttpUtility.JavaScriptStringEncode(IDPage.Replace("-", "")));
            RestoreCursor();
        }

        /// <summary>
        ///     Метод принимает результат из модальных окон
        /// </summary>
        public virtual void DialogResult(string dialogResult)
        {
            //base.DialogResult(dialogResult);
            //var a = dialogResult;
        }

        /// <summary>
        ///     Сохранить параметры пользователя
        /// </summary>
        public virtual void SaveParameters()
        {
        }

        /// <summary>
        ///     Трансляция событий формы на других пользователей
        /// </summary>
        /// <param name="Params">Коллекция параметров</param>
        public virtual void TranslatePageEvent(NameValueCollection Params)
        {
        }

        /// <summary>
        ///     Трансляция событий элементов управления на других пользователей
        /// </summary>
        /// <param name="Params">Коллекция параметров</param>
        public void TranslateCtrlEvent(NameValueCollection Params)
        {
            var ctrlId = Params["ctrl"];

            if (!(this is EntityPage) && ctrlId != "btnLike") return;
            if (ItemId == 0 && ctrlId != "btnLike") return;

            var existsMessage = false;
            var pages = KescoHub.GetAllPages().ToList();
            var receiveClients = SignaRReceiveClientsMessageEnum.ItemId_ItemName;

            pages.ForEach(p =>
            {
                if (!(p is Page)) return;
                var page = (Page)p;

                if (!page.V4Controls.ContainsKey(ctrlId)) return;

                if (((page.ItemId > 0 && page.ItemId == ItemId) || (page.ItemId >= 0 && ctrlId == "btnLike"))
                                    && page.ItemName == ItemName && page.IDPage != IDPage)

                {

                    if (ctrlId == "btnLike") receiveClients = SignaRReceiveClientsMessageEnum.ItemName;

                    existsMessage = true;

                    var old = page.V4Controls[ctrlId].Value;
                    var orig = page.V4Controls[ctrlId].OriginalValue;

                    page.V4Controls[ctrlId].Value = V4Controls[ctrlId].Value;
                    page.V4Controls[ctrlId]
                        .OnChanged(new ProperyChangedEventArgs(old, V4Controls[ctrlId].Value, orig, true));
                }
            });

            if (!existsMessage) return;
            SendCmdAsyncMessage(receiveClients);
        }

        /// <summary>
        ///  Отправка сообщения об изменениях значений контролов
        /// </summary>
        /// <param name="receiveClients">Кто должен получить сообщения </param>
        public void SendCmdAsyncMessage(SignaRReceiveClientsMessageEnum receiveClients = SignaRReceiveClientsMessageEnum.ItemId_ItemName)
        {
            KescoHub.SendMessage(new SignalMessage
            {
                PageId = IDPage,
                ItemId = ItemId.ToString(),
                ItemName = ItemName,
                IsV4Script = true,
                Message = "<js>cmdasync();</js>"
            }, receiveClients);
        }

        public void ClearObjects()
        {
            ObjList.Clear();
        }

        public Entity GetObjectById(Type t, string id)
        {
            var ret = ObjList.Find(o => (o.Type == t || t == typeof(Document)) && o.Object.Id == id);

            if (ret != null
                && V4IsPostBack
                && (string.IsNullOrEmpty(ret.Object.CurrentPostRequest) ||
                    ret.Object.CurrentPostRequest != IDPostRequest)
                && ret.Object.GetLastChanged(id) != ret.Object.Changed)
            {
                objList.Remove(ret);
                ret = null;
            }

            if (ret == null)
            {
                var ci = t.GetConstructor(new[] { typeof(string) });
                if (ci != null)
                {
                    var o = ci.Invoke(new object[] { id }) as Entity;
                    if (o != null)
                    {
                        o.CurrentPostRequest = IDPostRequest;
                        ret = new V4PageObj { Type = t, Object = o };
                        objList.Add(ret);
                    }
                }
            }
            else if (V4IsPostBack)
            {
                if (string.IsNullOrEmpty(ret.Object.CurrentPostRequest) ||
                    ret.Object.CurrentPostRequest != IDPostRequest) ret.Object.CurrentPostRequest = IDPostRequest;
            }

            if (ret != null) return ret.Object;

            return null;
        }


        /// <summary>
        ///     Процедура очистки загруженных и закэшированных свойств объектов
        /// </summary>
        protected virtual void ClearLoadedExternalProperties()
        {
        }


        //Удаляем все закешированные объекты на странице
        public void ClearCacheObjects()
        {
            ClearLoadedExternalProperties();
            if (objList != null) objList.Clear();
            objList = null;
            GC.Collect();
        }


        private void InitMenuButton()
        {
            MenuButtons.Clear();
            var btnEdit = new Button
            {
                ID = "btnEdit",
                V4Page = this,
                Text = Resx.GetString("cmdEdit"),
                Title = Resx.GetString("cmdEditDescription"),
                IconJQueryUI = ButtonIconsEnum.Edit,
                Width = 115,
                NextControl = "btnSave",
                OnClick = "cmdasync('cmd', 'Edit');"
            };
            AddMenuButton(btnEdit);

            var btnSave = new Button
            {
                ID = "btnSave",
                V4Page = this,
                Text = Resx.GetString("cmdOK"),
                Title = Resx.GetString("titleSaveAndClose"),
                IconJQueryUI = ButtonIconsEnum.Ok,
                Width = 105,
                NextControl = "btnRefresh",
                OnClick = "cmdasync('cmd', 'Save');"
            };
            AddMenuButton(btnSave);

            var btnApply = new Button
            {
                ID = "btnApply",
                V4Page = this,
                Text = Resx.GetString("cmdSave"),
                Title = Resx.GetString("titleSave"),
                IconJQueryUI = ButtonIconsEnum.Save,
                Width = 105,
                NextControl = "btnRefresh",
                OnClick = "cmdasync('cmd', 'Apply');"
            };
            AddMenuButton(btnApply);

            var btnRefresh = new Button
            {
                ID = "btnRefresh",
                V4Page = this,
                Text = Resx.GetString("cmdRefresh"),
                Title = Resx.GetString("cmdRefreshDescription"),
                IconJQueryUI = ButtonIconsEnum.Refresh,
                Width = 105,
                NextControl = "btnReCheck",
                OnClick = "cmdasync('cmd', 'Refresh');"
            };
            AddMenuButton(btnRefresh);

            var btnReCheck = new Button
            {
                ID = "btnReCheck",
                V4Page = this,
                Text = Resx.GetString("cmdReCheck"),
                Title = Resx.GetString("cmdReCheckDescription"),
                IconJQueryUI = ButtonIconsEnum.Check,
                NextControl = "btnCancel",                
                OnClick = "cmdasync('cmd', 'ReCheck');"
            };
            AddMenuButton(btnReCheck);

            var btnCancel = new Button
            {
                ID = "btnCancel",
                V4Page = this,
                Text = Resx.GetString("cmdCancel"),
                Title = Resx.GetString("cmdCancelDescription"),
                IconJQueryUI = ButtonIconsEnum.Cancel,
                Width = 80,
                OnClick = "cmdasync('cmd', 'Cancel');"
            };
            AddMenuButton(btnCancel);
        }

        /// <summary>
        ///     Добавление кнопок в меню в конец списка
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
            MenuButtons.AddRange(buttons);
        }

        /// <summary>
        ///     Добавление кнопок в меню в указанную позицию
        /// </summary>
        /// <remarks>
        ///     В качестве параметра может получать:
        ///     одиночный объект,
        ///     объекты через запятую,
        ///     массив объектов Button
        /// </remarks>
        /// <param name="buttons">объект контрола button</param>
        public void InsertMenuButton(int index, params Button[] buttons)
        {
            MenuButtons.InsertRange(index, buttons);
        }

        /// <summary>
        ///     Удаление кнопок в меню
        /// </summary>
        /// <param name="button">объект контрола button</param>
        public void RemoveMenuButton(Button button)
        {
            MenuButtons.Remove(button);
        }

        /// <summary>
        ///     Удаление всех кнопок в меню
        /// </summary>
        public void RemoveMenuAllButton()
        {
            MenuButtons = new List<Button>();
        }

        /// <summary>
        ///     Очистить коллекцию кнопок
        /// </summary>
        public void ClearMenuButtons()
        {
            MenuButtons.Clear();
        }

        protected virtual void RenderAddControl(StringWriter w)
        {
        }

        /// <summary>
        ///     Сформировать кнопки меню
        /// </summary>
        public virtual void RenderButtons(StringWriter w)
        {
            w.Write("<div id=\"pageHeader\" class=\"v4pageHeader\" style=\"{0}\">",
                MenuButtons.Count == 0 ? ";height:23px" : "");

            foreach (var b in MenuButtons)
            {
                V4Controls.Add(b);
                b.RenderControl(w);
                b.PropertyChanged.Clear();
            }

            RenderAddControl(w);

            if (!string.IsNullOrEmpty(LogoImage))
                w.Write("<img src=\"{0}\" style=\"float: left; margin-left: 2px; border: 0; height: 23px;\">",
                    LogoImage);

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


            if (!LikeId.IsNullEmptyOrZero())
            {
                var btnLike = new LikeDislike
                {
                    ID = "btnLike",
                    V4Page = this,
                    LikeId = LikeId,
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
                JS.Write("if(gi('pageHeader')) gi('pageHeader').innerHTML={0};", HttpUtility.JavaScriptStringEncode(w.ToString(), true));
            }
        }


        //TODO: УДАЛИТЬ после перехода на KESCORUN
        /// <summary>
        ///     Открытие в архиве документов
        /// </summary>
        /// <param name="id">Id Текущего документа</param>
        protected void OpenDoc(string id, bool replicate, bool openImage, string typeId = "")
        {
            DocType oDocType;

            if (string.IsNullOrEmpty(id))
                return;

            if (typeId == "")
            {
                var oDoc = new Document(id);
                oDocType = new DocType(oDoc.TypeId.ToString());
            }
            else
            {
                oDocType = new DocType(typeId);
            }

            JS.Write("srv4js('OPENDOC','id={0}&newwindow=1{2}{1}',", id, replicate ? "&replicate=true" : "",
                openImage ? "&imageid=-2" : "&imageid=0");
            JS.Write("function(rez,obj){if(rez.error){");
            if (!oDocType.Unavailable)
                JS.Write(
                    "Kesco.windowOpen('{0}','{1}', null, null, 'openDoc');",
                    oDocType.URL + (oDocType.URL.IndexOf("?", StringComparison.Ordinal) > 0 ? "&" : "?") +
                    "id=" +
                    id, "Empl_"+id);
            else
                JS.Write("alert({0}+rez.errorMsg);",
                    "Документ не имеет электронной формы.\nПросмотр изображения в архиве документов невозможен:\n");

            JS.Write("}");
            JS.Write("}");
            JS.Write(",null);");
        }

        /// <summary>
        ///     Открытие в архиве документов
        /// </summary>
        /// <param name="id">Id Текущего документа</param>
        protected void ShowDocumentInDocview(string id, bool replicate, bool openImage)
        {
            if (string.IsNullOrEmpty(id))
                return;

            JS.Write(DocViewInterop.OpenDocument(id, openImage, replicate));
        }


        #region RendeLink

        #region Resource

        /// <summary>
        /// Отрисовать ссылку на ресурс
        /// </summary>
        /// <param name="w">Поток вывода</param>
        /// <param name="id">Идентификатор контрола</param>
        /// <param name="value">Значение контрола</param>
        /// <param name="text">Text ссылки</param>
        /// <param name="ntf">Стиль сообщения</param>
        /// <param name="tabIndex">Переход по tab</param>
        /// <param name="title">Всплывающая подсказка</param>
        /// <param name="opener">Клиент, откуда будет открываться ссылка</param>
        public void RenderLinkResource(TextWriter w, string id, string value, string text, NtfStatus ntf = NtfStatus.Empty, string title = "", string tabIndex = "", string opener = "")
        {
            if (string.IsNullOrEmpty(opener)) opener = "linkRes";

            RenderLink(w, "linkRes" + id, "", text, "",
                Config.resource_form + (Config.resource_form.IndexOf('?') == -1 ? "?" : "&") + "id=" + value, "", "", title, tabIndex, ntf, false, CallerTypeEnum.Empty, false, true, false, opener);
        }

        #endregion


        #region Location

       
        /// <summary>
        /// Отрисовать ссылку на расположение
        /// </summary>
        /// <param name="w">Поток вывода</param>
        /// <param name="id">Идентификатор контрола</param>
        /// <param name="value">Значение контрола</param>
        /// <param name="text">Text ссылки</param>
        /// <param name="ntf">Стиль сообщения</param>
        /// <param name="tabIndex">Переход по tab</param>
        /// <param name="title">Всплывающая подсказка</param>
        /// <param name="opener">Клиент, откуда будет открываться ссылка</param>
        public void RenderLinkLocation(TextWriter w, string id, string value, string text, NtfStatus ntf = NtfStatus.Empty, string title = "", string tabIndex = "",  string opener = "")
        {
            if (string.IsNullOrEmpty(opener)) opener = "linkLoc";
            RenderLink(w, "hrefLoc" + id, "", text, "",
              Config.location_search + (Config.location_search.IndexOf('?') == -1 ? "?" : "&") + "id=" + value, "", "", title, tabIndex, ntf, false, CallerTypeEnum.Empty, false, true, false, opener);
        }

        #endregion


        #region Equipment

        /// <summary>
        ///     Отрисовать ссылку на оборудование
        /// </summary>
        /// <param name="w">Поток вывода</param>
        /// <param name="id">Идентификатор контрола</param>
        /// <param name="value">Значение контрола</param>
        /// <param name="text">Text ссылки</param>
        /// <param name="ntf">Стиль сообщения</param>
        /// <param name="tabIndex">Переход по tab</param>
        /// <param name="title">Всплывающая подсказка</param>
        /// <param name="opener">Клиент, откуда будет открываться ссылка</param>
        public void RenderLinkEquipment(TextWriter w, string id, string value, string text, NtfStatus ntf = NtfStatus.Empty, string title = "",  string tabIndex = "",  string opener = "")
        {
            if (string.IsNullOrEmpty(opener)) opener = "linkEq";

            RenderLink(w, "hrefEq" + id, "", text, "",
                Config.equipment_form + (Config.equipment_form.IndexOf('?') == -1 ? "?" : "&") + "id=" + value, "", "", title, tabIndex, ntf, false, CallerTypeEnum.Empty, false, true, false, opener);
        }

        #endregion


        #region PhoneNumber
        /// <summary>
        ///     Отрисовать ссылку на телефонный номер
        /// </summary>
        /// <param name="w">Поток вывода</param>
        /// <param name="id">Идентификатор контрола</param>
        /// <param name="value">Значение контрола</param>
        /// <param name="text">Text ссылки</param>
        /// <param name="ntf">Стиль сообщения</param>
        /// <param name="tabIndex">Переход по tab</param>
        /// <param name="title">Всплывающая подсказка</param>
        /// <param name="opener">Клиент, откуда будет открываться ссылка</param>
        public void RenderLinkPhoneNumber(TextWriter w, string id, string value, string text, NtfStatus ntf = NtfStatus.Empty, string title = "",  string tabIndex = "",  string opener = "")
        {
            if (string.IsNullOrEmpty(opener)) opener = "linkTel";

            RenderLink(w, "hrefTel" + id, "", text, "",
              Config.tel_form + (Config.equipment_form.IndexOf('?') == -1 ? "?" : "&") + "id=" + value, "", "", title, tabIndex, ntf, false, CallerTypeEnum.Empty, false, true, false, opener);
        }

        #endregion


        #region Document

        /// <summary>
        ///     Отрисовать ссылку на документ
        /// </summary>
        /// <param name="w">Поток вывода</param>
        /// <param name="docId">КодДокумента</param>
        public void RenderLinkDocument(TextWriter w, int docId, string docName, int? docTypeId = null, bool openImage = false, NtfStatus ntf = NtfStatus.Empty, string title = "", string opener = "")
        {
            var ntfClass = EnumAccessors.GetCssClassByNtfStatus(ntf, false);

            w.Write(
                "<a {0} onclick=\"cmdasync('cmd', 'ShowInDocView', 'DocId', {1}, 'TypeId', '{2}', 'openImage', {3});\" title=\"{4}\">",
                ntfClass != "" ? string.Format("class='{0}'", ntfClass) : "",
                docId,
                !docTypeId.HasValue ? "" : docTypeId.Value.ToString(),
                openImage ? 1 : 0,
                title);
            w.Write(docName);
            w.Write("</a>");
        }

        /// <summary>
        ///     Отрисовка ссылки на документ
        /// </summary>
        public void RenderLinkDocument(TextWriter w, int docId, Document doc, bool openImage = false, NtfStatus ntf = NtfStatus.Empty, string title = "", string opener = "")
        {
            if (doc == null || doc.Unavailable || doc.DataUnavailable)
            {
                w.Write("#" + docId);
                return;
            }

            var name = doc.GetFullDocumentName(CurrentUser);

            int? typeId = null;
            if (doc.DocType != null)
                typeId = int.Parse(doc.DocType.Id);

            RenderLinkDocument(w, docId, name, typeId, openImage, ntf, title, opener);
        }

        #endregion


        #region Employee

        /// <summary>
        ///     Отрисовка ссылки на сотрудника
        /// </summary>
        public void RenderLinkEmployee(TextWriter w, string htmlId, Employee empl, NtfStatus ntf, bool isNtf = true, string opener = "")
        {
            if (empl == null) return;
            var name = IsRusLocal ? empl.FullName : empl.FullNameEn;
            RenderLinkEmployee(w, htmlId, empl.Id, name, ntf, isNtf, opener);
        }

        /// <summary>
        ///     Отрисовка ссылки на сотрудника
        /// </summary>
        /// <param name="w">Поток</param>
        /// <param name="emplId">КодСотрудника</param>
        /// <param name="emplName">Название сотрудника</param>
        public void RenderLinkEmployee(TextWriter w, string htmlId, string emplId, string emplName, NtfStatus ntf, bool isNtf = true, string opener = "")
        {
            string url = string.Concat(Config.user_form, "?id=", emplId);
            RenderLinkCaller(w, htmlId, emplId, emplName, url, ntf, isNtf, CallerTypeEnum.Employee, true, true, opener);
        }

        #endregion


        #region Person

        /// <summary>
        ///     Отсисовка ссылки на лицо
        /// </summary>
        /// <param name="w">Поток</param>
        /// <param name="personId">КодЛица</param>
        /// <param name="ntf">Стиль NTF</param>
        /// <param name="isNtf">Размер NTF</param>
        public void RenderLinkPerson(TextWriter w, string htmlId, string personId, string personName,
            NtfStatus ntf = NtfStatus.Empty, bool isNtf = true, bool isSetToolTip = true, string opener="")
        {
            var url = string.Concat(Config.person_form, "?id=", personId);
            RenderLinkCaller(w, htmlId, personId, personName, url, ntf, isNtf, CallerTypeEnum.Person, false, isSetToolTip, opener);
        }

        #endregion


        ////////////////////////////////////////////////////////////////////////


        #region Caller
        /// <summary>
        ///     Отрисовка в поток гиперссылки со звонилкой
        /// </summary>
        /// <param name="w">Поток</param>
        /// <param name="id">Идентификатор контрола</param>
        /// <param name="value">Значение контрола, используется для передачи в звонилку</param>
        /// <param name="text">Текст гиперссылки</param>
        /// <param name="url">Ссылка, которая будет открыта через window.open</param>
        /// <param name="urlParams">Параметры окна для window.open</param>
        /// <param name="urlTarget">Handler окна для window.open</param>
        /// <param name="tabIndex">Индекс табуляции</param>
        /// <param name="callerType">Если не пустой, то определяет тип получаемых контактов для звонилки</param>
        public void RenderLinkCaller(TextWriter w, string id, string value, string text, string url, string urlParams, string urlTarget, string tabIndex, CallerTypeEnum callerType, bool isNoWrap = false)
        {
            RenderLink(w, id, value, text, "", url, urlParams, urlTarget, tabIndex, "", NtfStatus.Empty, false, callerType, isNoWrap);
        }

        /// <summary>
        ///     Отрисовка в поток гиперссылки со звонилкой
        /// </summary>
        /// <param name="w">Поток</param>
        /// <param name="value">Значение контрола, используется для передачи в звонилку</param>
        /// <param name="text">Текст гиперссылки</param>
        /// <param name="url">Ссылка, которая будет открыта через window.open</param>
        /// <param name="urlParams">Параметры окна для window.open</param>
        /// <param name="urlTarget">Handler окна для window.open</param>
        /// <param name="tabIndex">Индекс табуляции</param>
        /// <param name="callerType">Если не пустой, то определяет тип получаемых контактов для звонилки</param>
        public void RenderLinkCaller(TextWriter w, string value, string text, string url, string urlParams, string urlTarget, string tabIndex, CallerTypeEnum callerType, bool isNoWrap = false)
        {
            RenderLink(w, "", value, text, "", url, urlParams, urlTarget, tabIndex, "", NtfStatus.Empty, false, callerType, isNoWrap);
        }


        /// <summary>
        ///     Отрисовка в поток гиперссылки со звонилкой
        /// </summary>
        /// <param name="w">Поток</param>
        /// <param name="htmlId">Идентификатор ссылки</param>
        /// <param name="value">Значение контрола, используется для передачи в звонилку</param>
        /// <param name="text">Текст гиперссылки</param>
        /// <param name="url">Ссылка, которая будет открыта через window.open</param>
        /// <param name="ntf">Стиль отображения текста</param>
        /// <param name="isNtf">Определяет размер текста</param>
        /// <param name="callerType">Если не пустой, то определяет тип получаемых контактов для звонилки</param>
        public void RenderLinkCaller(TextWriter w, string htmlId, string value, string text, string url, NtfStatus ntf, bool isNtf, CallerTypeEnum callerType, bool isNoWrap = false, bool isSetToolTip = true, string opener="")
        {
            RenderLink(w, htmlId, value, text, "", url, "", "", "", "", ntf, isNtf, callerType, isNoWrap, true, isSetToolTip, opener);
        }

        /// <summary>
        ///     Отрисовка в поток гиперссылки со звонилкой
        /// </summary>
        /// <param name="w">Поток</param>
        /// <param name="htmlId">Идентификатор ссылки</param>
        /// <param name="value">Значение контрола, используется для передачи в звонилку</param>
        /// <param name="text">Текст гиперссылки</param>
        /// <param name="url">Ссылка, которая будет открыта через window.open</param>
        /// <param name="ntf">Стиль отображения текста</param>
        /// <param name="callerType">Если не пустой, то определяет тип получаемых контактов для звонилки</param>
        public void RenderLinkCaller(TextWriter w, string htmlId, string value, string text, string url, NtfStatus ntf, CallerTypeEnum callerType, bool isNoWrap = false)
        {
            RenderLink(w, htmlId, value, text, "", url, "", "", "", "", ntf, true, callerType, isNoWrap);
        }

        /// <summary>
        ///     Отрисовка в поток гиперссылки со звонилкой
        /// </summary>
        /// <param name="w">Поток</param>
        /// <param name="id">Идентификатор контрола</param>
        /// <param name="value">Значение контрола, используется для передачи в звонилку</param>
        /// <param name="text">Текст гиперссылки</param>
        /// <param name="onClick">Javascript-функция, которая вызывается на Click по гиперссылке</param>
        /// <param name="tabIndex">Индекс табуляции</param>
        /// <param name="callerType">Если не пустой, то определяет тип получаемых контактов для звонилки</param>
        public void RenderLinkCaller(TextWriter w, string id, string value, string text, string onClick, string tabIndex, CallerTypeEnum callerType, bool isNoWrap = false)
        {
            RenderLink(w, id, value, text, onClick, "", "", "", "", tabIndex, NtfStatus.Empty, false, callerType, isNoWrap);
        }

        /// <summary>
        ///     Отрисовка в поток гиперссылки со звонилкой
        /// </summary>
        /// <param name="w">Поток</param>
        /// <param name="value">Значение контрола, используется для передачи в звонилку</param>
        /// <param name="text">Текст гиперссылки</param>
        /// <param name="onClick">Javascript-функция, которая вызывается на Click по гиперссылке</param>
        /// <param name="tabIndex">Индекс табуляции</param>
        /// <param name="callerType">Если не пустой, то определяет тип получаемых контактов для звонилки</param>
        public void RenderLinkCaller(TextWriter w, string value, string text, string onClick, string tabIndex, CallerTypeEnum callerType, bool isNoWrap = false)
        {
            RenderLink(w, "", value, text, onClick, "", "", "", "", tabIndex, NtfStatus.Empty, false, callerType, isNoWrap);
        }

        #endregion


        ////////////////////////////////////////////////////////////////////////


        #region Link

        /// <summary>
        ///     Отрисовка в поток гиперссылки
        /// </summary>
        /// <param name="w">Поток</param>
        /// <param name="text">Текст гиперссылки</param>
        /// <param name="url">Ссылка, которая будет открыта через window.open</param>
        /// <param name="tabIndex">Индекс табуляции</param>
        public void RenderLink(TextWriter w, string text, string url, string title, string tabIndex, string opener = "")
        {
            RenderLink(w, "", "", text, "", url, "", "", title, tabIndex, NtfStatus.Empty, false, CallerTypeEnum.Empty, false, true, true, opener);
        }

        /// <summary>
        ///     Отрисовка в поток гиперссылки
        /// </summary>
        /// <param name="w">Поток</param>
        /// <param name="text">Текст гиперссылки</param>
        /// <param name="url">Ссылка, которая будет открыта через window.open</param>
        /// <param name="tabIndex">Индекс табуляции</param>
        public void RenderLink(TextWriter w, string text, string url, string title, string tabIndex, bool isHtmlEncode = true, string opener = "")
        {
            RenderLink(w, "", "", text, "", url, "", "", title, tabIndex, NtfStatus.Empty, false, CallerTypeEnum.Empty, false, isHtmlEncode, true, opener);
        }

        /// <summary>
        ///     Отрисовка в поток гиперссылки
        /// </summary>
        /// <param name="w">Поток</param>
        /// <param name="text">Текст гиперссылки</param>
        /// <param name="url">Ссылка, которая будет открыта через window.open</param>
        /// <param name="urlParams">Параметры окна для window.open</param>
        /// <param name="urlTarget">Handler окна для window.open</param>
        /// <param name="tabIndex">Индекс табуляции</param>
        public void RenderLink(TextWriter w, string text, string url, string urlParams, string urlTarget, string title,  string tabIndex, string opener="")
        {
            RenderLink(w, "", "", text, "", url, urlParams, urlTarget, title, tabIndex, NtfStatus.Empty, false, CallerTypeEnum.Empty, false, true, true, opener);
        }

        /// <summary>
        ///     Отрисовка в поток гиперссылки
        /// </summary>
        /// <param name="w">Поток</param>
        /// <param name="id">Идентификатор контрола</param>
        /// <param name="value">Значение контрола, используется для передачи в звонилку</param>
        /// <param name="text">Текст гиперссылки</param>
        /// <param name="onClick">Javascript-функция, которая вызывается на Click по гиперссылке</param>
        /// <param name="url">Ссылка, которая будет открыта через window.open</param>
        /// <param name="urlParams">Параметры окна для window.open</param>
        /// <param name="urlTarget">Handler окна для window.open</param>
        /// <param name="tabIndex">Индекс табуляции</param>
        /// <param name="callerType">Если не пустой, то определяет тип получаемых контактов для звонилки</param>
        public void RenderLink(TextWriter w, string id, string value, string text, string onClick, string url,
            string urlParams, string urlTarget, string title = "", string tabIndex = "", NtfStatus ntf = NtfStatus.Empty, bool isNtf = false,
            CallerTypeEnum callerType = CallerTypeEnum.Empty, bool isNoWrap = false, bool isHtmlEncode = true, bool isSetToolTip = true, string opener = "")
        {
            if (onClick.Length > 0 && url.Length > 0)
                throw new ArgumentException("Некорректно переданы парметры!");

            w.Write("<a ");

            if (id.Length > 0)
                w.Write(" id=\"{0}\" ", id);

            if (onClick.Length > 0)
                w.Write(" onclick=\"{0}\" ", HttpUtility.JavaScriptStringEncode(onClick));

            if (title.Length > 0)
                w.Write($" title=\"{title}\"");


            if (url.Length > 0)
            {
                opener = string.IsNullOrEmpty(opener) ? (string.IsNullOrEmpty(id) ? "'link'" : "this") : $"'{opener}'";
                w.Write(" onclick=\"Kesco.windowOpen('{0}','{1}', null, {2});\"",
                    HttpUtility.JavaScriptStringEncode(url),
                    urlTarget.Length == 0 ? "_blank" : urlTarget,
                    opener
                );
            }
            if (tabIndex.Length > 0)
                w.Write(" tabIndex={0} ", tabIndex);

            if (isNoWrap)
                w.Write(" style=\"white-space: nowrap;\"");

            var className = EnumAccessors.GetCssClassByNtfStatus(ntf, isNtf);

            if (!callerType.Equals(CallerTypeEnum.Empty) && value.Length > 0)
            {
                className += " v4_callerControl";
                w.Write(" data-id=\"" +
                        HttpUtility.UrlEncode(value) + "\" caller-type=\"" +
                        (int)callerType + "\"");
            }

            if (className.Length > 0)
                w.Write(" class=\"" + className + " \"");

            w.Write(">");
            w.Write(isHtmlEncode ? HttpUtility.HtmlEncode(text) : text);
            w.Write("</a>");
            if (isSetToolTip) w.Write("<script>v4_setToolTip();</script>");
        }

        #endregion
      

        #region EndLink

        /// <summary>
        ///     Отрисовать закрытие тега ссылки
        /// </summary>
        public void RenderLinkEnd(TextWriter w)
        {
            w.Write("</a>");
        }

        #endregion

        #endregion
    }
}