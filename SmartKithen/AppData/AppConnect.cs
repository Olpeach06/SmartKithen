using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartKithen.AppData
{
    internal class AppConnect
    {
        public static SmartKitchenEntities model01;
        static AppConnect() 
        {
            model01 = new SmartKitchenEntities();
        }
    }
}
