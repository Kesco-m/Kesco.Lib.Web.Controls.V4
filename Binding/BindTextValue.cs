using System;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.BaseExtention.BindModels;

namespace Kesco.Lib.Web.Controls.V4.Binding
{
    /// <summary>
    ///     Класс двухстороннего байдинга значения, отображаемое в контроле select
    /// </summary>
    /// <remarks>Инструкция по применению в базовом классе</remarks>
    public class BindTextValue : V4Binding, IDisposable
    {
        private readonly Select _select;
        private readonly IBinderValue<string> _valueBind;

        public BindTextValue(Select control, IBinderValue<string> value) : base(control)
        {
            _valueBind = value;
            _valueBind.ValueChangedEvent += ValueBindOnValueChangedEvent;
            _select = (Select) _control;
        }

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
        ///     Обработчик события изменения модели
        /// </summary>
        private void ValueBindOnValueChangedEvent(object sender, ValueChangedEventArgs arg)
        {
            if (arg.IsChange)
                _select.ValueText = arg.NewValue;
        }

        /// <summary>
        ///     Обработчик события контрола
        /// </summary>
        protected override void ControlOnValueChanged(object sender, ValueChangedEventArgs arg)
        {
            if (arg.IsChange)
                _valueBind.Value = arg.NewValue;
        }

        /// <summary>
        ///     Отписатся от событий
        /// </summary>
        protected override void Unsubscribe()
        {
            _select.TextChanged -= ControlOnValueChanged;
            _valueBind.ValueChangedEvent -= ValueBindOnValueChangedEvent;
        }

        /// <summary>
        ///     Финализатор
        /// </summary>
        ~BindTextValue()
        {
            Unsubscribe();
        }
    }
}