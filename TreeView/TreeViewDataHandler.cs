using System;
using System.Collections.Generic;
using System.Data;
using System.Resources;
using System.Web;
using System.Web.Script.Serialization;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.DALC;
using Kesco.Lib.Localization;
using Kesco.Lib.Web.Controls.V4.Common;
using System.Collections.Specialized;
using Kesco.Lib.Web.Settings.Parameters;
using System.Threading;
using System.Globalization;
using Kesco.Lib.BaseExtention.Enums.Docs;
using SQLQueries = Kesco.Lib.Entities.SQLQueries;

namespace Kesco.Lib.Web.Controls.V4.TreeView
{
    public class TreeViewDataHandler : IHttpHandler
    {

        protected static string ReturnId { get; set; }
        protected static string ReturnType { get; set; }
        protected static string OrderByField { get; set; }
        protected static string SearchText { get; set; }
        protected static string SearchParam { get; set; }
        protected static string MassLoad { get; set; }
        protected static string StateLoad { get; set; }
        protected static string OpenList { get; set; }
        protected static string SelectedNode { get; set; }
        protected static string LoadId { get; set; }

        protected static string IDPage { get; set; }
        protected static string CtrlID { get; set; }

        Page page;
        protected ResourceManager Resx;
        protected TreeViewDbSourceSettings dbSource;

        /// <summary>
        ///     Обработка клиентских команд
        /// </summary>
        /// <param name="context">Текущий контекст</param>
        public virtual void ProcessRequest(HttpContext context)
        {
            var type = context.Request.QueryString["type"];

            if (type == "save_state")
            {
                var clid = context.Request.QueryString["Clid"];
                var paramName = context.Request.QueryString["ParamName"];
                var state = context.Request.Form["state"];
                var parametersManager = new AppParamsManager(Convert.ToInt32(clid), new StringCollection());
                parametersManager.Params.Add(new AppParameter(paramName, state, AppParamType.SavedWithClid));
                parametersManager.SaveParams();
                return;
            }

            var id = string.IsNullOrWhiteSpace(context.Request.QueryString["nodeid"]) ||
                 context.Request.QueryString["nodeid"] == "#"
                ? 0
                : int.Parse(context.Request.QueryString["nodeid"]);
            ReturnId = string.IsNullOrWhiteSpace(context.Request.QueryString["return"])
                ? ""
                : context.Request.QueryString["return"];
            ReturnType = string.IsNullOrWhiteSpace(context.Request.QueryString["returntype"])
                ? ""
                : context.Request.QueryString["returntype"];
            OrderByField = string.IsNullOrWhiteSpace(context.Request.QueryString["orderBy"])
                ? "L"
                : context.Request.QueryString["orderBy"];
            SearchText = string.IsNullOrWhiteSpace(context.Request.QueryString["searchText"])
                ? ""
                : context.Request.QueryString["searchText"];
            SearchParam = string.IsNullOrWhiteSpace(context.Request.QueryString["searchParam"])
                ? ""
                : context.Request.QueryString["searchParam"];
            MassLoad = string.IsNullOrWhiteSpace(context.Request.QueryString["massload"])
                ? ""
                : context.Request.QueryString["massload"];
            IDPage = string.IsNullOrWhiteSpace(context.Request.QueryString["idpage"])
                ? ""
                : context.Request.QueryString["idpage"];
            CtrlID = string.IsNullOrWhiteSpace(context.Request.QueryString["ctrlid"])
                ? ""
                : context.Request.QueryString["ctrlid"];
            StateLoad = string.IsNullOrWhiteSpace(context.Request.QueryString["stateLoad"])
                ? "false"
                : context.Request.QueryString["stateLoad"];
            LoadId = string.IsNullOrWhiteSpace(context.Request.QueryString["loadId"])
                ? "0"
                : context.Request.QueryString["loadId"];
            var json = string.Empty;

            var p = (Page)context.Application[IDPage];

            if (p == null) return;

            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture(p.CurrentUser.Language);
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(p.CurrentUser.Language);
            Resx = Resources.Resx;

            var treeview = (TreeView)p.V4Controls[CtrlID];
            dbSource = treeview.DbSourceSettings;

            if (SearchText.IsNullEmptyOrZero())
            {
                if (StateLoad == "true")
                {
                    if (!LoadId.IsNullEmptyOrZero())
                    {
                        json = GetStateTreeData(LoadId);
                    }
                    else
                    {
                        var clid = "0";
                        var paramName = treeview.ParamName;
                        var parametersManager = new AppParamsManager(Convert.ToInt32(clid), new StringCollection { paramName });
                        var appParam = parametersManager.Params.Find(x => x.Name == paramName);
                        var jsonState = appParam.Value;
                        if (jsonState.IndexOf("open") > 0)
                        {
                            var openState = jsonState.Substring(jsonState.IndexOf("open"));
                            var startArray = openState.IndexOf("[");
                            var endArray = openState.IndexOf("]");
                            if (startArray != -1)
                            {
                                OpenList = openState.Substring(startArray + 1, (endArray - startArray) - 1).Replace("\"", "'");
                            }
                        }
                        if (jsonState.IndexOf("selected") > 0)
                        {
                            var selectedState = jsonState.Substring(jsonState.IndexOf("selected"));
                            var startArray = selectedState.IndexOf("[");
                            var endArray = selectedState.IndexOf("]");
                            if (startArray != -1)
                            {
                                SelectedNode = selectedState.Substring(startArray + 1, (endArray - startArray) - 1).Replace("\"", "");
                                var sqlParams = new Dictionary<string, object> { { "@id", SelectedNode } };

                                var sqlQuery = string.Format(SQLQueries.SELECT_ПолучениеОткрытыхУзловДерева, dbSource.ViewName, dbSource.PkField);

                                var dtParent = DBManager.GetData(
                                    sqlQuery, dbSource.ConnectionString, CommandType.Text, sqlParams); 
                                
                                OpenList = "";
                                foreach (DataRow row in dtParent.Rows)
                                {
                                    if (OpenList != "") OpenList += ",";
                                    OpenList += "'" + row[dbSource.PkField] + "'";
                                }
                            }
                        }

                        json = GetStateTreeData();
                    }
                }
                else
                {
                    json = GetTreedata(id);
                }
            }
            else
            {
                var jsonArr = GetSearchTreeData(SearchText, SearchParam);
                json = jsonArr[0];
                treeview.SearchResultCount = Convert.ToInt32(jsonArr[1]);
            }

            context.Response.ContentType = "text/json";
            context.Response.Write(json);
        }

