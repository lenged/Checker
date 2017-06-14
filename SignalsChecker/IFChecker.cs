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
    public class IF
    {
        IList<Signal> _sigList;
        String _name;
        public IF(IList<Signal> sigList, String name) 
        {
            this._sigList = sigList;
            this._name = name;
        }

        public override bool Equals(object obj)
        {
            IF rsh;
            bool flag = true;
            rsh = obj as IF;
            if(rsh == null)
            {
                return false;
            }
            if(this._sigList.Count != rsh._sigList.Count)
            {
                flag = false;
            }
            for(int i = 0; i < _sigList.Count; i++)
            {
                if(_sigList[i].Equals(rsh._sigList[i]) == false)
                {
                    flag = false;
                    break;
                }
            }
            return (_name == rsh._name) && flag;
        }

        public override String ToString()
        {
           String str = ""; 
           str += String.Format("[IF name {0}]\n", _name);
           foreach(var sig in _sigList)
           {
               str += sig.ToString();
           }
           return str;

        }

        /// <summary>
        /// dumpt to json file 
        /// </summary>
        public void DumpJson(JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Name");
            writer.WriteValue(_name);
            writer.WritePropertyName("SigList");
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
    public class IFChecker: IChecker
    {
        private IWorkbook wb;
        private ILogger log;
        private JsonWriter writer;
        private IList<IF> ifList;

        public IList<IF> IfList
        {
            get
            {
                return this.ifList;
            }
        }

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
                if(tmpSheet.GetRow(0) == null)
                {
                    log.LogWarning("Ignore Empty Sheet");
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