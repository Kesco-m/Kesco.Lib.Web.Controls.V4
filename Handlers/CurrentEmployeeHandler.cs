using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using Kesco.Lib.Entities.Corporate;
using Kesco.Lib.Log;

namespace Kesco.Lib.Web.Controls.V4.Handlers
{
    public class CurrentEmployeeHandler : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            var emplId = context.Request.Params["employeeId"];
            
            if (string.IsNullOrEmpty(emplId))
            {
                context.Response.Write($"Ошибка: При подписании не передан код сотрудника!");
                context.Response.StatusCode = 400;
                ResponseEnd(context);
                throw new ArgumentException("При подписании не передан код сотрудника");
                
            }

            try
           {
               var empl = new Employee(emplId);
               var curEmpls = empl.CurrentEmployees();
               var simpleEmpls = new List<Item>();
               curEmpls.ForEach(action: delegate(int eId)
               {
                   var e = new Employee(eId.ToString());
                   simpleEmpls.Add(new Item {Code = e.Id, Name = e.NameShort});
               });

               var jsonSerialiser = new JavaScriptSerializer();
               var json = jsonSerialiser.Serialize(simpleEmpls);

               context.Response.ContentType = "application/json";
               context.Response.Write(json);
            }
           catch (Exception ex)
           {
               context.Response.Write($"Ошибка: {ex.Message}");
               context.Response.StatusCode = 400;
               throw;
           }
           finally
            {
                ResponseEnd(context);
            }
        }

        private void ResponseEnd(HttpContext context)
        {
            context.Response.Flush();
            context.Response.SuppressContent = true;
            context.ApplicationInstance.CompleteRequest();
        }
        public bool IsReusable => false;
    }
}
