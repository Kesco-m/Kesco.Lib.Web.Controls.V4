using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Resources;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web;
using System.Web.Script.Serialization;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.BaseExtention.Enums.Docs;
using Kesco.Lib.DALC;
using Kesco.Lib.Localization;
using Kesco.Lib.Web.Controls.V4.Common;
using Kesco.Lib.Web.Settings;
using Kesco.Lib.Web.Settings.Parameters;
using Kesco.Lib.Web.SignalR;
using SQLQueries = Kesco.Lib.Entities.SQLQueries;

namespace Kesco.Lib.Web.Controls.V4.TreeView
{
    public class TreeViewDataHandler : IHttpHandler
    {
        protected TreeViewDbSourceSettings dbSource;

        protected ResourceManager Resx;
        protected TreeView treeView;
        protected static string RootIds { get; set; }
        protected static string SelectedIds { get; set; }
        protected static string ReturnId { get; set; }
        protected static string ReturnType { get; set; }
        protected static string ReturnCondition { get; set; }
        protected static string OrderByField { get; set; }
        protected static string OrderByDirection { get; set; }
        protected static string SearchText { get; set; }
        protected static string IsDefaultFilter { get; set; }
        protected static string SearchParam { get; set; }
        protected static bool StateLoad { get; set; }
        protected static string OpenList { get; set; }
        protected static string SelectedNode { get; set; }
        protected static string LoadId { get; set; }

        protected static string IDPage { get; set; }
        protected static string CtrlID { get; set; }

        protected static bool ShowTopNodesInSearchResult { get; set; }

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

            var id = string.IsNullOrWhiteSpace(context.Request.QueryString["nodeid"])
                     || context.Request.QueryString["nodeid"] == "#"
                ? 0
                : int.Parse(context.Request.QueryString["nodeid"]);

            if (!string.IsNullOrWhiteSpace(context.Request.QueryString["rootids"]))
                RootIds = context.Request.QueryString["rootids"];
            else if (!string.IsNullOrWhiteSpace(context.Request.QueryString["root"]))
                RootIds = context.Request.QueryString["root"];
            else if (!string.IsNullOrWhiteSpace(context.Request.QueryString["parent"]))
                RootIds = context.Request.QueryString["parent"];
            else
                RootIds = string.Empty;

            SelectedIds = string.IsNullOrWhiteSpace(context.Request.QueryString["selectedids"])
                ? string.Empty
                : context.Request.QueryString["selectedids"];

            ReturnId = string.IsNullOrWhiteSpace(context.Request.QueryString["return"])
                ? ""
                : context.Request.QueryString["return"];
            ReturnType = string.IsNullOrWhiteSpace(context.Request.QueryString["returntype"])
                ? ""
                : context.Request.QueryString["returntype"];
            ReturnCondition = string.IsNullOrWhiteSpace(context.Request.QueryString["returncondition"])
                ? ""
                : context.Request.QueryString["returncondition"];
            OrderByField = string.IsNullOrWhiteSpace(context.Request.QueryString["orderByField"])
                ? "L"
                : context.Request.QueryString["orderByField"];
            OrderByDirection = string.IsNullOrWhiteSpace(context.Request.QueryString["orderByDirection"])
                ? "ASC"
                : context.Request.QueryString["orderByDirection"];
            SearchText = string.IsNullOrWhiteSpace(context.Request.QueryString["searchText"])
                ? ""
                : context.Request.QueryString["searchText"];
            SearchParam = string.IsNullOrWhiteSpace(context.Request.QueryString["searchParam"])
                ? ""
                : context.Request.QueryString["searchParam"];
            IsDefaultFilter = string.IsNullOrWhiteSpace(context.Request.QueryString["isDefaultFilter"])
                ? ""
                : context.Request.QueryString["isDefaultFilter"];
            //MassLoad = string.IsNullOrWhiteSpace(context.Request.QueryString["massload"])
            //    ? ""
            //    : context.Request.QueryString["massload"];
            IDPage = string.IsNullOrWhiteSpace(context.Request.QueryString["idpage"])
                ? ""
                : context.Request.QueryString["idpage"];
            CtrlID = string.IsNullOrWhiteSpace(context.Request.QueryString["ctrlid"])
                ? ""
                : context.Request.QueryString["ctrlid"];
            StateLoad = string.IsNullOrWhiteSpace(context.Request.QueryString["stateLoad"]) ||
                        !string.IsNullOrWhiteSpace(RootIds)
                ? false
                : bool.Parse(context.Request.QueryString["stateLoad"]);
            LoadId = string.IsNullOrWhiteSpace(context.Request.QueryString["loadId"])
                ? "0"
                : context.Request.QueryString["loadId"];
            ShowTopNodesInSearchResult = string.IsNullOrWhiteSpace(context.Request.QueryString["searchShowTop"])
                ? false
                : bool.Parse(context.Request.QueryString["searchShowTop"]);
            var json = string.Empty;


            var p = KescoHub.GetPage(IDPage) as Page;

            if (p == null) return;

            Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture =
                CorporateCulture.GetCorporateCulture(p.CurrentUser.Language);
            Resx = Resources.Resx;

            treeView = (TreeView) p.V4Controls[CtrlID];
            dbSource = treeView.DbSourceSettings;
            OpenList = string.Empty;

            var partialLoad = id == 0 && !string.IsNullOrEmpty(RootIds);
            var openSingleNode = id == 0 && !LoadId.IsNullEmptyOrZero();
            var openSeveralNodes = id == 0 && !string.IsNullOrEmpty(SelectedIds);
            var firstLoad = IsDefaultFilter == "true";