        public bool IsReusable
        {
            get { return false; }
        }

        /// <summary>
        ///     Получение данных для дерева
        /// </summary>
        /// <param name="nodeid">Идентификатор узла</param>
        /// <returns>JSON данные</returns>
        private string GetTreedata(int nodeid)
        {
            var sqlParams = new Dictionary<string, object> {{"@Код", nodeid}, {"@Потомки", 1}};

            var sql = GetTreedata_Sql(OrderByField, SearchText, SearchParam);
            var dt = DBManager.GetData(sql, dbSource.ConnectionString, CommandType.Text, sqlParams);

            Node root = null;
            if (nodeid == 0)
            {
                root = new Node
                {
                    id = "0",
                    text = dbSource.RootName,
                    type = "folder",
                    state = new NodeState {opened = true, selected = false, loaded = true},
                    li_attr = new NodeAttr { text = dbSource.RootName, parentId = "0"}
                };
            }
            else
            {
                sqlParams["@Потомки"] = 0;

                var dtr = DBManager.GetData(sql, dbSource.ConnectionString, CommandType.Text, sqlParams);
                if (dtr.Rows.Count > 0)
                {
                    var rootNode = dtr.Rows[0];
                    root = new Node
                    {
                        id = nodeid.ToString(),
                        type = Convert.ToInt32(rootNode["ЕстьДети"]) == 1 ? "file" : "folder",
                        text = GetPrefixIcon(rootNode) + rootNode["text"] + GetPostFixIcon(rootNode),
                        state = new NodeState {opened = true, selected = false, loaded = true},
                        li_attr = new NodeAttr { text = rootNode["text"].ToString(), parentId = rootNode["ParentId"].ToString() }
                    };
                }
            }

            var view = new DataView(dt) {RowFilter = "ParentId=" + nodeid};

            foreach (DataRowView kvp in view)
            {
                var parentId = kvp["Id"].ToString();

                var node = new Node
                {
                    id = kvp["Id"].ToString(),
                    type = (int) kvp["ЕстьДети"] == 1 ? "file" : "folder",
                    text = GetPrefixIcon(kvp) + kvp["text"] + GetPostFixIcon(kvp),
                    state = new NodeState
                    {
                        opened = !kvp["Фильтр"].ToString().IsNullEmptyOrZero(), selected = false,
                        loaded = Convert.ToInt32(kvp["ЕстьДети"]) == 1
                    },
                    li_attr = new NodeAttr { text = kvp["text"].ToString(), parentId = kvp["ParentId"].ToString() }
                };

                root.children.Add(node);
                //AddChildItems(dt, node, parentId);
            }

            return new JavaScriptSerializer().Serialize(root);
        }

