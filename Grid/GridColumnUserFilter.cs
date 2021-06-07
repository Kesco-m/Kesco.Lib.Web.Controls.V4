using Newtonsoft.Json;
using System;

namespace Kesco.Lib.Web.Controls.V4.Grid
{
    [Serializable]
    public class GridColumnUserFilter
    {
        public GridColumnUserFilterEnum FilterType { get; set; }
        public virtual object FilterValue1 { get; set; }
        public virtual object FilterValue2 { get; set; }

        [JsonIgnore]
        public virtual object ComputeFilterValue1 => FilterValue1;

        [JsonIgnore]
        public virtual object ComputeFilterValue2 => FilterValue2;
    }
}