            //if (string.IsNullOrEmpty(SearchText) && string.IsNullOrEmpty(SearchParam) || SearchParam == "clearfilter")
            if (SearchParam != "filter")
            {
                treeView.SearchResultCount = 0;

                if (openSingleNode)
                {
                    InitOpenList(true);
                    json = GetTreeDataOpenList(firstLoad);
                }
                else if (openSeveralNodes)
                {
                    InitOpenList(false);
                    json = GetTreeDataOpenList(firstLoad);
                }
                else if (partialLoad)
                {
                    json = GetTreeDataPartial();
                }
                else if (StateLoad)
                {
                    InitOpenListState();
                    json = GetTreeDataOpenList(firstLoad);
                }
                else
                {
                    json = GetTreeData(id, firstLoad);
                }
            }
            else
            {
                var jsonArr = partialLoad
                    ? GetSearchTreeDataPartial(SearchText, SearchParam)
                    : GetSearchTreeData(SearchText, SearchParam);
                json = jsonArr[0];
                treeView.SearchResultCount = Convert.ToInt32(jsonArr[1]);

                if (json == null)
                {
                    if (partialLoad)
                    {
                        jsonArr = GetSearchTreeDataPartial(SearchText, SearchParam);
                    }
                    else
                    {
                        SearchParam = "";
                        IsDefaultFilter = "true";
                        GetTreeData(0, true);
                        jsonArr = new[] { GetTreeData(0, true), "0" };
                    }
                    json = jsonArr[0];
                    treeView.SearchResultCount = Convert.ToInt32(jsonArr[1]);
                }
            }

            if (!string.IsNullOrEmpty(SearchText) && treeView.SearchResultCount == 0)
            {
                InitOpenListState();
                json = GetTreeDataOpenList(firstLoad);
            }

            if (json == null)
            {
                InitOpenListState();
                json = GetTreeDataOpenList(firstLoad);
            }

            context.Response.ContentType = "text/json";
            context.Response.Write(json);
        }

        public bool IsReusable => false;

        /// <summary>
        ///     Получение данных для дерева
        /// </summary>
        /// <param name="nodeid">Идентификатор узла</param>
        /// <returns>JSON данные</returns>
        private string GetTreeData(int nodeid, bool firstLoad)
        {
            var where = firstLoad ? GetDefaultWhere() : "";

            var sqlParams = new Dictionary<string, object> {{"@Код", nodeid}, {"@Потомки", 1}};

            var sql = GetTreeData_Sql(OrderByField, OrderByDirection, SearchText, SearchParam, "", where);
            var dt = DBManager.GetData(sql, dbSource.ConnectionString, CommandType.Text, sqlParams);

            if (treeView.Settings == null) treeView.SetAdvSearchDataSource(dt);

            Node root = null;
            var status = "";

            if (nodeid == 0)
            {
                root = new Node
                {
                    id = "0",
                    text = dbSource.RootName,
                    type = "folder",
                    state = new NodeState {opened = true, selected = false, loaded = true},
                    li_attr = new NodeAttr {text = dbSource.RootName, parentId = "#", status = "0"}
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
                        text = GetPrefixIcon(rootNode) + $"<span style='margin-left:5px;margin-right:5px;color:{GetColor(status)}'>" + rootNode["text"] + "</span>" + GetPostFixIcon(rootNode),
                        state = new NodeState {opened = true, selected = false, loaded = true},
                        li_attr = new NodeAttr
                            {text = rootNode["text"].ToString(), parentId = rootNode["ParentId"].ToString(), status = "0"}
                    };
                }
            }

            var view = new DataView(dt) {RowFilter = "ParentId=" + nodeid};

            foreach (DataRowView kvp in view)
            {
                var parentId = kvp["Id"].ToString();
                try{status = kvp["Status"].ToString();} catch{ status = "0";}

                var node = new Node
                {
                    id = kvp["Id"].ToString(),
                    type = (int) kvp["ЕстьДети"] == 1 ? "file" : "folder",
                    text = GetPrefixIcon(kvp) + $"<span style='margin-left:5px;margin-right:5px;color:{GetColor(status)}'>" + kvp["text"] + "</span>" + GetPostFixIcon(kvp),
                    state = new NodeState
                    {
                        opened = !string.IsNullOrEmpty(kvp["Фильтр"].ToString()),
                        selected = false,
                        loaded = Convert.ToInt32(kvp["ЕстьДети"]) == 1
                    },
                    li_attr = new NodeAttr {text = kvp["text"].ToString(), parentId = kvp["ParentId"].ToString(), status = status }
                };

                root.children.Add(node);
                //AddChildItems(dt, node, parentId);
            }

