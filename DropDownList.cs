using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Контрол DropDownList (Выпадающий список)
    /// </summary>

    public class DropDownList : Select
    {
        /// <summary>
        ///     Коллекция данных
        /// </summary>
        public Dictionary<string, object> DataItems = new Dictionary<string, object>();

        //Список элементов
        List<Kesco.Lib.Entities.Item> _listOfItems = null;

        /// <summary>
        ///     Конструктор класса
        /// </summary>
        public DropDownList()
        {
            FillPopupWindow = FillSelect;
            SelectedItemById = GetObjectById;

            KeyField = "Id";
            ValueField = "Name";
            IsNotUseSelectTop = true;
        }

        /// <summary>
        /// Заполнение списка 
        /// </summary>
        /// <param name="search">Строка поиска</param>
        /// <returns>Интерфейс доступа к списку</returns>
        public IEnumerable FillSelect(string search)
        {
            OnBeforeSearch();
            FillItemsList();
            return _listOfItems;
        }

        /// <summary>
        /// Метод заполоняет список элементов
        /// </summary>
        private void FillItemsList()
        {
            _listOfItems = new List<Kesco.Lib.Entities.Item>();
            foreach (var item in DataItems)
            {
                _listOfItems.Add(new Kesco.Lib.Entities.Item {Id = item.Key, Value = item.Value});
            }
        }

        /// <summary>
        /// Метод заполняет выбранное значение
        /// </summary>
        /// <param name="val"></param>
        /// <param name="valueText"></param>
        protected override void SetControlValue(string val, string valueText = "")
        {
            base.SetControlValue(val);
            if (DataItems.Any(item => item.Key == val))
                ValueText = DataItems.First(item => item.Key == val).Value.ToString();
            else
                ValueText = "";
        }

        /// <summary>
        ///     Отрисовка выпадающего списка
        /// </summary>
        /// <param name="w">Поток вывода</param>
        protected override void RenderSelectBody(TextWriter w)
        {
            base.RenderSelectBody(w);

            if (IsReadOnly)
            {
                JS.Write("$('#{0} :input').attr('readonly', true);", HtmlID);
                JS.Write("$('#{0} :input').on('selectstart', false);", HtmlID);
                JS.Write("$('#{0} :input').on('mousedown', false);", HtmlID);
                JS.Write("$('#{0} :input').css('cursor', 'default');", HtmlID);
            }
        }

        public override void Flush()
        {
            base.Flush();
            V4Page.JS.Write("$('#{0}_0').unbind('selectstart').bind('selectstart', function(event){{ return false;}});", HtmlID);
            V4Page.JS.Write("$('#{0}_0').unbind('mousedown').bind('mousedown', function(event){{ return false;}});", HtmlID);
        }


        /// <summary>
        ///     Получение сущности по ID
        /// </summary>
        /// <param name="id">ID</param>
        /// <param name="name">Name </param>
        /// <returns>Сущность</returns>
        public virtual object GetObjectById(string id, string name = "")
        {
            return null;
        }
    }
}