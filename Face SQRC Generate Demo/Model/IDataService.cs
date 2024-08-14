using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FR_Core_Tech_Demo.Model
{
    public interface IDataService
    {
        void GetData(Action<DataItem, Exception> callback);
    }
}
