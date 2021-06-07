using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Kesco.Lib.Web.Controls.V4.Common
{
    public class ParamItem
    {
        protected XmlElement xel;
        public string Name => xel.GetAttribute("name");
        public string ActualTime => xel.GetAttribute("actualTime");

        public ParamItem(XmlElement xel)
        {
            this.xel = xel;
        }
        
    }
}
