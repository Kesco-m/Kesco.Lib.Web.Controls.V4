using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Контрол DropDownList (Выпадающий список)
    /// </summary>
    public class DropDownList : Select
    {
        //Список элементов
        private List<Entities.Item> _listOfItems;


        /// <summary>
        ///     Коллекция данных
        /// </summary>
        public Dictionary<string, object> DataItems = new Dictionary<string, object>();

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
        ///     Признак того, что значения контрола не ограничиваются списком
        /// </summary>
        public bool IsNoLimitList { get; set; }

        /// <summary>
        ///     Заполнение списка
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
        ///     Метод заполоняет список элементов
        /// </summary>
        private void FillItemsList()
        {
            _listOfItems = new List<Entities.Item>();
            foreach (var item in DataItems) _listOfItems.Add(new Entities.Item {Id = item.Key, Value = item.Value});
        }

        /// <summary>
        ///     Метод заполняет выбранное значение
        /// </summary>
        /// <param name="val"></param>
        /// <param name="valueText"></param>
        protected override void SetControlValue(string val, string valueText = "")
        {
            if (IsNoLimitList && string.IsNullOrEmpty(val) && !string.IsNullOrEmpty(valueText))
                val = valueText;

            base.SetControlValue(val);

            if (DataItems.Any(item => item.Key == val))
                ValueText = DataItems.First(item => item.Key == val).Value.ToString();
            else if (!IsNoLimitList)
                ValueText = "";
        }

        /// <summary>
        ///     Отрисовка выпадающего списка
        /// </summary>
        /// <param name="w">Поток вывода</param>
        protected override void RenderSelectBody(TextWriter w)
        {
            base.RenderSelectBody(w);

            if (IsNoLimitList)
                JS.Write("$('#{0} :input').attr('nolimit', true);", HtmlID);

            if (!IsReadOnly && !IsReadOnlyAlways) return;

            JS.Write("$('#{0} :input').attr('readonly', true);", HtmlID);

            if (IsNoLimitList) return;

            JS.Write("$('#{0} :input').on('selectstart', false);", HtmlID);
            JS.Write("$('#{0} :input').on('mousedown', false);", HtmlID);
            JS.Write("$('#{0} :input').css('cursor', 'default');", HtmlID);
        }

        public override void Flush()
        {
            base.Flush();

            if (IsNoLimitList) return;
            V4Page.JS.Write("$('#{0}_0').unbind('selectstart').bind('selectstart', function(event){{ return false;}});",
                HtmlID);
            V4Page.JS.Write("$('#{0}_0').unbind('mousedown').bind('mousedown', function(event){{ return false;}});",
                HtmlID);
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