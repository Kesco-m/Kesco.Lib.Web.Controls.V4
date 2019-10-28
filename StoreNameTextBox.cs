using System;

namespace Kesco.Lib.Web.Controls.V4
{
    public sealed class StoreNameTextBox : TextBox
    {
        /// <summary>
        ///     Перечисление условий для текстового поля
        /// </summary>
        public enum TextBoxEnum
        {
            /// <summary>
            ///     Номер счета не известен
            /// </summary>
            AccountUnknown = 10010
        }

        public const string NoNameValue = "_____________________";

        public override void V4OnInit()
        {
            base.V4OnInit();
            if (!IsUseCondition) return;
            TextBoxFiltersList.Add(new Item(((int) TextBoxEnum.AccountUnknown).ToString(),
                Resx.GetString("cAccountUnknown")));
        }

        public override string GetFilterClauseText()
        {
            switch ((TextBoxEnum) Convert.ToInt32(ValueTextBoxEnum))
            {
                case TextBoxEnum.AccountUnknown:
                {
                    if (!string.IsNullOrEmpty(Description))
                        return Description + ": " + Resx.GetString("cAccountUnknown");
                }
                    break;
            }

            return base.GetFilterClauseText();
        }

        protected override bool IsEditable()
        {
            return base.IsEditable() && ValueTextBoxEnum != ((int) TextBoxEnum.AccountUnknown).ToString();
        }

        public override void OnChanged(ProperyChangedEventArgs e)
        {
            base.OnChanged(e);

            if (ValueTextBoxEnum == ((int) TextBoxEnum.AccountUnknown).ToString())
            {
                Value = NoNameValue;
                SetPropertyChanged("Value");
            }
        }
    }
}