        /// <summary>
        ///     Получение данных для дерева при поиске
        /// </summary>
        /// <param name="searchText">Что ищем</param>
        /// <param name="searchParam">Как ищем</param>
        /// <returns>JSON данные</returns>
        protected string[] GetSearchTreeData(string searchText, string searchParam)
        {
            var parametersOut = new Dictionary<string, object> {{"@КоличествоНайденных", 0}};
            var sql = GetTreedata_Sql(OrderByField, SearchText, SearchParam);
            var recCount = 0;
            //var dt = DBManager.GetData(sql, DbSourceSettings.ConnectionString);
            var dt = new DataTable();
            using (var dbReader = new DBReader(sql, CommandType.Text, dbSource.ConnectionString, null,
                parametersOut))
            {
                if (dbReader.HasRows) dt.Load(dbReader);
                dbReader.Close();
                recCount = Convert.ToInt16(parametersOut["@КоличествоНайденных"]);
            }

            Node root = null;
            root = new Node
            {
                id = "0",
                text = dbSource.RootName,
                type = "folder",
                state = new NodeState {opened = true, selected = false, loaded = true},
                li_attr = new NodeAttr { text = dbSource.RootName, parentId = "0" }
            };

            var view = new DataView(dt) {RowFilter = "[ParentId] is null"};
            foreach (DataRowView row in view)
            {
                var parentId = row["Id"].ToString();
                var node = new Node
                {
                    id = row["Id"].ToString(),
                    type = Convert.ToInt32(row["ЕстьДети"]) == 1 ? "file" : "folder",
                    text = GetPrefixIcon(row) + GetFilteredText(row["text"].ToString(), searchText, row["BitMask"].ToString()) + 
                           GetPostFixIcon(row) + GetFilter(row["BitMask"].ToString()),
                    state = new NodeState
                    {
                        opened = row["BitMask"].ToString() != "4" && row["BitMask"].ToString() != "5", selected = false,
                        loaded = row["BitMask"].ToString() != "4" && row["BitMask"].ToString() != "5"
                    },
                    li_attr = new NodeAttr { text = row["text"].ToString(), parentId = row["ParentId"].ToString() }
                };
                root.children.Add(node);
                AddChildItems(dt, node, parentId, searchText);
            }

            return new[] {new JavaScriptSerializer().Serialize(root), recCount.ToString()};
        }

