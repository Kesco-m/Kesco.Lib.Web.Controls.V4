using System;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.Entities.Documents;

namespace Kesco.Lib.Web.Controls.V4.Binding
{
    /// <summary>
    ///     Класс двухстороннего байдинга поля документа и контрола
    /// </summary>
    /// <remarks>Инструкция по применению в базовом классе</remarks>
    [Serializable]
    public class BindDocField : V4Binding, IDisposable
    {
        /// <summary>
        ///     Конструктор
        /// </summary>
        public BindDocField(V4Control control, DocField field) : base(control)
        {
            Field = field;
            if (Field != null)
                Field.ValueChangedEvent += FieldOnValueChangedEventHandler;
        }

        public DocField Field { get; }

        /// <summary>
        ///     Освобождение объекта
        /// </summary>
        public void Dispose()
        {
            Unsubscribe();
            // если правильно все сделали, то финалайзер не требуется
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Обработчик изменения контрола
        /// </summary>
        protected override void ControlOnValueChanged(object sender, ValueChangedEventArgs arg)
        {
            if (arg.IsChange)
            {
                var ctrl = (V4Control) sender;

                if (ctrl is DatePicker)
                {
                    Field.Value = ((DatePicker) ctrl).ValueDate;
                }
                else if (ctrl is Select)
                {
                    object v = ((Select) ctrl).Value;
                    Field.ValueString = v == null || v.Equals(0) ? null : v.ToString();
                }
                else
                {
                    Field.Value = arg.NewValue;
                }
            }
        }

        /// <summary>
        ///     Обновление значений контрола из значений, харнимых в поле сущности
        /// </summary>
        public void RefreshValueFromField()
        {
            _control.Value = Field.ValueString;
        }

        /// <summary>
        ///     Обработчик изменения Поля документа
        /// </summary>
        private void FieldOnValueChangedEventHandler(object sender, ValueChangedEventArgs arg)
        {
            if (arg.IsChange)
            {
                var ctrl = _control;

                if (ctrl is DatePicker)
                {
                    var dp = (DatePicker) ctrl;
                    dp.ValueDate = Field.DateTimeValue;
                }
                else if (ctrl is Select)
                {
                    var s = (Select) ctrl;
                    if (Field.Value == null || Field.Value.Equals(0))
                        s.Value = "";
                    else
                        s.Value = Field.ValueString;
                }
                else
                {
                    _control.Value = arg.NewValue;
                }
            }
        }

        /// <summary>
        ///     Отписатся от событий
        /// </summary>
        protected override void Unsubscribe()
        {
            _control.ValueChanged -= ControlOnValueChanged;
            if (Field != null)
                Field.ValueChangedEvent -= FieldOnValueChangedEventHandler;
        }

        /// <summary>
        ///     Финализатор
        /// </summary>
        ~BindDocField()
        {
            Unsubscribe();
        }
    }
}