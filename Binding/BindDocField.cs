using System;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.Entities.Documents;

namespace Kesco.Lib.Web.Controls.V4.Binding
{
    /// <summary>
    ///     Класс двухстороннего байдинга поля документа и контрола
    /// </summary>
    /// <remarks>Инструкция по применению в базовом классе</remarks>
    public class BindDocField : V4Binding, IDisposable
    {
        private readonly DocField _field;

        /// <summary>
        ///     Конструктор
        /// </summary>
        public BindDocField(V4Control control, DocField field) : base(control)
        {
            _field = field;
            _field.ValueChangedEvent += FieldOnValueChangedEventHandler;
        }

		public DocField Field { get { return _field; } }

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
                    _field.Value = ((DatePicker) ctrl).ValueDate;
                }
                else if (ctrl is Select)
                {
                    object v = ((Select) ctrl).Value;
                    _field.ValueString = v == null || v.Equals(0) ? null : v.ToString();
                }
                else
                {
                    _field.Value = arg.NewValue;
                }
            }
        }

        /// <summary>
        ///     Обновление значений контрола из значений, харнимых в поле сущности
        /// </summary>
        public void RefreshValueFromField()
        {
            _control.Value = _field.ValueString;
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
                    var dp = ((DatePicker) ctrl);
                    dp.ValueDate = _field.DateTimeValue;
                }
                else
                    if (ctrl is Select)
                    {
                        var s = ((Select) ctrl);
                        if (_field.Value == null || _field.Value.Equals(0))
                            s.Value = "";
                        else
                            s.Value = _field.ValueString;
                    }
                    else
                        _control.Value = arg.NewValue;
            }
        }

        /// <summary>
        ///     Отписатся от событий
        /// </summary>
        protected override void Unsubscribe()
        {
            _control.ValueChanged -= ControlOnValueChanged;
            _field.ValueChangedEvent -= FieldOnValueChangedEventHandler;
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