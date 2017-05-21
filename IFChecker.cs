using STU.Checker;
using System;
using System.IO;
using System.Collections.Generic;
using Npoi.Core.Util;
using Npoi.Core.SS.UserModel;
using Npoi.Core.HSSF.UserModel;
using Npoi.Core.XSSF.UserModel;
using Microsoft.Extensions.Logging;

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

        public void DumpXml()
        {

        }
    }

    /// <summary>
    /// sv Interface Checker
    /// </summary>
    class IFChecker: IChecker
    {
        private IWorkbook wb;
        private ILogger log;
        private IList<IF> ifList;

        public IFChecker(IWorkbook wb, ILogger log)
        {
            this.wb = wb;
            this.log = log;
            ifList = new List<IF>();
        }
        public int Check()
        {
           IChecker checker;

           foreach(var sheet in wb)
           {
                ISheet tmpSheet = sheet as ISheet;
                if(tmpSheet == null)
                {
                    log.LogWarning("No Sheet in workbook");
                    continue;
                }
                checker = new SignalsChecker(tmpSheet, log);
                if(checker.Check() == 1)
                {
                    log.LogError(String.Format("The {0:s} Worksheet in Workbook can pass check", tmpSheet.SheetName))
                    return 1;
                }
                else
                {
                    ifList.Add(new IF((checker as SignalsChecker).SigList));
                }
           }
           return 0; 
        }
        
        private void DumpJson()
        {
            foreach(var If in ifList)
            {
                If.DumpJson();
            }
        }

        private void DumpXml()
        {
            foreach(var If in ifList)
            {
                If.DumpXml();
            }
        }

        public void Dump(Dump_e t)
        {
            if(t == Dump_e.JSON)
            {
                DumpJson();
            }
            else if(t == Dump_e.XML)
            {
                DumpXml();
            }
            else
            {
                log.LogError("ERROR: there is not support Dump TXT");
            }
        }
    }
}