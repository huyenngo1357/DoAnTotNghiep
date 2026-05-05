using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HappyHouse.Models
{
    public class DataProvider
    {
        static HappyHouseEntities _Entities = new HappyHouseEntities();
        public static HappyHouseEntities Entities
        {
            get { return _Entities; }
        }
    }
}