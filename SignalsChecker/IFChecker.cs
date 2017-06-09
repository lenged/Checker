using STU.Checker;
using System;
using System.IO;
using System.Collections.Generic;
using Npoi.Core.Util;
using Npoi.Core.SS.UserModel;
using Npoi.Core.HSSF.UserModel;
using Npoi.Core.XSSF.UserModel;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace STU.SignalsChecker
{
    /// <summary>
    /// systemverilog interface 
    /// </summary>
    class IF
    {
        IList<Signal> _sigList;
        String _name;
        public IF(IList<Signal> sigList, String name) 
        {
            this._sigList = sigList;
            this._name = name;
        }

        /// <summary>
        /// dumpt to json file 
        /// </summary>
        public void DumpJson(JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteStartArray();
            foreach(var sig in _sigList)
            {
                sig.DumpJson(writer);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
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
        private JsonWriter writer;
        private IList<IF> ifList;

        public IFChecker(IWorkbook wb, ILogger log, JsonWriter writer)
        {
            this.wb = wb;
            this.log = log;
            this.writer = writer;
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
                checker = new SignalsChecker(tmpSheet, log, writer);
                if(checker.Check() == 1)
                {
                    log.LogError(String.Format("The {0:s} Worksheet in Workbook can pass check", tmpSheet.SheetName));
                    return 1;
                }
                else
                {
                    ifList.Add(new IF((checker as SignalsChecker).SigList, tmpSheet.SheetName));
                }
           }
           return 0; 
        }
        
        private void DumpJson()
        {
            writer.WriteStartArray();
            foreach(var If in ifList)
            {
                If.DumpJson(writer);
            }
            writer.WriteEndArray();
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