using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.BaseExtention.BindModels;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.Entities.Documents;
using Kesco.Lib.Localization;
using Kesco.Lib.Web.Controls.V4.Binding;
using Page = Kesco.Lib.Web.Controls.V4.Common.Page;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Событие значение контрола изменено
    /// </summary>
    /// <param name="sender">контрол</param>
    /// <param name="e">аргумент</param>
    public delegate void ChangedEventHandler(object sender, ProperyChangedEventArgs e);

    /// <summary>
    ///     Событие удаление значения из списка
    /// </summary>
    /// <param name="sender">контрол</param>
    /// <param name="e">аргумент</param>
    public delegate void DeletedEventHandler(object sender, ProperyDeletedEventArgs e);

    /// <summary>
    ///     Делегат отрисовки контрола в режиме только чтение
    /// </summary>
    /// <param name="w">Поток</param>
    public delegate void VoidTextWriterDelegate(TextWriter w);

    /// <summary>
    ///     Делегат для работы с текстовым представлением контрола
    /// </summary>
    /// <param name="id">идентификатор</param>
    public delegate void VoidStringDelegate(string id);

    /// <summary>
    ///     Делегат для работы с текстовым представлением контрола поиска
    /// </summary>
    /// <param name="searchText">строка поиска</param>
    /// <param name="id">идентификатор</param>
    public delegate void VoidStringStringDelegate(string searchText, string id);

    /// <summary>
    ///     Делегат для работы с текстовым представлением контрола
    /// </summary>
    /// <param name="param">параметр</param>
    public delegate string StringStringDelegate(string param);

    /// <summary>
    ///     Заполнение списка
    /// </summary>
    public delegate IEnumerable EnumerableVoidDelegate();

    /// <summary>
    ///     Заполнение списка контрола Select
    /// </summary>
    /// <param name="idParent">Строка поиска</param>
    public delegate IEnumerable EnumerableStringDelegate(string idParent);

    /// <summary>
    ///     Делегат отрисовки нотификаций контрола
    /// </summary>
    /// <param name="sender">контрол</param>
    /// <param name="ntf">нотификация</param>
    public delegate void RenderNtfDelegate(object sender, Ntf ntf);

    /// <summary>
    ///     Делегат для работы с текстовым представлением контрола
    /// </summary>
    public delegate IList<string> ListStringStringDelegate();

    /// <summary>
    ///     Получение сущности по ID
    /// </summary>
    /// <param name="id">идентификатор</param>
    public delegate object ObjectStringDelegate(string id, string name = "");

    /// <summary>
    ///     Структура нотификации контрола
    /// </summary>
    public struct NtfData
    {
        /// <summary>
        ///     Цвет нотификации
        /// </summary>
        public string Color;

        /// <summary>
        ///     Класс CSS нотификации
        /// </summary>
        public string CssClass;

        /// <summary>
        ///     Статус нотификации
        /// </summary>
        public NtfStatus NtfStatus;

        /// <summary>
        ///     Текст нотификации
        /// </summary>
        public string Text;
    }

    /// <summary>
    ///     Класс нотификации контрола
    /// </summary>
    public class Ntf
    {
        /// <summary>
        ///     Конструктор
        /// </summary>
        public Ntf()
        {
            List = new List<NtfData>();
        }

        /// <summary>
        ///     Коллекция нотификаций контрола
        /// </summary>
        public List<NtfData> List { get; private set; }

        /// <summary>
        /// </summary>
        public bool Contains(string text)
        {
            return List.Exists(l => l.Text == text);
        }

        /// <summary>
        ///     Удаляет все элементы из коллекции
        /// </summary>
        public void Clear()
        {
            List.Clear();
        }

        /// <summary>
        ///     Метод добавления нотификации (только текст)
        /// </summary>
        /// <param name="text">Текст</param>
        public void Add(string text)
        {
            Add(text, null, null);
        }

        /// <summary>
        ///     Метод добавления нотификации (только текст и статус)
        /// </summary>
        /// <param name="text">Текст</param>
        public void Add(string text, NtfStatus status)
        {
            if(string.IsNullOrEmpty(text))
                return;

            var cssClass = "v4NtfError";
            if (status == NtfStatus.Information) cssClass = "v4NtfInformation";
            else if (status == NtfStatus.Recommended) cssClass = "v4NtfRecommended";

            Add(text, null, cssClass, status);
        }

        /// <summary>
        ///     Метод добавления нотификации (текст и цвет)
        /// </summary>
        /// <param name="text">текст</param>
        /// <param name="color">цвет</param>
        public void Add(string text, string color)
        {
            Add(text, color, null);
        }

        /// <summary>
        ///     Метод добавления нотификации (текст, цвет, класс CSS)
        /// </summary>
        /// <param name="text">текст</param>
        /// <param name="color">цвет</param>
        /// <param name="cssClass">класс CSS</param>
        public void Add(string text, string color, string cssClass, NtfStatus status = NtfStatus.Information)
        {
            List.Add(new NtfData {Text = text, Color = color, CssClass = cssClass, NtfStatus = status});
        }
    }

    /// <summary>
    ///     Базовый класс всех контролов версии 4
    /// </summary>
       
    public abstract class V4Control : Control
    {
        /// <summary>
        ///     Путь к картинкам (папка STYLES) из web.config
        /// </summary>
        public static string PathPic = "/Styles/";

        private string _bindingField = "";

        /// <summary>
        ///     Наименование стиля вывода на экран контрола V4, по-умолчанию "inline-block"
        /// </summary>
        private string _displayCaptionStyle = "inline-block";

        /// <summary>
        ///     Стиль вывода на экран контрола V4, по-умолчанию "inline-table"
        /// </summary>
        private string _displayStyle = "inline-table";

        private string _htmlID = "";
        private bool _isDisabled;
        private bool _isReadOnly;
        private bool _isRequired;
        private bool _refreshRequired;
        private string _value = "";

        /// <summary>
        ///     Коллекция изменений свойств контрола
        /// </summary>
        public List<string> PropertyChanged = new List<string>();

        /// <summary>
        ///     Словарь атрибутов
        /// </summary>
        public Dictionary<string, string> V4Attributes = new Dictionary<string, string>();

        /// <summary>
        ///     Отрисовка контрола в режиме "только чтение"
        /// </summary>
        public VoidTextWriterDelegate V4RenderControlReadOnly;

        /// <summary>
        ///     Коллекция сообщений валидации
        /// </summary>
        public NameValueCollection ValidationMsg = new NameValueCollection();

        /// <summary>
        ///     Конструктор
        /// </summary>
        protected V4Control()
        {
            Width = new Unit("100%");
            RenderContainer = true;
            Title = "";
            BindingField = "";
            CSSClass = "";
            V4RenderControlReadOnly = RenderControlReadOnly;
        }

        /// <summary>
        ///     Локализация
        /// </summary>
        public ResourceManager Resx
        {
            get
            {
                if (V4Page != null)
                    return V4Page.Resx;
                return Resources.Resx;
            }
        }

        /// <summary>
        ///     Признак отрисовки контейнера контрола
        /// </summary>
        public bool RenderContainer { get; set; }

        /// <summary>
        ///     Признак использования условий для фильтра
        /// </summary>
        public bool IsUseCondition { get; set; }

        /// <summary>
        ///     Признак составного контрола
        /// </summary>
        public bool IsComposite { get; set; }

        /// <summary>
        ///     Признак запрета выбора в условиях фильтра "значение не указано" и "любое значение"
        /// </summary>
        public bool IsNotUseEmpty { get; set; }

        /// <summary>
        ///     Дополнительные пользовательские данные
        /// </summary>
        public object UserData { get; set; }

        /// <summary>
        ///     Признак использования фильтра в запросах
        /// </summary>
        public bool IsCanUseFilter { get; set; }

        /// <summary>
        ///     Признак интервала в фильтрах
        /// </summary>
        public bool IsInterval { get; set; }

        /// <summary>
        ///     Наименование логической сущности. Используется для построения списка выбранных условий поиска.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///     Наименование логической сущности для построение справки.
        /// </summary>
        public string Help { get; set; }

        /// <summary>
        ///     Наименование логической сущности для отображения слева от контрола.
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        ///     Признак использования приложения Контакты
        /// </summary>
        public bool IsCaller { get; set; }

        /// <summary>
        ///     Что за контакты  отображаются в контроле
        /// </summary>
        public CallerTypeEnum CallerType { get; set; }

        /// <summary>
        ///     Стиль вывода на экран контрола V4, по-умолчанию "inline-table"
        /// </summary>
        public string DisplayStyle
        {
            get { return _displayStyle; }
            set { _displayStyle = value; }
        }

        /// <summary>
        ///     Доступ к наименованию стиля вывода на экран контрола V4, по-умолчанию "inline-block"
        /// </summary>
        public string DisplayCaptionStyle
        {
            get { return _displayCaptionStyle; }
            set { _displayCaptionStyle = value; }
        }

        /// <summary>
        ///     Коллекция скриптов
        /// </summary>
        public TextWriter JS
        {
            get { return V4Page.JS; }
        }

        /// <summary>
        ///     Id контрола для установки фокуса
        /// </summary>
        public string NextControl { get; set; }

        /// <summary>
        ///     Клиентский ID контрола
        /// </summary>
        public virtual string HtmlID
        {
            set { _htmlID = value; }
            get { return _htmlID.Length == 0 ? (_htmlID = ID) : _htmlID; }
        }

        /// <summary>
        ///     Индекс табуляции
        /// </summary>
        public int? TabIndex { get; set; }

        /// <summary>
        ///     Тайтл контрола
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        ///     CSS класс контрола
        /// </summary>
        public string CSSClass { get; set; }

        /// <summary>
        ///     Связанное поле контрола
        /// </summary>
        public string BindingField
        {
            set { _bindingField = value; }
            get { return _bindingField; }
        }

        /// <summary>
        ///     Признак имплементации события Changed
        /// </summary>
        public bool AttachedChangedEventHandler
        {
            get { return Changed != null; }
        }

        /// <summary>
        ///     Признак обязательности заполнения
        /// </summary>
        public bool IsRequired
        {
            get { return _isRequired; }
            set
            {
                if (_isRequired != value)
                {
                    SetPropertyChanged("IsRequired");
                }
                _isRequired = value;
            }
        }

        /// <summary>
        ///     Признак режима "Только чтение"
        /// </summary>
        public bool IsReadOnly
        {
            get { return _isReadOnly; }
            set
            {
                if (_isReadOnly != value)
                {
                    SetPropertyChanged("IsReadOnly");
                }
                _isReadOnly = value;
            }
        }

        /// <summary>
        ///     Принудительно перерисовать значение контрола
        /// </summary>
        public bool RefreshRequired
        {
            get { return _refreshRequired; }
            set
            {
                 if (value) SetPropertyChanged("RefreshRequired");
                _refreshRequired = value;
            }
        }

        /// <summary>
        ///     Признак "контрол отключен"
        /// </summary>
        public bool IsDisabled
        {
            get { return _isDisabled; }
            set
            {
                if (_isDisabled != value)
                {
                    SetPropertyChanged("IsDisabled");
                }
                _isDisabled = value;
            }
        }

        /// <summary>
        ///     Ширина
        /// </summary>
        public Unit Width { get; set; }

        /// <summary>
        ///     Высота
        /// </summary>
        public Unit Height { get; set; }

        /// <summary>
        ///     Акцессор базового класса V4.Page
        /// </summary>
        public Page V4Page
        {
            get { return Page as Page; }
            set { Page = value; }
        }

        /// <summary>
        ///     Значение контрола
        /// </summary>
        public virtual string Value
        {
            get { return _value; }
            set
            {
                value = value.TrimNoNullError();
                var changed = _value != value;
                var oldValue = _value;
                _value = value;

                if (changed)
                {
                    SetPropertyChanged("Value");
                    OnValueChanged(new ValueChangedEventArgs(value, oldValue));
                }
            }
        }

        /// <summary>
        ///     Признак видимости контрола
        /// </summary>
        public override bool Visible
        {
            get { return base.Visible; }
            set
            {
                if (base.Visible != value)
                {
                    SetPropertyChanged("Visible");
                    base.Visible = value;
                }
            }
        }

        /// <summary>
        ///     Метод добавления изменения свойства контрола в коллекцию изменений
        /// </summary>
        /// <param name="propertyName">Наименование свойства</param>
        public void SetPropertyChanged(string propertyName)
        {
            if (!PropertyChanged.Contains(propertyName))
            {
                PropertyChanged.Add(propertyName);
            }
        }

        /// <summary>
        ///     Получение ID контрола на странице для установки фокуса
        /// </summary>
        /// <returns>ID</returns>
        public virtual string GetFocusControl()
        {
            if (IsComposite) return HtmlID;
            return HtmlID + "_0";
        }

        /// <summary>
        ///     Установка фокуса на контрол
        /// </summary>
        public override void Focus()
        {
            V4Page.FocusControl = GetFocusControl();
        }

        /// <summary>
        ///     Инициализация контрола
        /// </summary>
        public virtual void V4OnInit()
        {
        }

        /// <summary>
        ///     Инициализация контрола
        /// </summary>
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            V4OnInit();
        }

        /// <summary>
        ///     Отрисовка контрола в режиме "только чтение"
        /// </summary>
        /// <param name="w">Поток</param>
        public void RenderControlReadOnly(TextWriter w)
        {
            w.Write(HttpUtility.HtmlEncode(Value));
        }

        /// <summary>
        ///     Событие изменения свойств контрола
        /// </summary>
        public event ChangedEventHandler Changed;

        /// <summary>
        ///     Событие изменения Value свойства контрола
        /// </summary>
        public event ValueChangedEventHandler ValueChanged;

        /// <summary>
        ///     Событие удаление значения из списка
        /// </summary>
        public event DeletedEventHandler Deleted;

        /// <summary>
        ///     Событие изменения свойств контрола
        /// </summary>
        /// <param name="e">Инициатор события</param>
        public virtual void OnChanged(ProperyChangedEventArgs e)
        {
            if (Changed != null)
            {
                OnValueChanged(new ValueChangedEventArgs(e.NewValue, e.OldValue));
                Changed(this, e);
            }
            if (OnRenderNtf != null)
            {
                RenderNtf();
            }
        }

        /// <summary>
        ///     Событие изменения Value свойства контрола
        /// </summary>
        public void OnValueChanged(ValueChangedEventArgs e)
        {
            if (ValueChanged != null)
            {
                ValueChanged(this, e);
            }
        }

        /// <summary>
        ///     Событие удаление значения из списка
        /// </summary>
        public virtual void OnDeleted(ProperyDeletedEventArgs e)
        {
            if (Deleted != null)
            {
                Deleted(this, e);
            }
        }

        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        /// <param name="w">Поток</param>
        public override void RenderControl(HtmlTextWriter w)
        {
            RenderControl(w);

            //Необходимо очистить все изменения, которые возможно были сделаны в Page_Load, иначе при выполнении любой следующей серверной команды
            //все изменения сделанные из клиентского кода после загрузки страницы, будут отменены
            PropertyChanged.Clear();
        }

        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        /// <param name="w">Поток</param>
        public virtual void RenderControl(TextWriter w)
        {
            if (!V4Page.V4IsPostBack && !String.IsNullOrEmpty(Caption)) //render caption
            {
                w.Write("<span id='{0}' class='v4caption' style='display:{2}'>{1}</span>",
                    string.Concat(HtmlID, "_cptn"), string.Concat(Caption, ":"), Visible ? DisplayCaptionStyle : "none");
            }
            if (!V4Page.V4IsPostBack) //render body
            {
                w.Write("<div id='{0}'", HtmlID);
                foreach (var attr in V4Attributes)
                {
                    w.Write(" {0}=\"{1}\"", attr.Key, attr.Value);
                }

                w.Write(" style='{0}display:{1}'", Height.IsEmpty ? "" : string.Concat("height:", Height, ";"),
                    Visible ? DisplayStyle : "none");

                if (!string.IsNullOrEmpty(CSSClass))
                {
                    w.Write(" class='{0}'", CSSClass);
                }
                if (!string.IsNullOrEmpty(Help))
                {
                    w.Write(" help='{0}'", Help);
                }
                w.Write(">");
            }
            RenderControlBody(w);
            if (OnRenderNtf != null)
            {
                if (!string.IsNullOrEmpty(NtfDependencies))
                {
                    var ids = NtfDependencies.Split(',');
                    foreach (var id in ids)
                    {
                        var ctrl = V4Page.V4Controls.Values.FirstOrDefault(x => x.ID == id);
                        if (ctrl == null)
                        {
                            throw new Exception("Контрол с id:" + id + " остутствует на форме.");
                        }
                        ctrl.Changed += (x, y) => RenderNtf();
                    }
                }
                OnRenderNtf(this, _ntf);
                w.Write("<div class='ntf' id='{0}_ntf'>", HtmlID);
                RenderNtf(w);
                w.Write("</div>");
            }
            if (!V4Page.V4IsPostBack) //render body
            {
                w.Write("</div>");
            }
            PropertyChanged.Clear();
        }

        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        /// <param name="w">Поток</param>
        protected virtual void RenderControlBody(TextWriter w)
        {
        }

        /// <summary>
        ///     Переопределенный метод ToString(), возвращающий значение контрола
        /// </summary>
        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        ///     Валидация контрола
        /// </summary>
        /// <returns>Валидный/не валидный</returns>
        public virtual bool Validation()
        {
            if (!IsReadOnly && !IsDisabled && IsRequired && Value.Length == 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Отрисовка нотификации контрола
        /// </summary>
        /// <param name="w">Поток</param>
        public virtual void RenderNtf(TextWriter w)
        {
            foreach (var ntf in _ntf.List)
            {
                w.Write("<div{1}{2}>- {0}</div>", ntf.Text,
                    string.IsNullOrEmpty(ntf.Color) ? "" : " style='color:" + ntf.Color + "'",
                    string.IsNullOrEmpty(ntf.CssClass) ? "" : " class='" + ntf.CssClass + "'");
            }
        }

        /// <summary>
        ///     Отправка клиенту скрипта с изменениями контрола
        /// </summary>
        public virtual void Flush()
        {
            if (PropertyChanged.Contains("Visible"))
            {
                JS.Write("gi('{0}').style.display='{1}';", HtmlID, Visible ? DisplayStyle : "none");
                JS.Write("if (gi('{0}_cptn'!=='undefined')) {{gi('{0}_cptn').style.display='{1}'}};", HtmlID,
                    Visible ? DisplayCaptionStyle : "none");
            }

            
            if (RefreshRequired)
            {
                V4Page.RefreshHtmlBlock(HtmlID, RenderControl);
                V4Page.RefreshHtmlBlock(HtmlID + "_ntf", RenderNtf);
                return;
            }


            if (PropertyChanged.Contains("IsReadOnly") || PropertyChanged.Contains("IsDisabled"))
            {
                V4Page.RefreshHtmlBlock(HtmlID, RenderControl);
                return;
            }

            if (OnRenderNtf != null && PropertyChanged.Contains("Ntf"))
            {
                V4Page.RefreshHtmlBlock(HtmlID + "_ntf", RenderNtf);
            }

            if (PropertyChanged.Contains("Caption"))
            {
                JS.Write("gi('{0}_cptn').innerHTML='{1}';", HtmlID, Caption);
            }
        }

        /// <summary>
        ///     Получение ID контрола, на который следует установить фокус
        /// </summary>
        /// <returns>ID контрола</returns>
        public string GetHtmlIdNextControl()
        {
            if (!string.IsNullOrEmpty(NextControl))
            {
                var nextCtrl = V4Page.V4Controls.Values.FirstOrDefault(x => x.ID == NextControl);
                if (nextCtrl != null)
                {
                    return nextCtrl.GetFocusControl();
                }
                //return HtmlID;
                return NextControl;
            }
            return NextControl;
        }

        /// <summary>
        ///     Установка фокуса на следующий контрол
        /// </summary>
        public void FocusToNextCtrl()
        {
            if (!string.IsNullOrEmpty(NextControl))
            {
                var nextCtrl = V4Page.V4Controls.Values.FirstOrDefault(x => x.ID == NextControl);
                if (nextCtrl != null)
                {
                    if ((nextCtrl.IsReadOnly || nextCtrl.IsDisabled || !nextCtrl.Visible) &&
                        nextCtrl.HtmlID != NextControl)
                    {
                        nextCtrl.FocusToNextCtrl();
                    }
                    else
                    {
                        nextCtrl.Focus(); //перейдем на следующий контрол если он задан явно
                    }
                }
                else
                {
                    V4Page.FocusControl = NextControl;
                }
            }
            else
            {
                var thisCtrl = false;
                foreach (var key in V4Page.V4Controls.Keys)
                {
                    if (thisCtrl && !V4Page.V4Controls[key].IsReadOnly && !V4Page.V4Controls[key].IsDisabled)
                    {
                        V4Page.V4Controls[key].Focus();
                        break;
                    }
                    if (key.Equals(HtmlID))
                    {
                        thisCtrl = true;
                    }
                }
            }
        }

        /// <summary>
        ///     Обработка клиентских команд
        /// </summary>
        /// <param name="collection">Коллекция параметров</param>
        public virtual void ProcessCommand(NameValueCollection collection)
        {
            
            if (collection["v"] != null)
            {
                var oldVal = Value;
                Value = collection["v"];
                OnChanged(new ProperyChangedEventArgs(oldVal, Value));
                if (!V4Page.JS.ToString().Contains("isChanged=true;"))
                    JS.Write("isChanged=true;");
            }
            if (collection["next"] == "1")
            {
                JS.Write("v4_setFocus2NextCtrl('{0}');", GetFocusControl());
            }
            
        }

        #region Ntf

        private readonly Ntf _ntf = new Ntf();

        /// <summary>
        ///     Отрисовка нотификаций контрола
        /// </summary>
        public event RenderNtfDelegate OnRenderNtf;

        /// <summary>
        ///     Акцессор для зависимостей нотификаций контрола
        /// </summary>
        public string NtfDependencies { get; set; }

        /// <summary>
        ///     Поле, возвращающее информацию о наличии о контрола нотификаций в статусе ошибка
        /// </summary>
        public bool NtfValid
        {
            get { return NtfValidation(); }
        }

        /// <summary>
        ///     Отрисовка нотификаций контрола
        /// </summary>
        public void RenderNtf()
        {
            if (_ntf.List.Count > 0)
            {
                _ntf.List.Clear();
            }

            if (OnRenderNtf != null)
            {
                OnRenderNtf(this, _ntf);
            }

            SetPropertyChanged("Ntf");
        }

        /// <summary>
        ///     Возвращает есть ли у контрола нотификации в статусе ошибка
        /// </summary>
        private bool NtfValidation()
        {
            return _ntf.List.Select(m => m.NtfStatus).Any(t => t == NtfStatus.Error);
        }

        /// <summary>
        ///     Возвращает все нотификации контрола в статусе ошибка
        /// </summary>
        public IEnumerable<NtfData> NtfNotValidData()
        {
            return _ntf.List.Select(m => m).Where(t => t.NtfStatus == NtfStatus.Error);
        }

        #endregion

        #region Binding

        /// <summary>
        ///     Источник связывания
        /// </summary>
        public object BindingSource;

        /// <summary>
        ///     Простое связывание
        /// </summary>
        /// <param name="source">источник</param>
        /// <param name="direction">направление</param>
        /// <returns>Признак успешного связывания</returns>
        public bool BindSimple(object source, BindingDirection direction)
        {
            return BindSimple(source, BindingField, direction);
        }

        private BindDocField _bindDocField;

        /// <summary>
        ///     Двухстороннее связывание по полю документа
        /// </summary>
        /// <remarks>При изменении значения модели менятся контрол и наоборот</remarks>
        /// <example>Для использования достаточно один раз присвоить поле документа</example>
        public DocField BindDocField
        {
			get
			{
				if (null == _bindDocField) return null;
				return _bindDocField.Field;
			}

            set
            {
                if (_bindDocField != null)
                    _bindDocField.Dispose();

                _bindDocField = new BindDocField(this, value);
            }
        }

        /// <summary>
        ///     Обновить контрол из данных поля
        /// </summary>
        /// <remarks>
        ///     Если сущность инициализируется первее привязки байдингов(документ загружен, знаяение полей получены, а далее
        ///     байдинг),
        ///     если байдинг поставить первее сущности то загрузятся пустые значения
        /// </remarks>
        public void RefreshFieldBind()
        {
            if (_bindDocField != null)
                _bindDocField.RefreshValueFromField();
        }

        private BindStringValue _bindStringValue;

        public IBinderValue<string> BindStringValue
        {
            set
            {
                if (_bindStringValue != null)
                    _bindStringValue.Dispose();

                _bindStringValue = new BindStringValue(this, value);
            }
        }

        /// <summary>
        ///     Простое связывание
        /// </summary>
        /// <param name="source">источник</param>
        /// <param name="field">поле</param>
        /// <param name="direction">направление</param>
        /// <returns>Признак успешного связывания</returns>
        public bool BindSimple(object source, string field, BindingDirection direction)
        {
            if (source == null || field.Length == 0)
            {
                return false;
            }
            bool changed;

            if (field.Equals("this"))
            {
                changed = DirectBind(ref source, source.GetType(), direction);
            }
            else
            {
                var t = source.GetType();
                var p = t.GetProperty(field);
                if (p == null)
                {
                    throw new Exception("Object has no field: " + field);
                }

                var val = p.GetValue(source, null);

                changed = DirectBind(ref val, p.PropertyType, direction);
                if (direction == BindingDirection.ToSource)
                {
                    p.SetValue(source, val, null);
                }
            }

            return changed;
        }

        /// <summary>
        ///     Прямое связывание
        /// </summary>
        /// <param name="val">значение</param>
        /// <param name="type">тип</param>
        /// <param name="direction">направление</param>
        /// <returns>Признак успешного связывания</returns>
        public virtual bool DirectBind(ref object val, Type type, BindingDirection direction)
        {
            var changed = false;
            if (direction == BindingDirection.FromSource)
            {
                if (val == null)
                {
                    Value = "";
                }
                else if (type == typeof (DateTime))
                {
                    Value = ((DateTime) val).ToString("dd.MM.yyyy");
                }
                else if (type == typeof (DateTime?))
                {
                    Value = ((DateTime?) val).Value.ToString("dd.MM.yyyy");
                }
                else if (type == typeof (bool))
                {
                    Value = (bool) val ? "1" : "0";
                }
                else if (type == typeof (bool?))
                {
                    Value = (bool) val ? "1" : "0";
                }
                else
                {
                    Value = val.ToString();
                }
            }
            else
            {
                object newval = null;
                if (type == typeof (string))
                {
                    newval = Value;
                }
                else if (type == typeof (int) || type == typeof (int?))
                {
                    if (Value.Length > 0)
                    {
                        newval = int.Parse(Value, NumberStyles.Any);
                    }
                }
                else if (type == typeof (short) || type == typeof (short?))
                {
                    if (Value.Length > 0)
                    {
                        newval = short.Parse(Value, NumberStyles.Any);
                    }
                }
                else if (type == typeof (Guid))
                {
                    newval = Value.Length > 0 ? Guid.Parse(Value) : Guid.Empty;
                }
                else if (type == typeof (Guid?))
                {
                    if (Value.Length > 0 && !Guid.Empty.Equals(Guid.Parse(Value)))
                    {
                        newval = Guid.Parse(Value);
                    }
                }
                else if (type == typeof (DateTime) || type == typeof (DateTime?))
                {
                    if (Value.Length > 0)
                    {
                        newval = DateTime.ParseExact(Value, "dd.MM.yyyy", null);
                    }
                }
                else if (type == typeof (bool))
                {
                    newval = Value.Equals("1");
                }
                else if (type == typeof (bool?) && Value.Length > 0)
                {
                    newval = Value.Equals("1");
                }
                else if (type == typeof (byte))
                {
                    newval = byte.Parse(Value.Length == 0 ? "0" : Value);
                }
                else if (type == typeof (decimal))
                {
                    newval = Value.Length == 0 ? 0m : decimal.Parse(Value);
                }
                else if (type == typeof (decimal?))
                {
                    if (Value.Length > 0)
                    {
                        newval = decimal.Parse(Value);
                    }
                }
                if (newval == null && val != null)
                {
                    changed = true;
                }
                else if (newval != null)
                {
                    changed = !newval.Equals(val);
                }
                val = newval;
            }

            if (OnRenderNtf != null && direction == BindingDirection.FromSource)
                RenderNtf();

            return changed;
        }

        #endregion
    }

    /// <summary>
    ///     Класс обработки события Changed
    /// </summary>
    public class ProperyChangedEventArgs : EventArgs
    {
        /// <summary>
        ///     Признак принятия изменения значения зависимыми контролами (по умолчанию - true)
        /// </summary>
        public bool IsChange
        {
            get { return NewValue != OldValue; }
        }

        /// <summary>
        ///     Новое значение
        /// </summary>
        public string NewValue;

        /// <summary>
        ///     Старое значение
        /// </summary>
        public string OldValue;

        /// <summary>
        ///     Конструктор
        /// </summary>
        /// <param name="oldValue">Старое значение</param>
        /// <param name="newValue">Новое значение</param>
        public ProperyChangedEventArgs(string oldValue, string newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    /// <summary>
    ///     Класс обработки события Deleted
    /// </summary>
    public class ProperyDeletedEventArgs : EventArgs
    {
        /// <summary>
        ///     Удаленное значение
        /// </summary>
        public string DelValue;

        /// <summary>
        ///     Конструктор
        /// </summary>
        public ProperyDeletedEventArgs(string delValue)
        {
            DelValue = delValue;
        }
    }
}