namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    /// Класс, опеределяющий какой метод необходимо использовать в контроле Select для получения значения указанного поля
    /// </summary>
    public class SelectMethodGetEntityValue
    {
        /// <summary>
        ///     Название ключа для поиска среди выводимых полей
        /// </summary>
        public string ValueField { get; set; }

        /// <summary>
        ///     Название метода, получения значения
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        ///     Параметры метода
        /// </summary>
        public object[] MethodParams;
    }
}