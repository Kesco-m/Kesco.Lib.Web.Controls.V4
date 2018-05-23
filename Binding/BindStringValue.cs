using System;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.BaseExtention.BindModels;

namespace Kesco.Lib.Web.Controls.V4.Binding
{
    /// <summary>
    ///     Класс двухстороннего байдинга значения string и контрола
    /// </summary>
    /// <remarks>Инструкция по применению в базовом классе</remarks>
    public class BindStringValue : V4Binding, IDisposable
    {
        private readonly IBinderValue<string> _valueBind;

        /// <summary>
        ///     Конструктор
        /// </summary>
        public BindStringValue(V4Control control, IBinderValue<string> value)
            : base(control)
        {
            _valueBind = value;
            _valueBind.ValueChangedEvent += FieldOnValueChangedEvent;
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

        private void FieldOnValueChangedEvent(object sender, ValueChangedEventArgs arg)
        {
            if (arg.IsChange)
                _control.Value = arg.NewValue;
        }

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
            _control.ValueChanged -= ControlOnValueChanged;
            _valueBind.ValueChangedEvent -= FieldOnValueChangedEvent;
        }

        /// <summary>
        ///     Финализатор
        /// </summary>
        ~BindStringValue()
        {
            Unsubscribe();
        }
    }
}