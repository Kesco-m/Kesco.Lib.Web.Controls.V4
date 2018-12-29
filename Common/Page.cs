using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.UI;
using System.Xml;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.BaseExtention.Enums;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.Entities;
using Kesco.Lib.Entities.Corporate;
using Kesco.Lib.Entities.Documents;
using Kesco.Lib.Entities.Persons;
using Kesco.Lib.Localization;
using Kesco.Lib.Log;
using Kesco.Lib.Web.Comet;
using Kesco.Lib.Web.Controls.V4.Common.DocumentPage;
using Kesco.Lib.Web.Controls.V4.Renderer;
using Kesco.Lib.Web.Settings;

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
            if (!ContainsKey(value.HtmlID))
            {
                Add(value.HtmlID, value);
            }
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
            {
                if (!ContainsKey(value.HtmlID))
                {
                    Add(value.HtmlID, value);
                }
                //else
                //{
                //    throw new ArgumentException("Контрол с HtmlID: " + value.HtmlID + " уже существует в коллекции, добавление не возможно.");
                //}
            }
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
        public bool IsKescoRun = false;
        
        /// <summary>
        /// Параметры строки запроса, полученные при первом запросе GET
        /// </summary>
        protected NameValueCollection CurrentQS;
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

        /// <summary>
        ///     Запись скриптов клиенту
        /// </summary>
        public TextWriter JS = new StringWriter();

        /// <summary>
        ///     Массив "слушателей", используется для пейджинга грида
        /// </summary>
        public ArrayList Listeners = new ArrayList();

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
            EnableViewState = false;
            CurrentUser = new Employee(true);

            IsComet = true;
            
            RegisterCss("/Styles/Kesco.V4/CSS/jquery.qtip.min.css");
            RegisterCss("/Styles/Kesco.V4/CSS/jquery-ui.css");
            RegisterCss("/Styles/Kesco.V4/CSS/Kesco.V4.css");

            if (!IsKescoRun)
            {
                RegisterScript("Kesco.Silver4js",
                    "<script src='/Styles/Kesco.V4/JS/Kesco.Silver4js.js' type='text/javascript'></script>");
                RegisterScript("Silverlight",
                    "<script src='/Styles/Kesco.V4/JS/Silverlight.js' type='text/javascript'></script>");

            }
            RegisterScript("jquery", "<script src='/Styles/Kesco.V4/JS/jquery-1.12.4.min.js' type='text/javascript'></script>");
            //RegisterScript("jquery", "<script src='/Styles/Kesco.V4/JS/jquery-3.3.1.min.js' type='text/javascript'></script>");
            //RegisterScrweb browseript("jquery", "<script src='/Styles/Kesco.V4/JS/jquery-migrate-3.0.1.min.js' type='text/javascript'></script>");
            RegisterScript("jqueryui", "<script src='/Styles/Kesco.V4/JS/jquery-ui.js' type='text/javascript'></script>");
            RegisterScript("jquerycookie", "<script src='/Styles/Kesco.V4/JS/jquery.cookie.js' type='text/javascript'></script>");
            RegisterScript("jqueryqtipmin", "<script src='/Styles/Kesco.V4/JS/jquery.qtip.min.js' type='text/javascript'></script>");
            RegisterScript("jqueryvalidate", "<script src='/Styles/Kesco.V4/JS/jquery.validate.min.js' type='text/javascript'></script>");
            RegisterScript("jquerymask", "<script src='/Styles/Kesco.V4/JS/jquery.ui.mask.js' type='text/javascript'></script>");

            RegisterScript("v4", "<script src='/Styles/Kesco.V4/JS/Kesco.V4.js' type='text/javascript'></script>");
            RegisterScript("Comet", "<script src='/Styles/Kesco.V4/JS/Kesco.Comet.js' type='text/javascript'></script>");
            RegisterScript("ContactRedirector","<script src='/Styles/Kesco.V4/JS/Kesco.ContactRedirector.js' type='text/javascript'></script>");
            RegisterScript("Datepicker", "<script src='/Styles/Kesco.V4/JS/Kesco.Datepicker.js' type='text/javascript'></script>");

            RegisterScript("kescoqtip","<script src='/Styles/Kesco.V4/JS/kesco.qtip.js' type='text/javascript'></script>");


            RegisterScript("Confirm","<script src='/Styles/Kesco.V4/JS/Kesco.Confirm.js' type='text/javascript'></script>");
            RegisterScript("Dialog","<script src='/Styles/Kesco.V4/JS/Kesco.Dialog.js' type='text/javascript'></script>");
            RegisterScript("Menu", "<script src='/Styles/Kesco.V4/JS/Kesco.Menu.js' type='text/javascript'></script>");
            RegisterScript("LocalTime", "<script src='/Styles/Kesco.V4/JS/Kesco.LocalTime.js' type='text/javascript'></script>");

            if (this is DocPage)
                RegisterScript("DocPage", "<script src='/Styles/Kesco.V4/JS/Kesco.DocPage.js' type='text/javascript'></script>");
        }

        /// <summary>
        /// Родительская страница для которой текущая открыта в модальном режиме
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

        ///// <summary>
        /////     ID типа сущности
        ///// </summary>
        //public int TypeId { get; set; }

        /// <summary>
        /// Название сущности
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        ///     Признак возможности редактирования сущности
        /// </summary>
        public bool IsEditable { get; set; }

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
        public bool IsRusLocal
        {
            get { return CurrentUser.Language == "ru"; }
        }

        /// <summary>
        ///     английская локализация клиента
        /// </summary>
        public bool IsEngLocal
        {
            get { return CurrentUser.Language == "en"; }
        }

        /// <summary>
        ///     эстонская локализация клиента
        /// </summary>
        public bool IsEstLocal
        {
            get { return CurrentUser.Language == "et"; }
        }

        /// <summary>
        ///     Признак работы лонгпулинга
        /// </summary>
        public bool IsComet { get; set; }

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
        public Dictionary<string, object> Data { get; private set; }

        /// <summary>
        ///     Установка/снятие для всех контролов на странице свойства "только чтение"
        /// </summary>
        public bool IsReadOnly
        {
            set
            {
                if (V4IsPostBack && value != IsReadOnly)
                {
                    foreach (var c in V4Controls.Values)
                    {
                        if (c.IsReadOnly != value) //чтобы сформировать скрипт для изменения контрола
                        {
                            c.SetPropertyChanged("IsReadOnly");
                        }
                    }
                }
                El["r"] = value ? "1" : "0";
            }
            get { return El["r"] == "1"; }
        }

        /// <summary>
        ///     Отключаем/Включаем ViewState (в текущей реализации ViewState отключен)
        /// </summary>
        public override sealed bool EnableViewState
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
        public Dictionary<string, string> JsScripts { get; private set; }

        /// <summary>
        ///     Коллекция стилей
        /// </summary>
        public List<string> CssScripts { get; private set; }

        /// <summary>
        ///     Запрос
        /// </summary>
        public HttpRequest V4Request { get; set; }

        /// <summary>
        ///     Ответ
        /// </summary>
        public HttpResponse V4Response { get; set; }

        /// <summary>
        ///     Абстрактное свойство, требующее обязательно переопределения на странице и устанавливающее ссылку на справку
        /// </summary>
        protected abstract string HelpUrl { get; set; }

        

        /// <summary>
        ///     Освобождение ресурсов занятых страницей. Блокирует объект Application на время операции
        /// </summary>
        public virtual void V4Dispose(bool redirect = false)
        {
            CometServer.UnregisterClient(IDPage, !redirect);

            Application.Lock();

            if (Application.AllKeys.Contains(IDPage))
                Application.Remove(IDPage);

            Application.UnLock();
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
            {
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Отправка клиенту скрипта с изменениями контролов
        /// </summary>
        public void Flush()
        {
            //Ошибка, SetScriptsAfterContent() ещё не вызывался
            //FocusControl = "";
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
            {
                JS.Write("setTimeout(function () {var objF=gi('" + FocusControl + "'); if (objF) objF.focus();}, 10);");
            }

            JS.Write("v4_setToolTip(); v4_setLocalDateTime();");
        }

        /// <summary>
        ///     обработка закрытия страницы
        /// </summary>
        public void Close()
        {
            PageEventManager.SendEvent(this, "Close", null);
            JS.Write("v4_dropWindow();");
        }

        public void ShowMessageOnPage(string htmlMessage)
        {
            var message =
                String.Format(
                    "$(\"body\").prepend(\"<div class='v4Div-outer-container' id='v4DivErrorOuter'></div>\"); " +
                    "$(\"#v4DivErrorOuter\").append(\"<div class='v4Div-inner-container' id='v4DivErrorInner'></div>\");" +
                    "$(\"#v4DivErrorInner\").append(\"<div class='v4Div-centered-content' style='width:500px;' id='v4DivErrorContent'>" +
                    "{0}" +
                    "</div>\");", HttpUtility.JavaScriptStringEncode(htmlMessage));
            JS.Write(message);
        }

        /// <summary>
        ///     Отрисовка страницы
        /// </summary>
        /// <param name="writer">Поток</param>
        protected override void Render(HtmlTextWriter writer)
        {
            if (!V4IsPostBack)
            {
                if (IsComet)
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
            {
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
        }

        /// <summary>
        ///     Регистрация скриптов на странице
        /// </summary>
        /// <param name="key">Ключ скрипта</param>
        /// <param name="script">Строка скрипта</param>
        public void RegisterScript(string key, string script)
        {
            JsScripts[key] = script;
        }

        /// <summary>
        ///     Регистрация CSS
        /// </summary>
        /// <param name="url">URL к файлу CSS-стилей</param>
        public void RegisterCss(string url)
        {
            if (!CssScripts.Contains(url))
            {
                CssScripts.Add(url);
            }
        }

        /// <summary>
        ///     Обработчик события инициализации страницы
        /// </summary>
        /// <param name="e">Параметр события</param>
        protected override void OnInit(EventArgs e)
        {
            Response.Cache.SetNoStore();
            ReturnId = string.IsNullOrEmpty(Request.QueryString["return"]) ? "" : Request.QueryString["return"];
            PostRequest = Request.HttpMethod.Equals("POST");
            
            if (!string.IsNullOrEmpty(Request.QueryString["clid"]))
            {
                int clid;
                if (int.TryParse(Request.QueryString["clid"], out clid))
                {
                    ClId = clid;
                }
            }

            if (!PostRequest)
            {
                ItemId = Request["ID"].ToInt();
                if (string.IsNullOrEmpty(ItemName))
                {
                    var match = Regex.Match(AppRelativeVirtualPath, BaseExtention.RegexPattern.FileName,
                        RegexOptions.IgnoreCase);

                    ItemName = match.Success ? match.Value : AppRelativeVirtualPath;
                    if (!string.IsNullOrEmpty(ItemName)) ItemName = ItemName.Replace(".","_");
                }

                CurrentQS = Request.QueryString;
            }

            CometAsyncState state = new CometAsyncState(null, null, null);
            state.ClientGuid = IDPage;
            state.Id = ItemId;
            state.Name = ItemName;
            state.Page = this;
            CometServer.RegisterClient(state);

            base.OnInit(e);
        }

        /// <summary>
        ///     Обработчик события загрузки страницы
        /// </summary>
        /// <param name="e">Параметр события</param>
        protected override void OnLoad(EventArgs e)
        {
            if (V4IsPostBack) return;

            GetV4Controls(this);
            base.OnLoad(e);
            
            if (!IsComet)
            {
                Header.Controls.AddAt(0,
                    new LiteralControl(Environment.NewLine +
                                       "<script type='text/javascript'>v4_isComet = false;</script>" +
                                       Environment.NewLine));
            }
            else
            {
                var script_index = 0;
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
                    //if (s.Key == "XXX" || s.Key == "XXX1")
                    //{
                    //    var isDbsDocument = V4Controls.Select(ctrl => ((V4Control) ctrl.Value).GetType().Name).Any(x => x == "DBSDocument");
                    //    if (!(this is DocumentPage.DocPage) && !isDbsDocument) continue;
                    //}

                    sbScripts.Append(s.Value);
                    sbScripts.Append(Environment.NewLine);
                }

                sbScripts.Append("<script type='text/javascript'>");
                sbScripts.Append(Environment.NewLine);
                sbScripts.AppendFormat("v4_domain ='{0}';", Config.domain);
                sbScripts.Append(Environment.NewLine);
                sbScripts.AppendFormat("var kesco_ip = '{0}';", Request.ServerVariables["REMOTE_ADDR"]);
                sbScripts.Append(Environment.NewLine);
                sbScripts.Append("var isChanged = false;");
                sbScripts.Append(Environment.NewLine);
                sbScripts.Append("var isValidate = false;");
                sbScripts.Append(Environment.NewLine);
                sbScripts.AppendFormat("var isEditable = '{0}';", IsEditable ? "true" : "false");
                sbScripts.Append(Environment.NewLine);
                sbScripts.AppendFormat("var v4_ItemId = '{0}';", ItemId);
                sbScripts.Append(Environment.NewLine);
                sbScripts.AppendFormat("var v4_ItemName = '{0}';", ItemName);
                sbScripts.Append(Environment.NewLine);
                sbScripts.Append("</script>");
                sbScripts.Append(Environment.NewLine);

                //Эти скрипты добавляются в начало заголовка
                Header.Controls.AddAt(script_index++, new LiteralControl(sbScripts.ToString()));

                var sb = new StringBuilder(Environment.NewLine + "<script type='text/javascript'>");
                sb.Append(Environment.NewLine);
                sb.AppendFormat("var idp='{0}'; var v4_cometUrl='{1}';", HttpUtility.JavaScriptStringEncode(IDPage),
                    HttpUtility.JavaScriptStringEncode(Request.ApplicationPath));
                sb.Append(Environment.NewLine);

                if (!string.IsNullOrEmpty(FocusControl))
                {
                    sb.AppendFormat("setTimeout(function () {{ var objF=gi('{0}'); if (objF) objF.focus(); }}, 10 );",
                        FocusControl);
                    sb.Append(Environment.NewLine);
                }

               
                sb.Append("function v4_tooltipCaller() {");
                sb.Append(Environment.NewLine);
                sb.Append("var dataId=$(this)[0].getAttribute('data-id');");
                sb.Append(Environment.NewLine);
                sb.Append("var callerType=$(this)[0].getAttribute('caller-type');");
                sb.Append(Environment.NewLine);
                sb.Append("if(dataId==null || dataId=='') return '';");
                sb.Append(Environment.NewLine);
                sb.Append("if(callerType==null) callerType='';");
                sb.Append(Environment.NewLine);
                sb.AppendFormat(
                    "return '{0}?lang={1}&parentidp={2}&force=1&id='+dataId+'&callerType=' + callerType + '&computerName=' + v4_clientName;",
                    Config.contacts, CurrentUser.Language, IDPage);
                sb.Append(Environment.NewLine);
                sb.Append("}");
                sb.Append(Environment.NewLine);
                sb.Append("function v4_startup(){");
                sb.Append(Environment.NewLine);
                sb.Append(JS);
                sb.Append("}");
                sb.Append(Environment.NewLine);
                sb.Append(
                    "v4_addLoadEvent(v4_startup); v4_addLoadEvent(v4_setLocalDateTime);");
                sb.Append(Environment.NewLine);
                sb.AppendFormat("v4_helpURL = '{0}';", HttpUtility.JavaScriptStringEncode(HelpUrl));
                sb.Append(Environment.NewLine);
                sb.Append("</script>" + Environment.NewLine);

                //Эти скрипты добавляются в конец заголовка
                Header.Controls.Add(new LiteralControl(sb.ToString()));
            }
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
            if (V4Request.Params.Count == 0)
            {
                return;
            }
            var key = V4Request.Params.Keys[1];
            switch (key)
            {
                case "ctrl":
                    if (!V4Controls.ContainsKey(V4Request.Params["ctrl"]))
                    {
                        throw new Exception("Control is not found id:" + V4Request.Params["ctrl"] + " Page:" +
                                            V4Request.RawUrl);
                    }

                    V4Controls[V4Request.Params["ctrl"]].ProcessCommand(V4Request.Params);

                    if(ItemId!=0)
                        TranslateCtrlEvent(V4Request.Params);
                    break;

                case "page":
                    switch (V4Request.Params["page"])
                    {
                        case "close":
                            if (OnBeforeClose())
                            {
                                var nvc = new NameValueCollection {{"idp", IDPage}};
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
            JS.Write("isValidate={0};", V4ValidationBeforeExit().ToString(CultureInfo.InvariantCulture).ToLower());
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
                        var id = param["Ctrl"];
                        if (!String.IsNullOrEmpty(id) && V4Controls.Keys.Contains(id))
                            RefreshHtmlBlock(id, V4Controls[id].RenderControl);
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
                }
            }
            catch (Exception  ex)
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
                if (!ctrl.Validation())
                {
                    return false;
                }
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
            JS.Write(WebExtention.DynamicLink(url,false));
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
        ///     Рендеринг надписей
        /// </summary>
        /// <param name="w">Исходный поток для записи HTML - кода</param>
        /// <param name="ntf">Список надписей</param>
        public void RenderNtf(TextWriter w, List<string> ntf)
        {
            RenderNtf(w, ntf, NtfStatus.Error);
        }

        /// <summary>
        ///     Рендеринг надписей
        /// </summary>
        public void RenderNtf(TextWriter w, List<string> ntf, NtfStatus status)
        {
            RenderNtf(w, ntf, status, "", "", true);
        }

        /// <summary>
        ///     Рендеринг надписей
        /// </summary>
        /// <param name="w">Исходный поток для записи HTML - кода</param>
        /// <param name="ntf">Список надписей</param>
        /// <param name="status">Статус надписей</param>
        /// <param name="divId">Идентификатор div</param>
        /// <param name="dashSpace">Рисовать ли тире</param>
        public void RenderNtf(TextWriter w, List<string> ntf, NtfStatus status, string divId, string className, bool dashSpace)
        {
            var n = ntf.Count;
            var color = EnumAccessors.GetColorByNtfStatus(status);
            var _dashSpace = dashSpace ? " - " : "";
            if (n > 0)
            {
                w.Write("<div {2} {3} style=\"color:{0} !important;;font-size:7pt !important;font-family:Verdana;font-weight:normal;\">{1}", 
                    color, 
                    _dashSpace, 
                    string.IsNullOrEmpty(divId) ? "" : "id=\"" + divId + "\"",
                    string.IsNullOrEmpty(className) ? "" : "class=\"" + className + "\""
                    );
            }
            for (var i = 0; i < n; i++)
            {
                var n4check = ntf[i];
                if (n4check == null) continue;
                if (n4check.IndexOf("<ns>", StringComparison.Ordinal) != -1)
                {
                    n4check = n4check.Remove(0, 4);
                    n4check = " " + _dashSpace + n4check.Replace("<ns>", "<br>" + _dashSpace);
                    w.Write(n4check);
                }
                else
                {
                    if (i > 0)
                    {
                        w.Write("<BR>");
                        w.Write(_dashSpace);
                    }
                    w.Write(ntf[i]);
                }
            }
            if (n > 0) w.Write("</div>");
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
                    if (!String.IsNullOrEmpty(target.ToString()))
                    {
                        result += target + "<br />";
                    }
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
                if (!ctrl.IsReadOnly && ctrl.IsRequired && ctrl.Value.Length == 0)
                {
                    return false;
                }
                if (!ctrl.Validation())
                {
                    return false;
                }
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
            {
                foreach (string key in ElXML.Keys)
                {
                    var tmpNodeXml = docXml.CreateElement("elXML");
                    var idAttr = docXml.CreateAttribute("i");
                    idAttr.Value = key;
                    tmpNodeXml.Attributes.Append(idAttr);
                    tmpNodeXml.InnerXml = ElXML[key];
                    root.AppendChild(tmpNodeXml);
                }
            }

            if (HTMLBlock.Count > 0)
            {
                foreach (string key in HTMLBlock.Keys)
                {
                    var n = docXml.CreateElement("v4html");
                    var a = docXml.CreateAttribute("i");
                    a.Value = key;
                    n.Attributes.Append(a);
                    n.AppendChild(docXml.CreateCDataSection(HTMLBlock[key]));
                    root.AppendChild(n);
                }
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
            if (String.IsNullOrEmpty(title)) title = Resx.GetString("alertMessage");
            var ctrlId = "";
            if (ctrlFocus != null)
                ctrlId = ctrlFocus.GetFocusControl();

            ShowMessage(message, title, MessageStatus.Information, ctrlId);
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
            int? height)
        {
            JS.Write("v4_showMessage(\"{0}\",\"{1}\",{2},\"{3}\",{4},{5});",
                HttpUtility.JavaScriptStringEncode(message.Replace(Environment.NewLine, "<br>")),
                HttpUtility.JavaScriptStringEncode(title), (int) status, ctrlIdFocus,
                width == null ? "null" : width.ToString(), height == null ? "null" : height.ToString());
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
        public void ShowConfirm(string message, string title, string captionYes, string captionNo, string callbackYes, string callbackNo,
            string ctrlIdFocus, int? width)
        {
            ShowConfirm(message, title, captionYes, captionNo, callbackYes, callbackNo, ctrlIdFocus, 75, 75, width, null);
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
            ShowConfirm(message, title, captionYes, captionNo, callbackYes, "", ctrlIdFocus, widthYes, widthNo, width, height);
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
        public void ShowConfirm(string message, string title, string captionYes, string captionNo, string callbackYes, string callbackNo,
            string ctrlIdFocus, int? widthYes, int? widthNo, int? width, int? height)
        {
            JS.Write("v4_showConfirm(\"{0}\",\"{1}\",\"{2}\",\"{3}\",{4},{5},\"{6}\",\"{7}\",\"{8}\",{9},{10});",
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
                height == null ? "null" : height.ToString());
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
            JS.Write("v4_showRecalc(\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",{6},{7},{8},{9},\"{10}\",\"{11}\",\"{12}\",\"{13}\",\"{14}\",{15},{16});",
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
                if (ctrl.BindingField.Length == 0)
                {
                    continue;
                }

                if (ctrl.BindSimple(obj, direction))
                {
                    changed = true;
                }
            }
            return changed;
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
            {
                Hide(idControl);
            }
            else if (string.IsNullOrEmpty(displayType))
            {
                Display(idControl);
            }
            else
            {
                JS.Write("di('{0}','{1}');", idControl, displayType);
            }
        }

        /// <summary>
        ///     Добавить скрипт: Показать элемент
        /// </summary>
        /// <param name="idControl">ID элемента управления</param>
        /// <param name="isVisible">Маркер видимости элемента</param>
        public void Display(string idControl, bool isVisible)
        {
            if (isVisible)
            {
                Display(idControl);
            }
            else
            {
                Hide(idControl);
            }
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
        ///     Отрисовать ссылку на ресурс
        /// </summary>
        public void RenderLinkResource(TextWriter w, string idRes, string tabIndex = "")
        {
            var resUrl = Config.resource_form;
            w.Write(
                "<a href=\"#\" onclick=\"v4_windowOpen('{0}{1}id={2}');\"{3}>",
                resUrl, (resUrl.IndexOf('?') == -1) ? "?" : "&", idRes,
                tabIndex.Length > 0 ? " tabIndex=" + tabIndex : "");
        }

        /// <summary>
        ///     Отрисовать ссылку на расположение
        /// </summary>
        public void RenderLinkLocation(TextWriter w, string idLoc, string tabIndex = "")
        {
            var resUrl = Config.location_search;
            w.Write(
                "<a href=\"#\" onclick=\"v4_windowOpen('{0}{1}idLoc={2}');\"{3}>",
                resUrl, (resUrl.IndexOf('?') == -1) ? "?" : "&", idLoc,
                tabIndex.Length > 0 ? " tabIndex=" + tabIndex : "");
        }

        public void RenderLinkLocation(TextWriter w, string id, string value, string text, NtfStatus ntf = NtfStatus.Empty, string tabIndex = "")
        {
            RenderLink(w, "hrefLoc"+ id, "", text, "", Config.location_search + ((Config.location_search.IndexOf('?') == -1) ? "?" : "&") + "id=" + value, "", "", tabIndex, ntf);
        }

        /// <summary>
        ///     Отрисовать ссылку на оборудование
        /// </summary>
        public void RenderLinkEquipment(TextWriter w, string id, string className, string title)
        {
            w.Write(
                  "<a href=\"#\" {2} {3} onclick=\"v4_windowOpen('{0}?id={1}');\">",
                  Config.equipment_form , id, className.Length > 0 ? className : "", title.Length > 0 ? title : "");
        }

        /// <summary>
        ///   Отрисовать ссылку на документ
        /// </summary>
        /// <param name="w">Поток вывода</param>
        /// <param name="docId">КодДокумента</param>
        public void RenderLinkDocument(TextWriter w, int docId, bool openImage = false, NtfStatus ntf = NtfStatus.Empty)
        {
            var ntfClass = EnumAccessors.GetCssClassByNtfStatus(ntf);

            w.Write("<a {0} href=\"javascript:void(0);\" onclick=\"cmdasync('cmd', 'ShowInDocView', 'DocId', {1}, 'openImage', {2});\">",
                ntfClass != "" ? string.Format("class='{0}'", ntfClass) : "",
                docId,
                openImage ? 1 : 0);
        }

        /// <summary>
        ///     Отрисовать закрытие тега ссылки
        /// </summary>
        public void RenderLinkEnd(TextWriter w)
        {
            w.Write("</a>");
        }

        /// <summary>
        ///     Восстановление курсора по-умолчанию после асинхронного вызова
        /// </summary>
        public void RestoreCursor()
        {
            JS.Write("Wait.render(false);");
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

        #region RendeLink

        /// <summary>
        ///     Отрисовка ссылки на сотрудника
        /// </summary>
        /// <param name="w">Поток</param>
        /// <param name="emplId">КодСотрудника</param>
        /// <param name="emplName">Название сотрудника</param>
        public void RenderLinkEmployee(TextWriter w, string htmlId, string emplId, string emplName, NtfStatus ntf)
        {
            var url = string.Concat(Config.user_form, "?id=", emplId);
            RenderLinkCaller(w, htmlId, emplId, emplName, url, ntf, CallerTypeEnum.Employee);
        }

        /// <summary>
        ///     Отрисовка ссылки на сотрудника
        /// </summary>
        public void RenderLinkEmployee(TextWriter w, string htmlId, Employee empl, NtfStatus ntf)
        {
            var name = IsRusLocal ? empl.FullName : empl.FullNameEn;
            RenderLinkEmployee(w, htmlId, empl.Id, name, ntf);
        }

        /// <summary>
        ///     Отсисовка ссылки на лицо
        /// </summary>
        /// <param name="w">Поток</param>
        /// <param name="personId">КодЛица</param>
        /// <param name="personName">Название лица</param>
        public void RenderLinkPerson(TextWriter w, string htmlId, string personId, string personName,
            NtfStatus ntf = NtfStatus.Empty)
        {
            var url = string.Concat(Config.person_form, "?id=", personId);
            RenderLinkCaller(w, htmlId, personId, personName, url, ntf, CallerTypeEnum.Person);
        }


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
        public void RenderLinkCaller(TextWriter w, string id, string value, string text, string url, string urlParams,
            string urlTarget, string tabIndex, CallerTypeEnum callerType)
        {
            RenderLink(w, id, value, text, "", url, urlParams, urlTarget, tabIndex, NtfStatus.Empty, callerType);
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
        public void RenderLinkCaller(TextWriter w, string value, string text, string url, string urlParams,
            string urlTarget, string tabIndex, CallerTypeEnum callerType)
        {
            RenderLink(w, "", value, text, "", url, urlParams, urlTarget, tabIndex, NtfStatus.Empty, callerType);
        }

        /// <summary>
        ///     Отрисовка в поток гиперссылки со звонилкой
        /// </summary>
        /// <param name="w">Поток</param>
        /// <param name="value">Значение контрола, используется для передачи в звонилку</param>
        /// <param name="text">Текст гиперссылки</param>
        /// <param name="url">Ссылка, которая будет открыта через window.open</param>
        /// <param name="callerType">Если не пустой, то определяет тип получаемых контактов для звонилки</param>
        public void RenderLinkCaller(TextWriter w, string htmlId, string value, string text, string url, NtfStatus ntf,
            CallerTypeEnum callerType)
        {
            RenderLink(w, htmlId, value, text, "", url, "", "", "", ntf, callerType);
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
        public void RenderLinkCaller(TextWriter w, string id, string value, string text, string onClick, string tabIndex,
            CallerTypeEnum callerType)
        {
            RenderLink(w, id, value, text, onClick, "", "", "", tabIndex, NtfStatus.Empty, callerType);
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
        public void RenderLinkCaller(TextWriter w, string value, string text, string onClick, string tabIndex,
            CallerTypeEnum callerType)
        {
            RenderLink(w, "", value, text, onClick, "", "", "", tabIndex, NtfStatus.Empty, callerType);
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
        public void RenderLink(TextWriter w, string text, string url, string urlParams, string urlTarget,
            string tabIndex)
        {
            RenderLink(w, "", "", text, "", url, urlParams, urlTarget, tabIndex);
        }

        /// <summary>
        ///     Отрисовка в поток гиперссылки
        /// </summary>
        /// <param name="w">Поток</param>
        /// <param name="text">Текст гиперссылки</param>
        /// <param name="url">Ссылка, которая будет открыта через window.open</param>
        /// <param name="tabIndex">Индекс табуляции</param>
        public void RenderLink(TextWriter w, string text, string url, string tabIndex)
        {
            RenderLink(w, "", "", text, "", url, "", "", tabIndex);
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
            string urlParams, string urlTarget, string tabIndex, NtfStatus ntf = NtfStatus.Empty,
            CallerTypeEnum callerType = CallerTypeEnum.Empty)
        {
            if (onClick.Length > 0 && url.Length > 0)
                throw new ArgumentException("Некорректно переданы парметры!");

            w.Write("<a href=\"javascript:void(0);\"");

            if (id.Length > 0)
                w.Write(" id=\"{0}\" ", id);

            if (onClick.Length > 0)
                w.Write(" onclick=\"{0}\" ", HttpUtility.JavaScriptStringEncode(onClick));

            if (url.Length > 0)
                w.Write("onclick=\"v4_windowOpen('{0}','{1}','{2}');\"",
                    HttpUtility.JavaScriptStringEncode(url),
                    urlTarget.Length == 0 ? "_blank" : urlTarget,
                      urlParams.Length == 0
                        ? "location=no, menubar=no, status=no, toolbar=no, resizable=yes, scrollbars=yes"
                        : urlParams
                    );
                
            if (tabIndex.Length > 0)
                w.Write("tabIndex={0} ", tabIndex);

           
            var className = EnumAccessors.GetCssClassByNtfStatus(ntf);

            if (!callerType.Equals(CallerTypeEnum.Empty) && value.Length > 0)
            {
                className += " v4_callerControl";
                w.Write(" data-id=\"" +
                        HttpUtility.UrlEncode(value) + "\" caller-type=\"" +
                        (int) callerType + "\"");
            }

            if (className.Length > 0)
                w.Write(" class=\"" + className + " \"");

            w.Write(">");
            w.Write(HttpUtility.HtmlEncode(text));
            w.Write("</a>");
        }

        #endregion

        public void TranslateCtrlEvent(NameValueCollection Params)
        {
            CometMessage m = new CometMessage
            {
                ClientGuid = IDPage,
                IsV4Script = true,
                Message = "<js>cmdasync();</js>",
                Status = 0,
                UserName = ""
            };

            Predicate<CometAsyncState> pred = (client) =>
            {
                if (client.Page == null) return false;
                if (client.Page == this) return false;
                if (((Page)client.Page).IDPage == IDPage) return false;
                if (client.Id != this.ItemId) return false;


                string ctrl_id = Params["ctrl"];
                //((Page)client.Page).V4Controls[Params["ctrl"]].ProcessCommand(Params);
                if (!((Page) client.Page).V4Controls.ContainsKey(ctrl_id)) return false;

                string old = ((Page)client.Page).V4Controls[ctrl_id].Value;
                ((Page)client.Page).V4Controls[ctrl_id].Value = V4Controls[ctrl_id].Value;
                ((Page)client.Page).V4Controls[ctrl_id].OnChanged(new ProperyChangedEventArgs(old, V4Controls[ctrl_id].Value));
                return true;
            };

            CometServer.PushMessage(m, pred);
            CometServer.Process();
        }

        private List<V4PageObj> objList { get; set; }
        private List<V4PageObj> ObjList
        {
            get
            {
                if (objList != null)
                {
                    return objList;
                }

                objList = new List<V4PageObj>();
                return objList;
            }
        }

        public Entity GetObjectById(Type t, string id)
        {
            var ret = ObjList.Find(o => (o.Type == t || t == typeof(Document)) && o.Object.Id == id);

            if (ret != null  
                && V4IsPostBack
                && (string.IsNullOrEmpty(ret.Object.CurrentPostRequest) || ret.Object.CurrentPostRequest != IDPostRequest)
                && ret.Object.GetLastChanged(id) != ret.Object.Changed)
            {
                objList.Remove(ret);
                ret = null;
            }
            
            if (ret == null)
            {
                var ci = t.GetConstructor(new Type[] { typeof(string) });
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
                if (string.IsNullOrEmpty(ret.Object.CurrentPostRequest) || ret.Object.CurrentPostRequest != IDPostRequest)
                {
                    ret.Object.CurrentPostRequest = IDPostRequest;
                }
            }

            if (ret != null)
            {
                return ret.Object;
            }

            return null;
        }

        //Удаляем все закешированные объекты на странице
        public void ClearCacheObjects()
        {
            if (objList != null) objList.Clear();
            objList = null;
            GC.Collect();
        }

    }
}