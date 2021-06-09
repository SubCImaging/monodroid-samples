using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubCTools.Interfaces
{
    public interface ISettingsService
    {
        void Update(string entry, string nodeValue = "", Dictionary<string, string> attributes = null);
        XInfo Load(string entry);
        T Load<T>(string entry);
    }
}
