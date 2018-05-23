using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.IO;
using System.Web;

namespace Kesco.Lib.Web.Controls.V4
{
    /// <summary>
    ///     Получение источника данных
    /// </summary>
    public delegate DataTable DataTableVoidDelegate();

    /// <summary>
    ///     Класс элемент дерева
    /// </summary>
    public class TreeItem
    {
        /// <summary>
        ///     Коллекция элементов дерева
        /// </summary>
        public Dictionary<string, TreeItem> Items = new Dictionary<string, TreeItem>();

        /// <summary>
        ///     Родительский элемент
        /// </summary>
        public TreeItem ParentItem { get; set; }

        /// <summary>
        ///     Текст
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        ///     Идентификатор
        /// </summary>
        public string Id { get; set; }
    }

    /// <summary>
    ///     Класс дерево
    /// </summary>
    public class Dynatree : V4Control
    {
        /// <summary>
        ///     Значение выбранного элемента дерева
        /// </summary>
        private string _value = "";

        /// <summary>
        ///     Коллекция элементов дерева
        /// </summary>
        public Dictionary<string, TreeItem> Items = new Dictionary<string, TreeItem>();

        /// <summary>
        ///     Поле Ключ
        /// </summary>
        public string KeyField { get; set; }

        /// <summary>
        ///     Поле Родитель
        /// </summary>
        public string ParentField { get; set; }

        /// <summary>
        ///     Поле Значение
        /// </summary>
        public string ValueField { get; set; }

        /// <summary>
        ///     Значение
        /// </summary>
        public string ValueText { get; set; }

        /// <summary>
        ///     Значение выбранного элемента дерева
        /// </summary>
        public override string Value
        {
            get { return _value; }
            set
            {
                if (!_value.Equals(value.Trim()))
                {
                    SetPropertyChanged("Value");
                    _value = value;
                }
            }
        }

        /// <summary>
        ///     Значиение тип int?
        /// </summary>
        public int? ValueInt
        {
            get
            {
                if (string.IsNullOrEmpty(Value))
                    return null;
                return int.Parse(Value);
            }
            set { Value = value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : null; }
        }

        /// <summary>
        ///     Получение источника данных
        /// </summary>
        public DataTableVoidDelegate GeTable { get; set; }

        /// <summary>
        ///     Установка фокуса
        /// </summary>
        public override void Focus()
        {
            if (V4Page.V4IsPostBack)
            {
                JS.Write("$('#{0}_0').dynatree('getTree').activateKey('{0}_{1}');", HtmlID, Value);
            }
        }

        /// <summary>
        ///     Инициализация
        /// </summary>
        /// <param name="e">аргумент</param>
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            JS.Write("$(function () {$('#" + HtmlID + "_0').dynatree({");
            JS.Write("onActivate: function (node) {cmd('ctrl','" + HtmlID + @"','vn',node.data.key.split('_')[1]);}");

            if (!IsReadOnly)
                JS.Write(@"
                    ,dnd: {
                        //preventVoidMoves: false, // Prevent dropping nodes 'before self', etc.
                        onDragStart: function (node) {
                            return true;
                        },
                        onDragEnter: function (node, sourceNode) {
                            //                        if (node.parent !== sourceNode.parent)
                            //                            return false;
                            //                        return ['before', 'after'];
                            return true;
                        },
                        onDrop: function (node, sourceNode, hitMode, ui, draggable) {
                            var confirmStr = '';
                            if (hitMode == 'over')
                                confirmStr = 'Вы действительно хотите сделать [' + v4_getTextFromHtml(sourceNode.data.title) + '] подчиненным для [' + v4_getTextFromHtml(node.data.title) + '] ?';
                            else if (hitMode == 'after')
                                confirmStr = 'Вы действительно хотите разместить [' + v4_getTextFromHtml(sourceNode.data.title) + '] после [' + v4_getTextFromHtml(node.data.title) + '] ?';
                            else if (hitMode == 'before')
                                confirmStr = 'Вы действительно хотите разместить [' + v4_getTextFromHtml(sourceNode.data.title) + '] перед [' + v4_getTextFromHtml(node.data.title) + '] ?';

                            if (confirm(confirmStr))
                                cmd('ctrl', 'dt', 'cmd', 'MoveNode', 'SourceNodeId', sourceNode.data.key, 'HitNodeId', node.data.key, 'Mode', hitMode);
                        }
                    }");

            JS.Write(@"})
            });");
        }

