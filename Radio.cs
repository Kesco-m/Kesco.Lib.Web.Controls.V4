using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Web;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Контрол радобатон
    /// </summary>
    public class Radio : V4Control
    {
        /// <summary>
        ///     Коллекция радиобатонов
        /// </summary>
        public List<Item> Items = new List<Item>();

        /// <summary>
        ///     Конструктор
        /// </summary>
        public Radio()
        {
            IsUseCondition = true;
        }

        /// <summary>
        ///     Текст контрола
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        ///     Атрибут Наименование
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Признак вывода набора радио в строку
        /// </summary>
        public bool IsRow { get; set; }

        public int PaddingLeftRadio { get; set; }
        public int HeightRow { get; set; }
        public int MarginLeftLabel { get; set; }


        /// <summary>
        ///      Текст элемента, выбранного в контроле
        /// </summary>
        public string ValueText
        {
            get
            {
                return Items.SingleOrDefault(x => x.Code == Value)?.Name;
            }
        }


        /// <summary>
        ///     Проверка выделен ли элемент в коллекции radio
        /// </summary>
        /// <param name="item">элемент коллекции</param>
        /// <returns>Checked</returns>
        public bool GetChecked(Item item)
        {
            var check = false;
            var p = Items.FirstOrDefault(x => x.Code == item.Code);
            if (p != null && p.Code == Value)
                check = true;

            return check;
        }

        /// <summary>
        ///     Отправка клиенту скрипта с изменениями контрола
        /// </summary>
        public override void Flush()
        {
            base.Flush();
            if (PropertyChanged.Contains("Value"))
            {
                var ckd = false;
                var p = Items.FirstOrDefault(x => x.Code == Value);
                if (p != null) ckd = true;
                JS.Write("if (gi('{0}{1}_0')) gi('{0}{1}_0').checked={2};", HtmlID, Value, ckd ? 1 : 0);
            }
        }

        public override void ProcessCommand(NameValueCollection collection)
        {
            if (collection["v"] != null && collection["rb"] != null)
            {
                var oldVal = Value;
                Value = collection["rb"];
                OnChanged(new ProperyChangedEventArgs(oldVal, Value));
                JS.Write("v4_isChanged=true;");
            }
            else
            {
                base.ProcessCommand(collection);
            }
        }

        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        /// <param name="w">Поток</param>
        protected override void RenderControlBody(TextWriter w)
        {
            if (Items.Count == 0) return;
            w.Write("<div id=\"{0}\" class=\"v4ch\">", HtmlID);
            if (IsRow)
            {
                foreach (var item in Items)
                {
                    w.Write(
                        "<input type=\"radio\" id=\"{0}{4}_0\" name=\"{2}\" {1} Title=\"{3}\" onclick=\"cmd('ctrl','{0}','v',(this.checked?'0':'1'),'rb','{4}');\"",
                        HtmlID, GetChecked(item) ? "checked" : "", Name, Title, item.Code);
                    if (IsReadOnly || IsDisabled)
                        w.Write(" disabled='1'");
                    if (!string.IsNullOrEmpty(NextControl))
                        w.Write(" nc='{0}'", GetHtmlIdNextControl());
                    if (TabIndex.HasValue)
                        w.Write(" TabIndex={0} ", TabIndex);
                    w.Write(
                        " onkeydown='v4cb_keyDown(event)' /><label id =\"label_{0}{1}\" for=\"{0}{1}_0\" style=\"margin-right: 15px;\">{2}</label>",
                        HtmlID, item.Code, HttpUtility.HtmlEncode(item.Name));
                }

                w.Write("</div>");
                return;
            }

            w.Write("<table cellpadding=\"0\" cellspacing=\"0\">");
            foreach (var item in Items)
            {
                w.Write("<tr style=\"height:{1}px;\"><td style=\"padding-left:{0}px;\">", PaddingLeftRadio, HeightRow);
                w.Write(
                    "<input type=\"radio\" id=\"{0}{4}_0\" name=\"{2}\" {1} Title=\"{3}\" onclick=\"cmd('ctrl','{0}','v',(this.checked?'1':'0'),'rb','{4}');\"",
                    HtmlID, GetChecked(item) ? "checked" : "", Name, Title, item.Code);
                if (IsReadOnly || IsDisabled)
                    w.Write(" disabled='1'");
                if (!string.IsNullOrEmpty(NextControl))
                    w.Write(" nc='{0}'", GetHtmlIdNextControl());
                if (TabIndex.HasValue)
                    w.Write(" TabIndex={0} ", TabIndex);
                w.Write(
                    " onkeydown='v4cb_keyDown(event)' /><label id =\"label_{0}{1}\" for=\"{0}{1}_0\" style=\"margin-left:{2}px;\">{3}</label>",
                    HtmlID, item.Code, MarginLeftLabel,
                    HttpUtility.HtmlEncode(item.Name));
                w.Write("</td></tr>");
            }

            w.Write("</table></div>");
        }
    }
}