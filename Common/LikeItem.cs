using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Kesco.Lib.Web.Controls.V4.Common
{
    public class LikeItem
    {
        protected XmlElement xel;
        public string LikeId => xel.GetAttribute("likeId");
        public string FormName => xel.GetAttribute("formName");

        public LikeItem(XmlElement xel)
        {
            this.xel = xel;
        }
        
    }
}
