using STU.Checker;
using System;
using System.IO;
using System.Collections.Generic;

namespace STU.SignalsChecker
{
    /// <summary>
    /// systemverilog interface 
    /// </summary>
    class IF
    {
        IList<Signal> sigList;
        public IF(IList<Signal> sigList) 
        {
            this.sigList = sigList;
        }

        /// <summary>
        /// dumpt to json file 
        /// </summary>
        public void DumpJson()
        {
            foreach(var sig in sigList)
            {
                sig.DumpJson();
            }
        }
    }
}