        /// <summary>
        ///     Получение данных для дерева для State
        /// </summary>
        /// <returns>JSON данные</returns>
        protected string GetStateTreeData(string id="")
        {
            if (!id.IsNullEmptyOrZero())
            {
                SelectedNode = id;
                var sqlParams = new Dictionary<string, object> { { "@id", id } };

                var sqlQuery = string.Format(SQLQueries.SELECT_ПолучениеОткрытыхУзловДерева, dbSource.ViewName, dbSource.PkField);

                var dtParent = DBManager.GetData(
                    sqlQuery, Settings.Config.DS_user, CommandType.Text, sqlParams);
               
                OpenList = "";
                foreach (DataRow row in dtParent.Rows)
                {
                    if (OpenList != "") OpenList += ",";
                    OpenList += "'" + row[dbSource.PkField] + "'";
                }
            }

            if (OpenList.IsNullEmptyOrZero()) OpenList = "'0'";

            var sql = GetTreedata_Sql(OrderByField, "", "", OpenList);
            var dt = DBManager.GetData(sql, dbSource.ConnectionString);
            Node root = null;
            root = new Node
            {
                id = "0",
                text = dbSource.RootName,
                type = "folder",
                state = new NodeState {opened = true, selected = false, loaded = true},
                li_attr = new NodeAttr { text = dbSource.RootName, parentId = "0" }
            };

            var view = new DataView(dt) {RowFilter = "[ParentId] is null"};
            foreach (DataRowView row in view)
            {
                var parentId = row["Id"].ToString();
                var s = row["BitMask"].ToString();
                bool hasChild = Convert.ToInt32(row["ЕстьДети"]) > 1;


                var node = new Node
                {
                    id = row["Id"].ToString(),
                    type = !hasChild ? "file" : "folder",
                    text = GetPrefixIcon(row) + row["text"] + GetPostFixIcon(row),
                    state = new NodeState
                    {
                        opened = row["BitMask"].ToString() == "1" || !hasChild,
                        selected = row["Id"].ToString() == SelectedNode,
                        loaded = row["BitMask"].ToString() == "1" || !hasChild
                    },
                    li_attr = new NodeAttr { text = row["text"].ToString(), parentId = row["ParentId"].ToString() }
                };
                root.children.Add(node);
                AddStateChildItems(dt, node, parentId);
            }

            return new JavaScriptSerializer().Serialize(root);
        }

        private void AddStateChildItems(DataTable dt, Node parentNode, string parentId)
        {
            var viewItem = new DataView(dt) { RowFilter = "[ParentId]=" + parentId };
            foreach (DataRowView childView in viewItem)
            {
                bool hasChild = Convert.ToInt32(childView["ЕстьДети"]) > 1;
                var node = new Node
                {
                    id = childView["Id"].ToString(),
                    text = GetPrefixIcon(childView) + childView["text"] + GetPostFixIcon(childView),
                    type = !hasChild ? "file" : "folder",
                    state = new NodeState {
                        opened = childView["BitMask"].ToString() == "1" || !hasChild,
                        selected = childView["Id"].ToString() == SelectedNode,
                        loaded = childView["BitMask"].ToString() == "1" || !hasChild
                    },
                    li_attr = new NodeAttr { text = childView["text"].ToString(), parentId = childView["ParentId"].ToString() }
                };
                parentNode.children.Add(node);
                var pId = childView["Id"].ToString();
                AddStateChildItems(dt, node, pId);
            }
        }

        /// <summary>
        ///     Получение данных для дерева - дети узла
        /// </summary>
        /// <param name="dt">Источник данных</param>
        /// <param name="parentNode">Корневой узел</param>
        /// <param name="parentId">Идентификатор корневого узла</param>
        /// <param name="searchText">Строка поиска</param>
        private void AddChildItems(DataTable dt, Node parentNode, string parentId, string searchText = "")
        {
            var viewItem = new DataView(dt) {RowFilter = "[ParentId]=" + parentId};
            foreach (DataRowView childView in viewItem)
            {
                var node = new Node
                {
                    id = childView["Id"].ToString(),
                    text = searchText.IsNullEmptyOrZero()
                        ? GetPrefixIcon(childView) + childView["text"] + GetPostFixIcon(childView)
                        : GetPrefixIcon(childView) + GetFilteredText(childView["text"].ToString(), searchText, childView["BitMask"].ToString()) +
                          GetPostFixIcon(childView) + GetFilter(childView["BitMask"].ToString()),
                    type = Convert.ToInt32(childView["ЕстьДети"]) == 1 ? "file" : "folder",
                    state = new NodeState {opened = true, selected = false, loaded = true},
                    li_attr = new NodeAttr { text = childView["text"].ToString(), parentId = childView["ParentId"].ToString() }
                };
                parentNode.children.Add(node);
                var pId = childView["Id"].ToString();
                AddChildItems(dt, node, pId, searchText);
            }
        }

