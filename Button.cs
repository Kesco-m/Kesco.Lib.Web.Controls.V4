using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using Kesco.Lib.BaseExtention;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Контрол Button (Кнопка)
    /// </summary>
    public class Button : V4Control
    {
        private string _text;

        public List<SelectedRunItem> SelectedRunMenu;

        /// <summary>
        ///     Конструктор
        /// </summary>
        public Button()
        {
            Width = Unit.Empty;
            IsUseCondition = true;
        }

        /// <summary>
        ///     Тут пишем клиентский скрипт, который выполнится на событии onclick
        /// </summary>
        public string OnClick { get; set; }

        // Рисовать ли перед кнопкой разделитель
        public bool SeparatorBefore { get; set; } = false;

        /// <summary>
        ///     Тут пишем текст, который отобразится на кнопке
        /// </summary>
        public string Text
        {
            get { return _text; }
            set
            {
                if (_text != value)
                {
                    SetPropertyChanged("Text");
                    _text = value;
                }
            }
        }

        /// <summary>
        ///     Добавляем иконку из JQUI
        /// </summary>
        public string IconJQueryUI { get; set; }

        /// <summary>
        ///     Добавляем вторую иконку из JQUI
        /// </summary>
        public string IconJQueryUISecondary { get; set; }

        /// <summary>
        ///     Добавляем иконку из Styles
        /// </summary>
        public string IconKesco { get; set; }

        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        /// <param name="w">Поток</param>
        public override void RenderControl(TextWriter w)
        {
            if (SeparatorBefore)
                w.Write(
                    @"<span style=""height:65px; border-left: #c5c5c5 solid 2px; margin-left: 2px; margin-right: 3px;""></span>");


            if (SelectedRunMenu != null && SelectedRunMenu.Count > 0)
            {
                var addedItem = new List<int>();
                w.Write("<nobr>");
                w.Write(@"<select name=""{0}"" id=""{0}"">", "run" + HtmlID);
                SelectedRunMenu.OrderBy(x => x.Order).ToList().ForEach(delegate(SelectedRunItem ri)
                {
                    var p = SelectedRunMenu.FirstOrDefault(x => x.GroupValue == ri.Value);

                    if (p == null)
                    {
                        if (!addedItem.Contains(ri.Value))
                        {
                            addedItem.Add(ri.Value);
                            w.Write(@"<option {0} {1} value=""{2}"">{3}</option>", ri.IsDisabled ? "disabled" : "",
                                ri.IsSelected ? "selected" : "", ri.Value, ri.Name);
                        }
                    }
                    else
                    {
                        addedItem.Add(ri.Value);
                        w.Write(@"<optgroup label=""{0}"" >", ri.Name);
                        SelectedRunMenu.Where(x => x.GroupValue == ri.Value).ToList().OrderBy(x => x.Order).ToList()
                            .ForEach(
                                delegate(SelectedRunItem rig)
                                {
                                    if (!addedItem.Contains(rig.Value))
                                    {
                                        addedItem.Add(rig.Value);
                                        w.Write(
                                            @"<option {0} {1} value=""{2}"" style=""margin-left:10px"">{3}</option>",
                                            rig.IsDisabled ? "disabled" : "",
                                            rig.IsSelected ? "selected" : "", rig.Value, rig.Name);
                                    }
                                });

                        w.Write("</optgroup>");
                    }
                });

                w.Write("</select>");
            }


            w.Write("<button");
            w.Write(" id='{0}'", HtmlID);

            if (!string.IsNullOrEmpty(Value))
                w.Write(" value=\"{0}\"", HttpUtility.HtmlEncode(Value));


            w.Write(" onclick=\"{0}\"", OnClick);
            w.Write(" style = \"border-width: 1px !important;");
            if (!Visible)
                w.Write("display:none;");
            if (Width != Unit.Empty)
                w.Write("width:{0};", Width);


            if (Height != Unit.Empty)
                w.Write("height:{0};", Height);

            w.Write(Style);

            w.Write("\"");

            var cssClass = CSSClass;
            if (IsDisabled)
                cssClass = CSSClass.IsNullEmptyOrZero() ? "button_disabled" : CSSClass + ", button_disabled";

            if (!string.IsNullOrEmpty(cssClass)) w.Write(" class='{0}'", cssClass);

            if (!string.IsNullOrEmpty(Title))
                w.Write(" title='{0}'", HttpUtility.HtmlEncode(Title));

            if (!string.IsNullOrEmpty(NextControl))
                w.Write(" nc='{0}'", HttpUtility.HtmlEncode(NextControl));

            if (TabIndex.HasValue)
                w.Write(" TabIndex={0} ", TabIndex);

            w.Write(">");
            w.Write(Text);
            w.Write("</button>");

            if (!string.IsNullOrEmpty(IconJQueryUI))
                w.Write("<script>$('#{0}').button({{icons: {{primary: {1}{2}}}{3}}});</script>", HtmlID, IconJQueryUI,
                    !string.IsNullOrEmpty(IconJQueryUISecondary) ? $", secondary: {IconJQueryUISecondary}" : "",
                    SelectedRunMenu != null && SelectedRunMenu.Count > 0 ? ", text: false" : "");
            else if (!string.IsNullOrEmpty(IconKesco))
                w.Write("<script>$('#{0}').prepend(\"<img src='{1}'/>\").button();</script>", HtmlID, IconKesco);
            else
                w.Write("<script>$('#{0}').button();</script>", HtmlID);

            if (SelectedRunMenu != null && SelectedRunMenu.Count > 0)
            {
                w.Write("</nobr>");
                w.Write(
                    "<script>$('#run{0}').selectmenu({{width : 'auto'}}); $('.ui-selectmenu-menu').css('z-index', 10000); </script>",
                    HtmlID);
            }
        }

        /// <summary>
        ///     Отправка клиенту скрипта с изменениями контрола
        /// </summary>
        public override void Flush()
        {
            if (PropertyChanged.Count == 0) return;

            if (PropertyChanged.Contains("Visible"))
                JS.Write("if(gi('{0}'))gi('{0}').style.display='{1}';", HtmlID, Visible ? "" : "none");
            else if (PropertyChanged.Contains("IsReadOnly"))
                JS.Write("if(gi('{0}'))gi('{0}').disabled='{1}';", HtmlID, IsReadOnly || IsDisabled ? "1" : "");
            else if (PropertyChanged.Contains("IsDisabled"))
                if (!IsDisabled)
                    JS.Write(
                        "$('#{0}').prop('disabled', false).removeClass('ui-state-disabled').addClass('ui-state-default');",
                        HtmlID);

            if (PropertyChanged.Contains("Text")) JS.Write("if(gi('{0}'))gi('{0}').innerText='{1}';", HtmlID, Text);
        }
    }
}