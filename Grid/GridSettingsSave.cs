using System;
using System.Collections.Generic;

namespace Kesco.Lib.Web.Controls.V4.Grid
{
    [Serializable]
    public class GridSettingsSort
    {
        public string FieldName;
        public int? OrderByNumber { get; set; }
        public GridColumnOrderByDirectionEnum OrderByDirection { get; set; }
    }

    [Serializable]
    public class GridSettingsUserFilter
    {
        public string FieldName;
        public GridColumnUserFilter FilterUser;
        public Dictionary<object, object> FilterUniqueValues;
        public GridColumnFilterEqualEnum? FilterEqual { get; set; }
    }

    [Serializable]
    public class GridSettingsUserFilterDate : GridSettingsUserFilter
    {
        public new GridColumnUserFilterDate FilterUser;
    }
}
