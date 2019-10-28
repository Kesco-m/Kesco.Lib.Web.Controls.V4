using System.Configuration;
using System.Xml;
using Kesco.Lib.Web.Controls.V4.Common;

namespace Kesco.Lib.Web.Controls.V4.Handlers
{
    public class LikeConfigurationSectionHandler : IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
            var list = section.SelectNodes("like");
            if (list == null || list.Count == 0) return null;

            var li = new LikeItem[list.Count];

            for (var i = 0; i < li.Length; i++)
                li[i] = new LikeItem((XmlElement) list[i]);

            return li;
        }
    }
}