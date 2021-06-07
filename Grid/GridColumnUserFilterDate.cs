using Newtonsoft.Json;
using System;

namespace Kesco.Lib.Web.Controls.V4.Grid
{
    [Serializable]
    public class GridColumnUserFilterDate : GridColumnUserFilter
    {
        public virtual bool IsCurrentDate { get; set; }

        [JsonIgnore]
        public override object ComputeFilterValue1
        {
            get
            {
                if (IsCurrentDate)
                {
                    return FilterValue1 == null ? DateTime.Today : DateTime.Today.AddDays(Convert.ToInt32(FilterValue1));
                }

                return FilterValue1;
            }
        }

        [JsonIgnore]
        public override object ComputeFilterValue2
        {
            get
            {
                if (IsCurrentDate)
                {
                    return FilterValue2 == null ? DateTime.Today : DateTime.Today.AddDays(Convert.ToInt32(FilterValue2));
                }

                return FilterValue2;
            }
        }
    }
}