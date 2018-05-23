using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Контрол ComboBox (Выпадающий список)
    /// </summary>
    [DefaultProperty("Value")]
    [ToolboxData("<{0}:ComboBox runat=server />")]
    public class ComboBox : V4Control
    {
        /// <summary>
        ///     Коллекция данных
        /// </summary>
        public Dictionary<string, object> DataItems = new Dictionary<string, object>();

        /// <summary>
        ///     Заполнение списка
        /// </summary>
        public EnumerableVoidDelegate FillList;

        /// <summary>
        ///     Коллекция строковых данных
        /// </summary>
        public Dictionary<string, string> Items = new Dictionary<string, string>();

        /// <summary>
        ///     Конструктор
        /// </summary>
        public ComboBox()
        {
            KeyField = "";
            ValueField = "";
            DataField = "";
            ValueText = "";
            EmptyValueText = "";
            EmptyValueExist = true;
            CSSClass = "v4c";
            OnChangeClientScript = "";
        }

        /// <summary>
        ///     Поле ключа
        /// </summary>
        public string KeyField { get; set; }

        /// <summary>
        ///     Поле значения
        /// </summary>
        public string ValueField { get; set; }

        /// <summary>
        ///     Поле данных
        /// </summary>
        public string DataField { get; set; }

        /// <summary>
        ///     Значение в текстовом виде
        /// </summary>
        public string ValueText { get; set; }

        /// <summary>
        ///     Текст пустой строки
        /// </summary>
        public string EmptyValueText { get; set; }

        /// <summary>
        ///     Признак наличия пустой строки
        /// </summary>
        public bool EmptyValueExist { get; set; }

        /// <summary>
        ///     Данные
        /// </summary>
        public object ValueData { get; set; }

        /// <summary>
        ///     Скрипт на событие OnChange
        /// </summary>
        public string OnChangeClientScript { get; set; }

        /// <summary>
        ///     Заполнение списка
        /// </summary>
        public void FillItems()
        {
            var dt = FillList();
            foreach (var o in dt)
            {
                string key, val;
                object data = null;
                if (o is DataRow)
                {
                    key = (o as DataRow)[KeyField].ToString();
                    val = (o as DataRow)[ValueField].ToString();
                    if (!String.IsNullOrEmpty(DataField))
                        data = (o as DataRow)[DataField];
                }
                else if (o is string)
                {
                    key = val = o.ToString();
                }
                else
                {
                    var t = o.GetType();
                    key = t.GetProperty(KeyField).GetValue(o, null).ToString();
                    val = t.GetProperty(ValueField).GetValue(o, null).ToString();
                    if (!String.IsNullOrEmpty(DataField))
                        data = t.GetProperty(DataField).GetValue(o, null);
                }
                if (key.Equals(Value))
                {
                    ValueText = val;
                    ValueData = data;
                }

                if (!Items.ContainsKey(key))
                    Items.Add(key, val);

                if (!DataItems.ContainsKey(key))
                    DataItems.Add(key, data);
            }
        }

        /// <summary>
        ///     Проверка наличия ключа в коллекции
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Признак наличия</returns>
        public bool Contains(string key)
        {
            return Items.ContainsKey(key);
        }

        /// <summary>
        ///     Обновление контрола
        /// </summary>
        public void Refresh()
        {
            V4Page.RefreshHtmlBlock(ID, RenderControlBody);
        }

        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        /// <param name="w">Поток</param>
        protected override void RenderControlBody(TextWriter w)
        {
            if (Items.Count == 0 && FillList != null)
            {
                FillItems();
            }
            if (IsReadOnly)
            {
                if (Value.Length > 0)
                {
                    object o = Items[Value];
                    if (o != null)
                    {
                        w.Write(HttpUtility.HtmlEncode(o.ToString()));
                    }
                }
            }
            else
            {
                if (Value == "" && Items.ContainsValue(""))
                    Value = Items.First(x => x.Value == "").Key;

                w.Write(
                    "<select id='{0}_0' style='width:{1};{2};padding:0px;border:1px;' onchange='{3};v4cb_changed(event);' ",
                    HtmlID, Width.IsEmpty ? "100%" : Width.ToString(), Height.IsEmpty ? "" : "height:" + Height + ";",
                    OnChangeClientScript);
                w.Write(" onkeydown='v4cb_keyDown(event);'");
                w.Write(" isRequired={0}", IsRequired ? 1 : 0);
                w.Write(" class='{0}{1}'", CSSClass, (IsRequired && Value.Length == 0) ? " v4s_required" : "");

                if (IsRequired && Value.Length == 0)
                    w.Write(" selectedIndex=-1");
                if (!string.IsNullOrEmpty(NextControl))
                    w.Write(" nc='{0}'", GetHtmlIdNextControl());
                if (IsDisabled)
                    w.Write(" disabled='true'");
                if (TabIndex.HasValue)
                    w.Write(" TabIndex={0} ", TabIndex);
                w.Write(" help='{0}'>", HttpUtility.HtmlEncode(Help));

                if (!Items.ContainsValue("") && EmptyValueExist)
                    w.Write("<option value=''>{0}</option>", EmptyValueText);

                foreach (var o in Items)
                {
                    w.Write("<option{2} value='{1}'>{0}</option>", o.Value, o.Key,
                        o.Key.Equals(Value) ? " selected='selected'" : "");
                }
                w.Write("</select>");
            }
        }

        /// <summary>
        ///     Обработка клиентских команд
        /// </summary>
        /// <param name="collection">Коллекция параметров</param>
        public override void ProcessCommand(NameValueCollection collection)
        {
            if (collection["t"] != null)
            {
                ValueText = collection["t"].Trim();
            }
            base.ProcessCommand(collection);
        }

        /// <summary>
        ///     Отправка клиенту скрипта с изменениями контрола
        /// </summary>
        public override void Flush()
        {
            base.Flush();
            if (PropertyChanged.Contains("Value"))
            {
                if (!IsReadOnly)
                    JS.Write("v4cb_setValue('{0}','{1}');", HtmlID, HttpUtility.HtmlEncode(Value));
                else
                {
                    if (Value.Length > 0)
                    {
                        object o = Items[Value];
                        if (o != null)
                        {
                            JS.Write("v4cb_setValueReadOnly('{0}','{1}');", HtmlID, HttpUtility.HtmlEncode(o.ToString()));
                        }
                    }
                }
            }
        }
    }
}