﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kesco.Lib.Web.Controls.V4.Grid
{
    public class GridColumnUserFilter
    {
        public GridColumnUserFilterEnum FilterType { get; set; }
        public object FilterValue1 { get; set; }
        public object FilterValue2 { get; set; }
    }
}