        /// <summary>
        ///     Получение строки запроса по переданным параметрам
        /// </summary>
        /// <param name="orderBy">Порядок сортировки</param>
        /// <param name="searchText">Строка поиска</param>
        /// <param name="searchParam">Параметры поиска</param>
        /// <param name="openList">Список открытых нодов</param>
        /// <returns></returns>
        protected virtual string GetTreedata_Sql(string orderBy = "L", string searchText = "", string searchParam = "", string openList = "")
        {
            if (orderBy != "L") orderBy = dbSource.NameField;

            if (searchText.IsNullEmptyOrZero())
            {
                if (openList.IsNullEmptyOrZero())
                    return string.Format(SQLQueries.SELECT_ЗагрузкаУзловДерева, dbSource.ViewName, dbSource.PkField, dbSource.NameField);

                return $@"
                SET NOCOUNT ON

                IF OBJECT_ID('tempdb.#TreeFilter') IS NOT NULL DROP TABLE #TreeFilter
                CREATE TABLE #TreeFilter(
                        TempID int IDENTITY(1,1),
                        [{dbSource.PkField}] [int],
                        {dbSource.NameField} [varchar](300),       
                        [Parent] [int],
                        [L] [int],
                        [R] [int],
                        {(string.IsNullOrEmpty(dbSource.ModifyUserField) ? string.Empty : "[Изменил] [int],")} 
                        {(string.IsNullOrEmpty(dbSource.ModifyDateField) ? string.Empty : "[Изменено] [datetime],")} 
                        BitMask tinyint    
                )

                INSERT #TreeFilter
                SELECT	Parent.[{dbSource.PkField}],
                        Parent.{dbSource.NameField},      
                        Parent.[Parent],
                        Parent.[L],
                        Parent.[R],
                        {(string.IsNullOrEmpty(dbSource.ModifyUserField) ? string.Empty : "Parent." + dbSource.ModifyUserField + " [Изменил],")} 
                        {(string.IsNullOrEmpty(dbSource.ModifyDateField) ? string.Empty : "Parent." + dbSource.ModifyDateField + " [Изменено],")} 
                        1 BitMask
                FROM	{dbSource.ViewName} Parent 
                WHERE EXISTS(SELECT * FROM {dbSource.ViewName} Child 
					                WHERE	Child.{dbSource.PkField} IN ({openList})
						                AND Parent.L <=	Child.L AND Parent.R>=Child.R)
                ORDER BY Parent.L

                INSERT #TreeFilter
                SELECT	Child.[{dbSource.PkField}],
                        Child.{dbSource.NameField},      
                        Child.[Parent],
                        Child.[L],
                        Child.[R],
                        {(string.IsNullOrEmpty(dbSource.ModifyUserField) ? string.Empty : "Child." + dbSource.ModifyUserField + " [Изменил],")} 
                        {(string.IsNullOrEmpty(dbSource.ModifyDateField) ? string.Empty : "Child." + dbSource.ModifyDateField + " [Изменено],")}
                        2 BitMask
                FROM	{dbSource.ViewName} Parent
                LEFT JOIN {dbSource.ViewName} Child ON Child.Parent = Parent.{dbSource.PkField}
                WHERE Parent.{dbSource.PkField} IN ({openList}) AND NOT EXISTS(SELECT * FROM #TreeFilter X WHERE Child.{dbSource.PkField} = X.{dbSource.PkField})
                ORDER BY Parent.L

                INSERT #TreeFilter 
                SELECT  [{dbSource.PkField}],
                        {dbSource.NameField},      
                        [Parent],
                        [L],
                        [R],
                        {(string.IsNullOrEmpty(dbSource.ModifyUserField) ? string.Empty : dbSource.ModifyUserField + " [Изменил],")} 
                        {(string.IsNullOrEmpty(dbSource.ModifyDateField) ? string.Empty : dbSource.ModifyDateField + " [Изменено],")} 
                        4 BitMask
                FROM    {dbSource.ViewName} 
                WHERE   Parent IS NULL                                  
                        AND NOT EXISTS(SELECT * FROM #TreeFilter X WHERE {dbSource.ViewName}.{dbSource.PkField} = X.{dbSource.PkField})

                SELECT #TreeFilter.[{dbSource.PkField}] id,
                        #TreeFilter.{dbSource.NameField} text,      
                        #TreeFilter.[Parent] ParentId,
                        #TreeFilter.[L],
                        #TreeFilter.[R],
                        {(string.IsNullOrEmpty(dbSource.ModifyUserField) ? string.Empty : "#TreeFilter.[Изменил],")} 
                        {(string.IsNullOrEmpty(dbSource.ModifyDateField) ? string.Empty : "#TreeFilter.[Изменено],")} 
                        #TreeFilter.BitMask,
                        #TreeFilter.R-#TreeFilter.L ЕстьДети
                FROM #TreeFilter
                ORDER BY {orderBy}
                DROP TABLE #TreeFilter
                ";
            }

            searchText = searchParam == "1" ? searchText + "%" : "%" + searchText + "%";

            return $@"
                DECLARE @МаксимальноеКоличествоНайденных int = 100
                SET NOCOUNT ON
               
                IF OBJECT_ID('tempdb..#TreeFilter') IS NOT NULL DROP TABLE #TreeFilter
                CREATE TABLE #TreeFilter(
                        TempID int IDENTITY(1,1),
                        [{dbSource.PkField}] [int],
                        {dbSource.NameField} [varchar](300),       
                        [Parent] [int],
                        [L] [int],
                        [R] [int],
                        {(string.IsNullOrEmpty(dbSource.ModifyUserField) ? string.Empty : "[Изменил] [int],")} 
                        {(string.IsNullOrEmpty(dbSource.ModifyDateField) ? string.Empty : "[Изменено] [datetime],")} 
                        BitMask tinyint          
                )

                INSERT #TreeFilter
                SELECT  [{dbSource.PkField}],
                        {dbSource.NameField},      
                        [Parent],
                        [L],
                        [R],
                        {(string.IsNullOrEmpty(dbSource.ModifyUserField) ? string.Empty : dbSource.ModifyUserField + " [Изменил],")} 
                        {(string.IsNullOrEmpty(dbSource.ModifyDateField) ? string.Empty : dbSource.ModifyDateField + " [Изменено],")} 
                        1 BitMask
                FROM    {dbSource.ViewName} 
                WHERE   {dbSource.NameField} LIKE '{searchText}'
                ORDER BY L
 
                SET @КоличествоНайденных = @@ROWCOUNT
                DELETE #TreeFilter WHERE TempID > @МаксимальноеКоличествоНайденных
 
                UPDATE  Parent
                SET     BitMask = BitMask ^ 2
                FROM    #TreeFilter Parent
                WHERE   EXISTS(SELECT * FROM #TreeFilter Child WHERE Parent.L < Child.L AND Parent.R > Child.R)
 
                INSERT  #TreeFilter
                SELECT  [{dbSource.PkField}],
                        {dbSource.NameField},      
                        [Parent],
                        [L],
                        [R],
                        {(string.IsNullOrEmpty(dbSource.ModifyUserField) ? string.Empty : dbSource.ModifyUserField + " [Изменил],")} 
                        {(string.IsNullOrEmpty(dbSource.ModifyDateField) ? string.Empty : dbSource.ModifyDateField + " [Изменено],")} 
                        2 BitMask
                FROM    {dbSource.ViewName} Parent 
                WHERE   EXISTS( SELECT * FROM #TreeFilter Child 
                                WHERE Parent.L <= Child.L AND Parent.R>=Child.R)                                        
                        AND NOT EXISTS(SELECT * FROM #TreeFilter X WHERE Parent.{dbSource.PkField} = X.{dbSource.PkField})
 
                UPDATE  #TreeFilter
                SET     BitMask = BitMask ^ 4
                WHERE   Parent IS NULL
 
                INSERT #TreeFilter 
                SELECT  [{dbSource.PkField}],
                        {dbSource.NameField},      
                        [Parent],
                        [L],
                        [R],
                        {(string.IsNullOrEmpty(dbSource.ModifyUserField) ? string.Empty : dbSource.ModifyUserField + " [Изменил],")} 
                        {(string.IsNullOrEmpty(dbSource.ModifyDateField) ? string.Empty : dbSource.ModifyDateField + " [Изменено],")} 
                        4 BitMask
                FROM    {dbSource.ViewName} 
                WHERE   Parent IS NULL                                  
                        AND NOT EXISTS(SELECT * FROM #TreeFilter X WHERE {dbSource.ViewName}.{dbSource.PkField} = X.{dbSource.PkField})
        
                SELECT [{dbSource.PkField}] id,
                        {dbSource.NameField} text,      
                        [Parent] ParentId,
                        [L],
                        [R],
                        {(string.IsNullOrEmpty(dbSource.ModifyUserField) ? string.Empty : "[Изменил],")} 
                        {(string.IsNullOrEmpty(dbSource.ModifyDateField) ? string.Empty : "[Изменено],")} 
                        BitMask,
                        R-L ЕстьДети
                FROM #TreeFilter
                ORDER BY {orderBy}
                DROP TABLE #TreeFilter
                ";
        }

        /// <summary>
        ///     Возвращает "отфильтровано" по переданной маске
        /// </summary>
        /// <param name="bitMask">маска</param>
        /// <returns></returns>
        private string GetFilter(string bitMask)
        {
            if (bitMask == "2" || bitMask == "3" || bitMask == "6" || bitMask == "7")
                return "<font color='green'> (" + Resx.GetString("lblFiltered") + ")</font>";
            return "";
        }

        /// <summary>
        ///     Возвращает выделенный текст поиска в названии узла
        /// </summary>
        /// <param name="text"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        private string GetFilteredText(string text, string filter, string bitMask)
        {
            if (filter.IsNullEmptyOrZero()) return text;
            // не выделяем текст, если не найдено (для случая "начинается с")
            if (bitMask == "2" || bitMask == "4" || bitMask == "6") return text;
            if (!text.ToLower().Contains(SearchText.ToLower())) return text;
            var curPos = text.ToLower().IndexOf(SearchText.ToLower());
            while (curPos != -1)
            {
                text = text.Insert(curPos, "<span style='background-color:#FFFF00'>");
                curPos = curPos + 40 + SearchText.Length - 1;
                text = text.Insert(curPos, "</span>");
                curPos = text.ToLower().IndexOf(SearchText.ToLower(), curPos);
            }

            return text;
        }

        /// <summary>
        ///     Вывод иконок до названия
        /// </summary>
        /// <param name="dt">DataRow</param>
        /// <returns>возвращаямая строка, содержащая готовую разметку</returns>
        protected virtual string GetPrefixIcon(DataRow dt)
        {
            return "";
        }

        /// <summary>
        ///     Вывод иконок до названия
        /// </summary>
        /// <param name="dt">DataRowView</param>
        /// <returns>возвращаямая строка, содержащая готовую разметку</returns>
        protected virtual string GetPrefixIcon(DataRowView dt)
        {
            return "";
        }

        /// <summary>
        ///     Вывод иконок после названия
        /// </summary>
        /// <param name="dt">DataRow</param>
        /// <returns>возвращаямая строка, содержащая готовую разметку</returns>
        protected virtual string GetPostFixIcon(DataRow dt)
        {
            return "";
        }

        /// <summary>
        ///     Вывод иконок после названия
        /// </summary>
        /// <param name="dt">DataRowView</param>
        /// <returns>возвращаямая строка, содержащая готовую разметку</returns>
        protected virtual string GetPostFixIcon(DataRowView dt)
        {
            return "";
        }

        /// <summary>
        ///     Класс узла дерева
        /// </summary>
        private class Node
        {
            public Node()
            {
                children = new List<Node>();
            }

            public string id { get; set; }
            public string text { get; set; }
            public NodeState state { get; set; }
            public List<Node> children { get; set; }
            public string type { get; set; }
            public NodeAttr li_attr { get; set; }
        }

        /// <summary>
        ///     Класс State узла
        /// </summary>
        private class NodeState
        {
            public bool opened { get; set; }
            public bool selected { get; set; }
            public bool loaded { get; set; }
        }

        private class NodeAttr
        {
            public string parentId { get; set; }
            public string text { get; set; }
        }

    }
}