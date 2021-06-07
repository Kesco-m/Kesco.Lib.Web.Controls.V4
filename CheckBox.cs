using System.IO;
using Kesco.Lib.BaseExtention;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Контрол CheckBox (Галочка)
    /// </summary>
    public class CheckBox : V4Control
    {
        /// <summary>
        ///     Текст чекбокса
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        ///     Создавать надпись как связанную с контролом
        /// </summary>
        public bool LabelFor { get; set; } = true;

        public string OnClick { get; set; } = "";

        /// <summary>
        ///     Признак выбора чекбокса
        /// </summary>
        public bool Checked
        {
            get { return !Value.IsNullEmptyOrZero(); }
            set
            {
                if (Value.Equals("1") ^ value) SetPropertyChanged("Value");
                Value = value ? "1" : "0";
            }
        }

        /// <summary>
        ///     Отправка клиенту скрипта с изменениями контрола
        /// </summary>
        public override void Flush()
        {
            base.Flush();
            if (PropertyChanged.Contains("Value"))
                JS.Write("if (gi('{0}_0')) gi('{0}_0').checked={1};", HtmlID, Checked ? 1 : 0);
        }

        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        /// <param name="w">Поток</param>
        protected override void RenderControlBody(TextWriter w)
        {
            w.Write(
                "<input type='checkbox' id='{0}_0' {1} ov='{2}' style='padding-bottom: 3px;' onclick=\"{3} cmd('ctrl','{0}','v',(this.checked?'1':'0'),'ov', '{2}');\"",
                HtmlID, Checked ? "checked" : "", OriginalValue, OnClick);
            if (IsReadOnly || IsDisabled)
                w.Write(" disabled ");
            if (!string.IsNullOrEmpty(NextControl))
                w.Write(" nc='{0}'", GetHtmlIdNextControl());
            if (TabIndex.HasValue)
                w.Write(" TabIndex={0} ", TabIndex);
            w.Write(" onkeydown='v4cb_keyDown(event)'");
            w.Write(" /><label {0} style='display: inline-block; vertical-align:top; padding-left:3px;'>{1}</label>", LabelFor ? string.Format("for=\"{0}_0\"", HtmlID) : "", Text);
        }
    }
}