            return new JavaScriptSerializer().Serialize(root);
        }

        /// <summary>
        ///     Получение данных для дерева (при частичной загрузке)
        /// </summary>
        /// <returns>JSON данные</returns>
        private string GetTreeDataPartial()
        {
            var sqlParams = new Dictionary<string, object> {{"@Код", string.Empty}, {"@Потомки", 2}};

            var sql = GetTreeData_Sql(OrderByField, OrderByDirection, SearchText, SearchParam);

            var dt = DBManager.GetData(sql, dbSource.ConnectionString, CommandType.Text, sqlParams);

            if (treeView.Settings == null) treeView.SetAdvSearchDataSource(dt);

            var rootsView = new DataView(dt) {RowFilter = "Id IN (" + RootIds + ")"};

            var roots = new List<Node>();
            var status = "";

            foreach (DataRowView r in rootsView)
            {
                try { status = r["Status"].ToString(); } catch { status = "0"; }
                var root = new Node
                {
                    id = r["Id"].ToString(),
                    type = Convert.ToInt32(r["ЕстьДети"]) == 1 ? "file" : "folder",
                    text = GetPrefixIcon(r) + $"<span style='margin-left:5px;margin-right:5px;color:{GetColor(status)}'>" + r["text"] + "</span>" + GetPostFixIcon(r),
                    state = new NodeState {opened = true, selected = false, loaded = true},
                    li_attr = new NodeAttr {text = r["text"].ToString(), parentId = r["ParentId"].ToString(), status = status}
                };

                roots.Add(root);

                var childsView = new DataView(dt) {RowFilter = "ParentId=" + r["Id"]};

                foreach (DataRowView drv in childsView)
                {
                    var parentId = drv["Id"].ToString();
                    try { status = drv["Status"].ToString(); } catch { status = "0"; }
                    var node = new Node
                    {
                        id = drv["Id"].ToString(),
                        type = (int) drv["ЕстьДети"] == 1 ? "file" : "folder",
                        text = GetPrefixIcon(drv) + $"<span style='margin-left:5px;margin-right:5px;color:{GetColor(status)}'>" + drv["text"] + "</span>" + GetPostFixIcon(drv),
                        state = new NodeState
                        {
                            opened = !string.IsNullOrEmpty(drv["Фильтр"].ToString()),
                            selected = false,
                            loaded = Convert.ToInt32(drv["ЕстьДети"]) == 1
                        },
                        li_attr = new NodeAttr {text = drv["text"].ToString(), parentId = drv["ParentId"].ToString(), status = status }
                    };

                    root.children.Add(node);
                    //AddChildItems(dt, node, parentId);
                }
            }

            return new JavaScriptSerializer().Serialize(roots);
        }

        public string[] GetAddQuery(TreeView tv)
        {
            var where = "";
            var sqlParams = new Dictionary<string, object>();

            if (tv.Settings.IsFilterEnable)
            {
                foreach (var col in tv.Settings.TableColumns)
                {
                    if (col.FilterUser != null)
                    {
                        where += GetSQLQuery(tv, col, where == "");
                    }
                }

                if (tv.Settings.AddTableColumns != null)
                    foreach (var col in tv.Settings.AddTableColumns)
                    {
                        if (col.FilterUser != null)
                        {
                            where += GetSQLQuery(tv, col, where == "");
                        }
                    }
            }

            if (!tv.Settings.FilterClause.IsNullEmptyOrZero())
            {
                sqlParams.Add("@КодУсловия", tv.Settings.FilterClause);
                var sqlQuery = string.Format(SQLQueries.SELECT_IDs_ДополнительныеФильтрыПриложений, "");
                var dtCond = DBManager.GetData(sqlQuery, Config.DS_user, CommandType.Text, sqlParams);

                foreach (DataRow row in dtCond.Rows)
                {
                    if (where != "") where += " AND "; else where = "WHERE ";
                    where += row["Запрос"];
                }
            }

            var addTable = "";
            var addField = "";
            if (tv.DbSourceSettings != null && tv.DbSourceSettings.AdditionalTable != null)
            {
                foreach (var table in tv.DbSourceSettings.AdditionalTable)
                {
                    switch (table.Key)
                    {
                        case "vwРозетки":
                            addTable += "LEFT JOIN vwРозетки ON vwРозетки.КодРасположения = T1.[КодРасположения]";

                            foreach (var field in table.Value)
                            {
                                addField = addField + "," + field;
                            }
                            break;
                    }
                }
            }

            return new string[] {where, addTable, addField};
        }

        /// <summary>
        ///     Получение данных для дерева при поиске
        /// </summary>
        /// <param name="searchText">Что ищем</param>
        /// <param name="searchParam">Как ищем</param>
        /// <returns>JSON данные</returns>
        protected string[] GetSearchTreeData(string searchText, string searchParam)
        {
            var addQuery = GetAddQuery(treeView);

            var parametersOut = new Dictionary<string, object> {{"@КоличествоНайденных", 0}};
            var sql = GetTreeData_Sql(OrderByField, OrderByDirection, SearchText, SearchParam, "", addQuery[0], addQuery[2], addQuery[1]);
            var recCount = 0;
            var dt = new DataTable();
            using (var dbReader = new DBReader(sql, CommandType.Text, dbSource.ConnectionString, null, parametersOut))
            {
                if (dbReader.HasRows) dt.Load(dbReader);
                dbReader.Close();
                recCount = Convert.ToInt16(parametersOut["@КоличествоНайденных"]);
            }

            if (recCount == 0)
            {
                return new[] { null, recCount.ToString() };
            }

            if (treeView.Settings == null) treeView.SetAdvSearchDataSource(dt);

            Node root = null;
            var status = "";
            root = new Node
            {
                id = "0",
                text = dbSource.RootName,
                type = "folder",
                state = new NodeState {opened = true, selected = false, loaded = true},
                li_attr = new NodeAttr {text = dbSource.RootName, parentId = "#", status = "0" }
            };

            if (dt.Columns.Contains("ParentId"))
            {
                var view = new DataView(dt) {RowFilter = "[ParentId] is null"};
                foreach (DataRowView row in view)
                {
                    var parentId = row["Id"].ToString();
                    try { status = row["Status"].ToString(); } catch { status = "0"; }
                    var node = new Node
                    {
                        id = row["Id"].ToString(),
                        type = Convert.ToInt32(row["ЕстьДети"]) == 1 ? "file" : "folder",
                        text = GetPrefixIcon(row) +
                               $"<span style='margin-left:5px;margin-right:5px;color:{GetColor(status)}'>" +
                               GetFilteredText(row["text"].ToString(), searchText, row["BitMask"].ToString()) +
                               "</span>" +
                               GetPostFixIcon(row) +
                               GetFilter(row["BitMask"].ToString()) + GetAddSearchIcon(row["BitMask"].ToString()),
                        state = new NodeState
                        {
                            opened = row["BitMask"].ToString() != "4" &&
                                     row["BitMask"].ToString() != "5" ||
                                     Convert.ToInt32(row["ЕстьДети"]) == 1,
                            selected = false,
                            loaded = row["BitMask"].ToString() != "4" &&
                                     row["BitMask"].ToString() != "5" ||
                                     Convert.ToInt32(row["ЕстьДети"]) == 1
                        },
                        li_attr = new NodeAttr {text = row["text"].ToString(), parentId = row["ParentId"].ToString(), status = status }
                    };
                    root.children.Add(node);
                    AddChildItemsSearch(dt, node, parentId, searchText);
                }
            }

            return new[] {new JavaScriptSerializer().Serialize(root), recCount.ToString()};
        }

        /// <summary>
        ///     Получение данных для дерева при поиске (при частичной загрузке)
        /// </summary>
        /// <param name="searchText">Что ищем</param>
        /// <param name="searchParam">Как ищем</param>
        /// <returns>JSON данные</returns>
        protected string[] GetSearchTreeDataPartial(string searchText, string searchParam)
        {
            var sql = GetTreeData_Sql(OrderByField, OrderByDirection, SearchText, SearchParam);
            var status = "";
            var parametersOut = new Dictionary<string, object> {{"@КоличествоНайденных", 0}};
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

            if (treeView.Settings == null) treeView.SetAdvSearchDataSource(dt);

            var roots = new List<Node>();

            if (dt.Columns.Contains("id") && dt.Columns.Contains("ParentId"))
            {
                var rootsView = new DataView(dt) {RowFilter = "id IN (" + RootIds + ")"};

                foreach (DataRowView r in rootsView)
                {
                    try { status = r["Status"].ToString(); } catch { status = "0"; }
                    var root = new Node
                    {
                        id = r["id"].ToString(),
                        type = (int) r["ЕстьДети"] == 1 ? "file" : "folder",
                        text = GetPrefixIcon(r) +
                               $"<span style='margin-left:5px;margin-right:5px;color:{GetColor(status)}'>" +
                               GetFilteredText(r["text"].ToString(), searchText, r["BitMask"].ToString()) +
                               "</span>" +
                               GetPostFixIcon(r) +
                               GetFilter(r["BitMask"].ToString()),
                        state = new NodeState
                        {
                            opened = r["BitMask"].ToString() != "4" &&
                                     r["BitMask"].ToString() != "5" ||
                                     (int) r["ЕстьДети"] == 1,
                            selected = false,
                            loaded = r["BitMask"].ToString() != "4" &&
                                     r["BitMask"].ToString() != "5" ||
                                     (int) r["ЕстьДети"] == 1
                        },
                        li_attr = new NodeAttr {text = r["text"].ToString(), parentId = r["ParentId"].ToString(), status = status }
                    };

                    roots.Add(root);

                    var childsView = new DataView(dt) {RowFilter = "ParentId=" + r["id"]};

                    foreach (DataRowView rc in childsView)
                    {
                        var parentId = rc["id"].ToString();
                        try { status = rc["Status"].ToString(); } catch { status = "0"; }
                        var node = new Node
                        {
                            id = rc["id"].ToString(),
                            type = (int) rc["ЕстьДети"] == 1 ? "file" : "folder",
                            text = GetPrefixIcon(rc) +
                                   $"<span style='margin-left:5px;margin-right:5px;color:{GetColor(status)}'>" +
                                   GetFilteredText(rc["text"].ToString(), searchText, rc["BitMask"].ToString()) +
                                   "</span>" +
                                   GetPostFixIcon(rc) +
                                   GetFilter(rc["BitMask"].ToString()),
                            state = new NodeState
                            {
                                opened = rc["BitMask"].ToString() != "4" &&
                                         rc["BitMask"].ToString() != "5" ||
                                         (int) rc["ЕстьДети"] == 1,
                                selected = false,
                                loaded = rc["BitMask"].ToString() != "4" &&
                                         rc["BitMask"].ToString() != "5" ||
                                         (int) rc["ЕстьДети"] == 1
                            },
                            li_attr = new NodeAttr
                                {text = rc["text"].ToString(), parentId = rc["ParentId"].ToString(), status = status }
                        };
                        root.children.Add(node);
                        //AddChildItems(dt, node, parentId, searchText);
                    }
                }
            }

            return new[] {new JavaScriptSerializer().Serialize(roots), recCount.ToString()};
        }

        protected void InitOpenList(bool singleNode)
        {
            var sqlParams = new Dictionary<string, object>();
            var sqlQuery = string.Empty;

            if (singleNode)
            {
                SelectedNode = LoadId;
                sqlParams.Add("@id", LoadId);
                sqlQuery = string.Format(SQLQueries.SELECT_ДеревоВсеРодителиУзла, dbSource.ViewName, dbSource.PkField);
            }
            else
            {
                sqlParams.Add("@ids", SelectedIds);
                sqlQuery = string.Format(SQLQueries.SELECT_ДеревоВсеРодителиУзлов, dbSource.ViewName, dbSource.PkField);
            }

            var dtParent = DBManager.GetData(
                sqlQuery, Config.DS_user, CommandType.Text, sqlParams);

            foreach (DataRow row in dtParent.Rows)
            {
                if (OpenList != "") OpenList += ",";
                OpenList += "'" + row[dbSource.PkField] + "'";
            }
        }

        protected void InitOpenListState()
        {
            var clid = "0";
            var paramName = treeView.ParamName;
            var parametersManager =
                new AppParamsManager(Convert.ToInt32(clid), new StringCollection {paramName});
            var appParam = parametersManager.Params.Find(x => x.Name == paramName);
            if (appParam == null) return;
            var jsonState = appParam.Value;

            if (jsonState.IndexOf("open") > 0)
            {
                var openState = jsonState.Substring(jsonState.IndexOf("open"));
                var startArray = openState.IndexOf("[");
                var endArray = openState.IndexOf("]");
                if (startArray != -1)
                    OpenList = openState.Substring(startArray + 1, endArray - startArray - 1)
                        .Replace("\"", "'");
            }

            if (jsonState.IndexOf("selected") > 0)
            {
                var selectedState = jsonState.Substring(jsonState.IndexOf("selected"));
                var startArray = selectedState.IndexOf("[");
                var endArray = selectedState.IndexOf("]");
                if (startArray != -1)
                {
                    SelectedNode = selectedState.Substring(startArray + 1, endArray - startArray - 1)
                        .Replace("\"", "");
                    var sqlParams = new Dictionary<string, object> {{"@id", SelectedNode}};

                    var sqlQuery = string.Format(SQLQueries.SELECT_ДеревоВсеРодителиУзла, dbSource.ViewName,
                        dbSource.PkField);

                    var dtParent = DBManager.GetData(
                        sqlQuery, dbSource.ConnectionString, CommandType.Text, sqlParams);

                    foreach (DataRow row in dtParent.Rows)
                    {
                        if (OpenList != "") OpenList += ",";
                        OpenList += "'" + row[dbSource.PkField] + "'";
                    }
                }
            }
        }

        /// <summary>
        ///     Получение данных из State, учитывая открытые узлы
        /// </summary>
        /// <returns>JSON данные</returns>
        protected string GetTreeDataOpenList(bool firstLoad)
        {
            if (OpenList.IsNullEmptyOrZero()) OpenList = "'0'";
            var where = firstLoad ? GetDefaultWhere() : "";
            var sql = GetTreeData_Sql(OrderByField, OrderByDirection, "", "", OpenList, where);
            var dt = DBManager.GetData(sql, dbSource.ConnectionString);

            if (treeView.Settings == null) treeView.SetAdvSearchDataSource(dt);

            Node root = null;
            var status = "";
            root = new Node
            {
                id = "0",
                text = dbSource.RootName,
                type = "folder",
                state = new NodeState {opened = true, selected = false, loaded = true},
                li_attr = new NodeAttr {text = dbSource.RootName, parentId = "0", status = "0" }
            };

            var view = new DataView(dt) {RowFilter = "[ParentId] is null"};
            foreach (DataRowView row in view)
            {
                var parentId = row["Id"].ToString();
                try { status = row["Status"].ToString(); } catch { status = "0"; }
                var s = row["BitMask"].ToString();
                var hasChild = Convert.ToInt32(row["ЕстьДети"]) > 1;


                var node = new Node
                {
                    id = row["Id"].ToString(),
                    type = !hasChild ? "file" : "folder",
                    text = GetPrefixIcon(row) + $"<span style='margin-left:5px;margin-right:5px;color:{GetColor(status)}'>" + row["text"] + "</span>" + GetPostFixIcon(row),
                    state = new NodeState
                    {
                        opened = row["BitMask"].ToString() == "1" || !hasChild,
                        selected = ReturnId != "2" && row["Id"].ToString() == SelectedNode,
                        loaded = row["BitMask"].ToString() == "1" || !hasChild
                    },
                    li_attr = new NodeAttr {text = row["text"].ToString(), parentId = "0", status = status }
                };
                root.children.Add(node);
                AddChildItems(dt, node, parentId);
            }

            return new JavaScriptSerializer().Serialize(root);
        }

        private void AddChildItems(DataTable dt, Node parentNode, string parentId)
        {
            var status = "";
            var viewItem = new DataView(dt) {RowFilter = "[ParentId]=" + parentId};
            foreach (DataRowView childView in viewItem)
            {
                try { status = childView["Status"].ToString(); } catch { status = "0"; }
                var hasChild = Convert.ToInt32(childView["ЕстьДети"]) > 1;
                var node = new Node
                {
                    id = childView["Id"].ToString(),
                    text = GetPrefixIcon(childView) + $"<span style='margin-left:5px;margin-right:5px;color:{GetColor(status)}'>" + childView["text"] + "</span>" + GetPostFixIcon(childView),
                    type = !hasChild ? "file" : "folder",
                    state = new NodeState
                    {
                        opened = childView["BitMask"].ToString() == "1" || !hasChild,
                        selected = ReturnId != "2" && childView["Id"].ToString() == SelectedNode,
                        loaded = childView["BitMask"].ToString() == "1" || !hasChild
                    },
                    li_attr = new NodeAttr
                        {text = childView["text"].ToString(), parentId = childView["ParentId"].ToString(), status = status }
                };
                parentNode.children.Add(node);
                var pId = childView["Id"].ToString();
                AddChildItems(dt, node, pId);
            }
        }

        /// <summary>
        ///     Получение данных для дерева - дети узла
        /// </summary>
        /// <param name="dt">Источник данных</param>
        /// <param name="parentNode">Корневой узел</param>
        /// <param name="parentId">Идентификатор корневого узла</param>
        /// <param name="searchText">Строка поиска</param>
        private void AddChildItemsSearch(DataTable dt, Node parentNode, string parentId, string searchText = "")
        {
            var status = "";
            var viewItem = new DataView(dt) {RowFilter = "[ParentId]=" + parentId};
            foreach (DataRowView childView in viewItem)
            {
                try { status = childView["Status"].ToString(); } catch { status = "0"; }
                var hasChild = Convert.ToInt32(childView["ЕстьДети"]) > 1;
                var openedLoaded = !(childView["BitMask"].ToString() == "1" && hasChild);
                var node = new Node
                {
                    id = childView["Id"].ToString(),
                    text = string.IsNullOrEmpty(searchText) && string.IsNullOrEmpty(SearchParam)
                        ? GetPrefixIcon(childView) + $"<span style='margin-left:5px;margin-right:5px;color:{GetColor(status)}'>" + childView["text"] + "</span>" + GetPostFixIcon(childView)
                        : GetPrefixIcon(childView) + $"<span style='margin-left:5px;margin-right:5px;color:{GetColor(status)}'>" + GetFilteredText(childView["text"].ToString(), searchText,
                              childView["BitMask"].ToString()) + "</span>" +
                          GetPostFixIcon(childView) + GetFilter(childView["BitMask"].ToString()) + GetAddSearchIcon(childView["BitMask"].ToString()),
                    type = Convert.ToInt32(childView["ЕстьДети"]) == 1 ? "file" : "folder",
                    state = new NodeState
                    {
                        opened = openedLoaded,
                        selected = false,
                        loaded = openedLoaded
                    },
                    li_attr = new NodeAttr
                        {text = childView["text"].ToString(), parentId = childView["ParentId"].ToString(), status = status }
                };
                parentNode.children.Add(node);
                var pId = childView["Id"].ToString();
                AddChildItemsSearch(dt, node, pId, searchText);
            }
        }

        private string GetAddSearchIcon(string mask)
        {
            if (SearchParam == "filter")
            {
                if (mask == "1" || mask == "3" || mask == "5" || mask == "7")
                    return "<font class='v4TreeviewFiltered' title='"+ Resx.GetString("Inv_lblSatisfiesFiltrationConditions") + "'> (!)</font>";
            }

            return "";
        }

        /// <summary>
        ///     Получение строки запроса по переданным параметрам
        /// </summary>
        /// <param name="orderByField">Поле сортировки</param>
        /// <param name="orderByDirection">Направление сортировки</param>
        /// <param name="searchText">Строка поиска</param>
        /// <param name="searchParam">Параметры поиска</param>
        /// <param name="openList">Список открытых нодов</param>
        /// <returns></returns>
        protected virtual string GetTreeData_Sql(string orderByField = "L", string orderByDirection = "ASC",
            string searchText = "", string searchParam = "", string openList = "", string where = "", string addField = "", string addTable = "")
        {
            if (orderByField != "L") orderByField = dbSource.NameField;

            var orderBy = orderByField + " " + orderByDirection;

            if (string.IsNullOrEmpty(searchText) && string.IsNullOrEmpty(searchParam))
            {
                if (openList.IsNullEmptyOrZero())
                    return string.Format(SQLQueries.SELECT_ДеревоВсеПотомкиУзла, dbSource.ViewName, dbSource.PkField,
                        dbSource.NameField, orderBy, string.IsNullOrEmpty(RootIds) ? "-1" : RootIds);

                return string.Format(SQLQueries.SELECT_ДеревоОткрытыеУзлы,
                    dbSource.PkField,
                    dbSource.NameField,
                    dbSource.ViewName,
                    string.IsNullOrEmpty(dbSource.ModifyUserField) ? string.Empty : "[Изменил] [int],",
                    string.IsNullOrEmpty(dbSource.ModifyDateField) ? string.Empty : "[Изменено] [datetime],",
                    string.IsNullOrEmpty(dbSource.ModifyUserField)
                        ? string.Empty
                        : "Parent." + dbSource.ModifyUserField + " [Изменил],",
                    string.IsNullOrEmpty(dbSource.ModifyDateField)
                        ? string.Empty
                        : "Parent." + dbSource.ModifyDateField + " [Изменено],",
                    string.IsNullOrEmpty(dbSource.ModifyUserField)
                        ? string.Empty
                        : "Child." + dbSource.ModifyUserField + " [Изменил],",
                    string.IsNullOrEmpty(dbSource.ModifyDateField)
                        ? string.Empty
                        : "Child." + dbSource.ModifyDateField + " [Изменено],",
                    string.IsNullOrEmpty(dbSource.ModifyUserField) ? string.Empty : "#TreeFilter.[Изменил],",
                    string.IsNullOrEmpty(dbSource.ModifyDateField) ? string.Empty : "#TreeFilter.[Изменено],",
                    orderBy,
                    openList
                );
            }

            searchText = searchParam == "1" ? searchText + "%" : "%" + searchText + "%";

            return string.Format(SQLQueries.SELECT_ДеревоНайденныеУзлы,
                dbSource.PkField,
                dbSource.NameField,
                dbSource.ViewName,
                orderBy,
                searchText,
                string.IsNullOrEmpty(dbSource.ModifyUserField) ? string.Empty : "[Изменил] [int],",
                string.IsNullOrEmpty(dbSource.ModifyDateField) ? string.Empty : "[Изменено] [datetime],",
                string.IsNullOrEmpty(dbSource.ModifyUserField)
                    ? string.Empty
                    : dbSource.ModifyUserField + " [Изменил],",
                string.IsNullOrEmpty(dbSource.ModifyDateField)
                    ? string.Empty
                    : dbSource.ModifyDateField + " [Изменено],",
                string.IsNullOrEmpty(dbSource.ModifyUserField) ? string.Empty : "[Изменил],",
                string.IsNullOrEmpty(dbSource.ModifyDateField) ? string.Empty : "[Изменено],",
                ShowTopNodesInSearchResult ? 1 : 0,
                string.IsNullOrEmpty(RootIds) ? "-1" : RootIds
            );
        }

        /// <summary>
        ///     Возвращает "отфильтровано" по переданной маске
        /// </summary>
        /// <param name="bitMask">маска</param>
        /// <returns></returns>
        private string GetFilter(string bitMask)
        {
            if (bitMask == "2" || bitMask == "3" || bitMask == "6" || bitMask == "7")
                return "<font class='v4TreeviewFiltered' title='"+ Resx.GetString("Inv_lblNotAllSubordinateItemsDisplayed") + "'> (" + Resx.GetString("lblFiltered") + ")</font>";
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
            if (string.IsNullOrEmpty(filter)) return text;
            // не выделяем текст, если не найдено (для случая "начинается с")
            if (bitMask == "2" || bitMask == "4" || bitMask == "6") return text;

            var searchText = SearchText.Split(' ');

            if (searchText.Length == 1)
            {
                if (!text.ToLower().Contains(SearchText.ToLower())) return text;
            }
            else
            {
                var contains = false;
                foreach (var searchItemText in searchText)
                {
                    if (text.ToLower().Contains(searchItemText.ToLower()))
                    {
                        contains = true;
                        break;
                    }
                }

                if (!contains) return text;

            }

            foreach (var searchItemText in searchText)
            {
                var curPos = text.ToLower().IndexOf(searchItemText.ToLower());
                while (curPos != -1)
                {
                    text = text.Insert(curPos, "<span class='found' tabindex='-1' style='background-color:#FFFF00'>");
                    curPos = curPos + 68 + searchItemText.Length - 1;
                    text = text.Insert(curPos, "</span>");
                    curPos = text.ToLower().IndexOf(searchItemText.ToLower(), curPos);
                }
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

        //TODO: Удалить 
        /// <summary>
        ///     Определение цвета текста
        /// </summary>
        /// <param name="status">Костыль</param>
        /// <returns>возвращаямая строка, содержащая цвет</returns>
        protected virtual string GetColor(string status)
        {
            return "black";
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
            public string status { get; set; }
            public NodeState state { get; set; }
            public List<Node> children { get; }
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
            public string status { get; set; }
        }

        private string GetDefaultWhere()
        {
            var where = "";
            if (treeView.ColumnsDefaultValues == null) return where;

            foreach (var col in treeView.ColumnsDefaultValues)
            {
                where += " AND T0." + col.Key + "=" + col.Value;
            }
            return where;
        }

        public string GetSQLQuery(TreeView tv, TreeViewColumn col, bool addWhere)
        {
            var where = "";
            if (addWhere) where = "WHERE "; else where += " AND ";
            switch (col.FilterUser.FilterType)
            {
                case TreeViewColumnUserFilterEnum.Указано:
                    if (col.ColumnType == TreeViewColumnTypeEnum.String)
                        where += "ISNULL(T0." + col.FieldName + ",'') <> '' ";
                    else
                        where += "T0." + col.FieldName + " IS NOT NULL ";
                    break;
                case TreeViewColumnUserFilterEnum.НеУказано:
                    if (col.ColumnType == TreeViewColumnTypeEnum.String)
                        where += "ISNULL(T0." + col.FieldName + ",'') = '' ";
                    else
                        where += "T0." + col.FieldName + " IS NULL ";
                    break;
                case TreeViewColumnUserFilterEnum.Равно:
                    if (col.IsFilteredListColumn)
                    {
                        where += "T0." + col.FieldName + " IN(" + col.FilterUser.FilterValue1 + ") ";
                    }
                    else
                    {
                        if (col.ColumnType == TreeViewColumnTypeEnum.String)
                            where += "T0." + col.FieldName + " = '" + col.FilterUser.FilterValue1 + "'";
                        else if (col.ColumnType == TreeViewColumnTypeEnum.Date)
                            where += "T0." + col.FieldName + " = '" + DateTime.Parse(col.FilterUser.FilterValue1.ToString()).ToString("yyyyMMdd") + "'";
                        else
                            where += "T0." + col.FieldName + " = " + col.FilterUser.FilterValue1;
                    }
                    break;
                case TreeViewColumnUserFilterEnum.НеРавно:
                    if (col.ColumnType == TreeViewColumnTypeEnum.String || col.ColumnType == TreeViewColumnTypeEnum.Date)
                        where += "T0." + col.FieldName + " <> '" + col.FilterUser.FilterValue1 + "'";
                    else
                        where += "T0." + col.FieldName + " <> " + col.FilterUser.FilterValue1;
                    break;
                case TreeViewColumnUserFilterEnum.Между:
                    if (col.ColumnType == TreeViewColumnTypeEnum.Date)
                        where = where + "T0." + col.FieldName + " BETWEEN '" + DateTime.Parse(col.FilterUser.FilterValue1.ToString()).ToString("yyyyMMdd") + "' AND '" + DateTime.Parse(col.FilterUser.FilterValue2.ToString()).ToString("yyyyMMdd") + "'";
                    else
                        where = where + "T0." + col.FieldName + " >= " + col.FilterUser.FilterValue1 + " AND T0." + col.FieldName + " <= " + col.FilterUser.FilterValue2;
                    break;
                case TreeViewColumnUserFilterEnum.НачинаетсяС:
                    if (col.ColumnType == TreeViewColumnTypeEnum.String)
                        where += "T0." + col.FieldName + " LIKE '" + col.FilterUser.FilterValue1 + "%'";
                    break;
                case TreeViewColumnUserFilterEnum.ЗаканчиваетсяНа:
                    if (col.ColumnType == TreeViewColumnTypeEnum.String)
                        where += "T0." + col.FieldName + " LIKE '%" + col.FilterUser.FilterValue1 + "'";
                    break;
                case TreeViewColumnUserFilterEnum.Содержит:
                    if (col.ColumnType == TreeViewColumnTypeEnum.String)
                    {
                        if (tv.ColumnNotSplitWhenFilter != null && !tv.ColumnNotSplitWhenFilter.Contains(col.FieldName) && col.FilterUser.FilterValue1.ToString().Split(' ').Length > 1)
                        {
                            where += "(";
                            var wordsCount = 0;
                            foreach (var word in col.FilterUser.FilterValue1.ToString().Split(' '))
                            {
                                if (wordsCount > 0) where += " AND ";
                                where += "T0." + col.FieldName + " LIKE '%" + word + "%'";
                                wordsCount++;
                            }
                            where += ")";
                        }
                        else
                        {
                            where += "(T0." + col.FieldName + " LIKE '%" + col.FilterUser.FilterValue1 + "%'";
                            /*
                            if (col.FieldName == "text")
                            {
                                where += AddPathSubQuery(treeView.DbSourceSettings.PathField,
                                    col.FilterUser.FilterValue1.ToString(), col.FilterUser.FilterValue2);
                            }
                            */
                            where += ")";
                        }
                    }
                    break;
                case TreeViewColumnUserFilterEnum.НеСодержит:
                    if (col.ColumnType == TreeViewColumnTypeEnum.String)
                        where += "T0." + col.FieldName + " NOT LIKE '%" + col.FilterUser.FilterValue1 + "%'";
                    break;
                case TreeViewColumnUserFilterEnum.Больше:
                    if (col.ColumnType == TreeViewColumnTypeEnum.Date)
                    {
                        where += "T0." + col.FieldName + " > '" + DateTime.Parse(col.FilterUser.FilterValue1.ToString()).ToString("yyyyMMdd") + "'";
                    }
                    else
                        where += "T0." + col.FieldName + " > " + col.FilterUser.FilterValue1;

                    break;
                case TreeViewColumnUserFilterEnum.БольшеИлиРавно:
                    if (col.ColumnType == TreeViewColumnTypeEnum.Date)
                        where += "T0." + col.FieldName + " >= '" + DateTime.Parse(col.FilterUser.FilterValue1.ToString()).ToString("yyyyMMdd") + "'";
                    else
                        where += "T0." + col.FieldName + " >= " + col.FilterUser.FilterValue1;
                    break;
                case TreeViewColumnUserFilterEnum.Меньше:
                    if (col.ColumnType == TreeViewColumnTypeEnum.Date)
                        where += "T0." + col.FieldName + " < '" + DateTime.Parse(col.FilterUser.FilterValue1.ToString()).ToString("yyyyMMdd") + "'";
                    else
                        where += "T0." + col.FieldName + " < " + col.FilterUser.FilterValue1;
                    break;
                case TreeViewColumnUserFilterEnum.МеньшеИлиРавно:
                    if (col.ColumnType == TreeViewColumnTypeEnum.Date)
                        where += "T0." + col.FieldName + " <= '" + DateTime.Parse(col.FilterUser.FilterValue1.ToString()).ToString("yyyyMMdd") + "'";
                    else
                        where += "T0." + col.FieldName + " <= " + col.FilterUser.FilterValue1;
                    break;
                case TreeViewColumnUserFilterEnum.Да:
                    where += "T0." + col.FieldName + " = 1" ;
                    break;
                case TreeViewColumnUserFilterEnum.Нет:
                    where += "T0." + col.FieldName + " = 0";
                    break;

            }

            return where;
        }

        private string AddPathSubQuery(string pathField, string fieldValue, object fieldValue2)
        {
            var where = "";
            if (!pathField.IsNullEmptyOrZero())
            {
                var searchParam = "0";
                if (fieldValue2 != null)
                    searchParam = fieldValue2.ToString();

                if (fieldValue.Split(' ').Length > 1 && searchParam != "1")
                {
                    where = " OR (";
                    foreach (var word in fieldValue.Split(' '))
                    {
                        where = where + " " + pathField + " LIKE '" +
                                           (searchParam == "1" ? word + "%'" : "%" + word + "%' AND ");
                    }

                    where = where.Remove(where.Length - 4);
                    where += ")";
                }
            }

            return where;
        }


        protected string[] GetOffConditionSqlString()
        {
            var sqlStr1 = "";
            var sqlStr2 = "";

            if (treeView.OffTreeNodeFieldMap?.Condition != null)
            {
                if (treeView.OffTreeNodeFieldMap.ConditionType == TreeViewOffConditionTypeEnum.Int)
                {
                    if (treeView.OffTreeNodeFieldMap.Condition == false)
                    {
                        sqlStr1 = $" AND T1.{treeView.OffTreeNodeFieldMap.FieldName1} = 0 AND NOT EXISTS(" +
                                 $"SELECT * FROM {treeView.DbSourceSettings.ViewName} Parent " +
                                 $"WHERE T1.L > Parent.L AND T1.R < Parent.R AND Parent.{treeView.OffTreeNodeFieldMap.FieldName1} = 1) ";

                        sqlStr2 = $" AND {treeView.OffTreeNodeFieldMap.FieldName1} = 0 ";
                    }
                }
                else if (treeView.OffTreeNodeFieldMap.ConditionType == TreeViewOffConditionTypeEnum.ToDate)
                {
                    sqlStr1 = $" AND T1.{treeView.OffTreeNodeFieldMap.FieldName1} > getdate() AND NOT EXISTS(" +
                              $"SELECT * FROM {treeView.DbSourceSettings.ViewName} Parent " +
                              $"WHERE T1.L > Parent.L AND T1.R < Parent.R AND Parent.{treeView.OffTreeNodeFieldMap.FieldName1} <= getdate()) ";

                    sqlStr2 = $" AND {treeView.OffTreeNodeFieldMap.FieldName1} > getdate() ";
                }
                else if (treeView.OffTreeNodeFieldMap.ConditionType == TreeViewOffConditionTypeEnum.FromToDate)
                {
                    sqlStr1 = $" AND T1.{treeView.OffTreeNodeFieldMap.FieldName1} <= getdate() AND T1.{treeView.OffTreeNodeFieldMap.FieldName2} > getdate() AND NOT EXISTS(" +
                              $"SELECT * FROM {treeView.DbSourceSettings.ViewName} Parent " +
                              $"WHERE T1.L > Parent.L AND T1.R < Parent.R AND (Parent.{treeView.OffTreeNodeFieldMap.FieldName1} > getdate() OR Parent.{treeView.OffTreeNodeFieldMap.FieldName2} < getdate())) ";

                    sqlStr2 = $" AND {treeView.OffTreeNodeFieldMap.FieldName1} <= getdate() AND T1.{treeView.OffTreeNodeFieldMap.FieldName2} > getdate() ";
                }
            }

            return new[] { sqlStr1, sqlStr2 };
        }

    }
}