using System;
using Kesco.Lib.BaseExtention;

namespace Kesco.Lib.Web.Controls.V4.Binding
{
    /// <summary>
    ///     Базовый класс валидации
    /// </summary>
    [Serializable]
    public abstract class V4Binding
    {
        protected readonly V4Control _control;
        /* Как пользоватся?
         * В каждом контроле наследуемого от V4Control есть соответствующее поле
         * Например для DocFieldBinding - BindDocField, для StringValueBinding - StringValueBind
         * Чтобы начать пользоватся достаточно один раз присвоить к контролу экзымпляр со значением
         * Например так Control.BindDocField = Doc.CurrencyField
         * Так же возможно связывание объектов реализующих интерфейс IValueBinder<T>
         * Например Entities.Documents.BaseDocFacade реалзиует интерфейс IValueBinder<T> его так же можно использовать для привязки
         * BaseDocFacade реализует интерфейс IValueBinder<string> значит наиболее подходящим методом в V4Control будет StringValueBind,
         * который принимает значение IValueBinder<string>
         */

        protected V4Binding(V4Control control)
        {
            _control = control;
            control.ValueChanged += ControlOnValueChanged;
        }

        protected abstract void ControlOnValueChanged(object sender, ValueChangedEventArgs arg);

        /// <summary>
        ///     Отписатся от событий
        /// </summary>
        protected abstract void Unsubscribe();
    }
}