        /// <summary>
        ///     Загрузка контрола
        /// </summary>
        /// <param name="e">аргумент</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (Value.Length > 0)
                JS.Write("$('#{0}_0').dynatree('getTree').getNodeByKey('{0}_{1}').focus();", HtmlID, Value);
        }

        /// <summary>
        ///     Инициализация и регистрация скриптов
        /// </summary>
        public override void V4OnInit()
        {
            //string path1 = System.Configuration.ConfigurationSettings.AppSettings["path1"];
            //string path2 = System.Configuration.ConfigurationSettings.AppSettings["path2"];
            //string path3 = System.Configuration.ConfigurationSettings.AppSettings["path3"];
            //string path4 = System.Configuration.ConfigurationSettings.AppSettings["path4"];
            //string path5 = System.Configuration.ConfigurationSettings.AppSettings["path5"];

            //V4Page.RegisterScript("jquery", "<script src='" + path1 + "' type='text/javascript'></script>");
            //V4Page.RegisterScript("jquery-ui.custom", "<script src='" + path2 + "' type='text/javascript'></script>");
            //V4Page.RegisterScript("jquery.cookie", "<script src='" + path3 + "' type='text/javascript'></script>");
            //V4Page.RegisterScript("dynatree", "<script src='" + path4 + "' type='text/javascript'></script>");
            //V4Page.RegisterCss(path5);
        }

        /// <summary>
        ///     Перемещение элемента
        /// </summary>
        /// <param name="idSourceNode">ID ветви исходника</param>
        /// <param name="idHitNode">ID ветви назначения</param>
        /// <param name="moveType">Тип перемещения</param>
        /// <returns></returns>
        public virtual bool MoveItem(int idSourceNode, int idHitNode, int moveType)
        {
            return true;
        }

        /// <summary>
        ///     Обработка клиентских команд
        /// </summary>
        /// <param name="collection">Коллекция параметров</param>
        public override void ProcessCommand(NameValueCollection collection)
        {
            if (collection["t"] != null)
                ValueText = collection["t"].Trim();
            if (collection["cmd"] == "MoveNode")
            {
                var sourceNodeId = collection["SourceNodeId"];
                var _sourceNodeId = int.Parse(sourceNodeId.Split('_')[1]);

                var hitNodeId = collection["HitNodeId"];
                var _hitNodeId = int.Parse(hitNodeId.Split('_')[1]);
                var mode = collection["Mode"];

                var _mode = 0; //over
                if (mode.Equals("after"))
                    _mode = 1;
                else if (mode.Equals("before"))
                    _mode = 2;
                if (MoveItem(_sourceNodeId, _hitNodeId, _mode))
                    JS.Write(
                        "var t{0}=$('#{0}_0').dynatree('getTree');t{0}.getNodeByKey('{1}').move(t{0}.getNodeByKey('{2}'),'{3}');",
                        HtmlID, sourceNodeId, hitNodeId, mode);
            }
            else if (collection["vn"] != null)
            {
                var oldValue = _value;
                _value = collection["vn"];
                OnChanged(new ProperyChangedEventArgs(oldValue, collection["vn"]));
            }
            base.ProcessCommand(collection);
        }

        /// <summary>
        ///     Отрисовка контрола
        /// </summary>
        /// <param name="w">Поток</param>
        protected override void RenderControlBody(TextWriter w)
        {
            w.Write("<div id='{0}_0'><ul id='{0}Data' style='display: none;'>", HtmlID);
            RenderTree(w);
            w.Write("</ul></div>");
        }

        /// <summary>
        ///     Обновление контрола
        /// </summary>
        public void Refresh()
        {
            V4Page.RefreshHtmlBlock(HtmlID, RenderControlBody);
            JS.Write(" $(function () {$('#" + HtmlID + @"_0').dynatree({
                    onActivate: function (node) {cmd('ctrl','" + HtmlID + @"','vn',node.data.key.split('_')[1]);}");
            if (!IsReadOnly)
                JS.Write(@"
                    ,dnd: {
                        //preventVoidMoves: false, // Prevent dropping nodes 'before self', etc.
                        onDragStart: function (node) {
                            return true;
                        },
                        onDragEnter: function (node, sourceNode) {
                            //                        if (node.parent !== sourceNode.parent)
                            //                            return false;
                            //                        return ['before', 'after'];
                            return true;
                        },
                        onDrop: function (node, sourceNode, hitMode, ui, draggable) {
                            var confirmStr = '';
                            if (hitMode == 'over')
                                confirmStr = 'Вы действительно хотите сделать [' + v4_getTextFromHtml(sourceNode.data.title) + '] подчиненным для [' + v4_getTextFromHtml(node.data.title) + '] ?';
                            else if (hitMode == 'after')
                                confirmStr = 'Вы действительно хотите разместить [' + v4_getTextFromHtml(sourceNode.data.title) + '] после [' + v4_getTextFromHtml(node.data.title) + '] ?';
                            else if (hitMode == 'before')
                                confirmStr = 'Вы действительно хотите разместить [' + v4_getTextFromHtml(sourceNode.data.title) + '] перед [' + v4_getTextFromHtml(node.data.title) + '] ?';

                            if (confirm(confirmStr))
                                cmd('ctrl', 'dt', 'cmd', 'MoveNode', 'SourceNodeId', sourceNode.data.key, 'HitNodeId', node.data.key, 'Mode', hitMode);
                        }
                    }");

            JS.Write(@"});
            });");
            if (Value.Length > 0)
                JS.Write("$('#{0}_0').dynatree('getTree').getNodeByKey('{0}_{1}').focus();", HtmlID, Value);
        }

        /// <summary>
        ///     Отрисовка элемента дерева
        /// </summary>
        /// <param name="w">Поток</param>
        /// <param name="r">Источник данных</param>
        public virtual void RenderItem(TextWriter w, DataRow r)
        {
            w.Write("<li id='{0}_{1}'{2} data=\"icon:'/s/bed.gif'\">", HtmlID, r[KeyField],
                Value.Equals(r[KeyField].ToString()) ? " class='active'" : "");
            w.Write(HttpUtility.HtmlDecode(r[ValueField].ToString()));
        }

        /// <summary>
        ///     Отрисовка дерева
        /// </summary>
        /// <param name="w">Поток</param>
        public void RenderTree(TextWriter w)
        {
            var dt = GeTable();
            var deep = 0;
            var parents = new Dictionary<int, int>();

            foreach (DataRow row in dt.Rows)
            {
                var l = (int) row["L"];
                var r = (int) row["R"];

                var parentDeep = (row["Parent"] == null || row["Parent"] == DBNull.Value ||
                                  !parents.ContainsKey((int) row["Parent"]))
                    ? 0
                    : parents[(int) row["Parent"]];

                var d = deep - parentDeep;
                if (d > 0)
                {
                    for (var i = 0; i < d; i++)
                        w.Write("</ul>");
                    deep -= d;
                }

                RenderItem(w, row);

                if (r - l > 1)
                {
                    deep++;
                    parents[(int) row[KeyField]] = deep;
                    w.Write("<ul>");
                }
            }

            for (var i = 0; i < deep; i++)
                w.Write("</ul>");
        }
    }
}