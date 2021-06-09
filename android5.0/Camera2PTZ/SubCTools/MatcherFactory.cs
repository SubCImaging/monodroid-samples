using SubCTools.DVROverlay.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubCTools.DVROverlay
{
    public class MatcherFactory
    {
        static readonly Lazy<MatcherFactory> instance = new Lazy<MatcherFactory>(() => new MatcherFactory());

        List<IMatcher> matchers = new List<IMatcher>();

        private MatcherFactory() { }

        public static MatcherFactory Instance
        {
            get
            {
                return instance.Value;
            }
        }

        //public IMatcher Get(string ID)
        //{
        //    var matcher = matchers.FirstOrDefault(m => m.ID)
        //}
    }
}
