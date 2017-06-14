using STU.Checker;
using System;
using Npoi.Core.SS;
using Npoi.Core.SS.UserModel;
using Npoi.Core.Util;
using Npoi.Core.POIFS;
using Npoi.Core.XSSF.UserModel;
using Npoi.Core.HSSF.UserModel;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace STU.SignalsChecker
{
    struct SignalWidth
    {
       public int start;
       public int end; 
    }
     class Signal
    {
        enum Con_e {DEFAULT, CH_NAME, ONE, ZERO, EXPRESSION};
        enum IO_e {INPUT, OUTPUT}
        string _name;
        string _instanceDef;
        IO_e _io;
        SignalWidth _width;
        Con_e _connect;
        String _misc; //for CH_NAME and EXPRESSION Con_e

        public String Name
        {
            get
            {
                return _name;
            }
        }

        public String Width
        {
            get
            {
                return String.Format("{0:d}", (_width.end+1));
            }
        }
        public String InstanceDef
        {
            get
            {
                return _instanceDef;
            }
        }

        public String IO
        {
            get
            {
                return _io.ToString();
            }
        }

        public String Connection
        {
            get
            {
                return GenConnection();
            }
        }

        public Signal() {}

        /// <summary>
        /// Construct 
        /// </summary>
        /// <param name="name">Signal name</param>
        /// <param name="instanceDef">instance name</param>
        /// <param name="io">signal io property</param>
        /// <param name="width_start">start bit number</param>
        /// <param name="width_end">end bit number</param>
        /// <param name="Con">
        /// connection description: Type%misc
        ///Type is Con_e enume name, "" and "NA" means Con_e.DEFAULT
        ///misc: only "CH_NAME" and "EXPRESSION" has  property
        ///examples "NA", "CH_NAME"%"AA", "EXPRESSION"%"AA||BB"
        /// </param>
        public Signal(String name, String instanceDef = "INS", String io = "I", int width_start=0, int width_end=39, String Con="", String misc = "")
        {
            this._name = name;
            this._instanceDef = instanceDef;
            if(io == "I")
            {
                this._io = IO_e.INPUT;
            }
            else
            {
                this._io = IO_e.OUTPUT;
            }
            this._width.start = width_start;
            this._width.end = width_end;
            switch(Con)
            {
                case "NA":
                case "":
                    this._connect = Con_e.DEFAULT;
                    break;
                case "CH_NAME":
                    this._connect = Con_e.CH_NAME;
                    break;
                case "ZERO":
                    this._connect = Con_e.ZERO;
                    break;
                case "ONE":
                    this._connect = Con_e.ONE;
                    break;
                case "EXPRESSION":
                    this._connect = Con_e.EXPRESSION;
                    break;
                default:
                    this._connect = Con_e.DEFAULT;
                    break;
            }
            if(this._connect == Con_e.CH_NAME || this._connect == Con_e.EXPRESSION)
            {
                this._misc = misc;
            }
            
        }

        /// <summary>
        /// this method is used to spilce the signal use verilog operator {}
        /// </summary>
        /// <returns></returns>
        private String JoinSignalWithWidth(SignalWidth width, String instanceDef, String name)
        {
            String ret;
            ret = "{";
            for(int i = 0; i < (width.end-width.start+1); i++)
            {
                if((width.end-i == width.start) && (width.start == 0))//last one
                {
                    ret += String.Format("`{0:s}.{1:s}{2:d}", instanceDef, name, (width.end-i)); 
                }
                else
                {
                    ret += String.Format("`{0:s}.{1:s}{2:d},", instanceDef, name, (width.end-i)); 
                }
            }
            if(width.start > 0)
            {
                ret += String.Format("{0:d}'d0", width.start);
            }
            ret += "};";
            return ret;

        }
        /// <summary>
        ///this function generate the connection string according conncet filed
        /// </summary>
        /// <returns> the connection string</returns>
        private String GenConnection()
        {
            String ret;
            switch(_connect)
            {
                case Con_e.DEFAULT: //empty or NA in table
                    ret = JoinSignalWithWidth(_width, _instanceDef, _name);
                    return ret;
                case Con_e.CH_NAME: 
                    ret = JoinSignalWithWidth(_width, _instanceDef, _misc);
                    return ret;
                case Con_e.ONE:
                    ret = String.Format("'h{0:x}", Math.Pow(2, _width.end+1)-1);
                    return ret;
                case Con_e.ZERO:
                    ret = String.Format("'h0");
                    return ret;
                case Con_e.EXPRESSION:
                    ret = _misc;
                    return ret;
                defaut:
                    return "";
            }
            return "";
        }

        public void DumpJson()
        {

        }

        public void DumpXml()
        {

        }
    }


    class SignalsChecker: IChecker
    {
        private ISheet sheet;
        private ILogger log;

        private JsonWriter writer;
        private IList<Signal> sigList;
        public IList<Signal> SigList
        {
           get
           {
               return sigList;
           }
        }
        public SignalsChecker(ISheet sheet, ILogger log, JsonWriter writer)
        {
           this.sheet = sheet;
           this.log = log;
           this.writer = writer;
           this.sigList = new List<Signal>();
        }

        /// <summary>
        /// check the title row 
        /// </summary>
        /// <param name="row">title row in worksheet</param>
        /// <returns>0 is ok</returns>
        public int TitleLineCheck(IRow titileRow)
        {
            if(titileRow.LastCellNum < 3)
            {
                log.LogError("Table column number error");
                log.LogError("there are three columns: SignalName|instance|IO|Connection");
                return 1;
            }
            foreach(var cell in titileRow.Cells)
            {
                switch(cell.ColumnIndex)
                {
                    case 0:
                        if(cell.CellType != CellType.String && cell.StringCellValue != "SignalName")
                        {
                           log.LogError("The first column of the title line must be SignalName");
                           return 1; 
                        }
                        break;
                    case 1:
                        if(cell.CellType != CellType.String && cell.StringCellValue != "Instance")
                        {
                            log.LogError("The second column of the title line must be Instance");
                            return 1;
                        }
                        break;

                    case 2:
                        if(cell.CellType != CellType.String && cell.StringCellValue != "IO")
                        {
                            log.LogError("The second column of the title line must be IO");
                            return 1;
                        }
                        break;
                    case 3:
                        if(cell.CellType != CellType.String && cell.StringCellValue != "Conncetion")
                        {
                            log.LogError("The third column of the title line must be Conncetion");
                            return 1;
                        }
                        break;
                    default:
                        if(cell.CellType != CellType.Blank)
                        {
                            log.LogError("Table column number error");
                            log.LogError("there are three columns: SignalName|IO|Connection");
                            return 1;
                        }
                        break;

                }
           }
           return 0;
        }

        /// <summary>
        /// check empty line in worksheet 
        /// </summary>
        /// <param name="row">row in tbale</param>
        /// <returns>1 is empty line</returns>
        private bool EmptyLineCheck(IRow row)
        {
            bool isEmpty = true;
            foreach(var cell in row.Cells)
            {
                if(cell.CellType != CellType.Blank)
                {
                    isEmpty = false;
                    break;
                }
            } 
            return isEmpty;
        }

        /// <summary>
        /// normal row check, check every cell for the row
        /// </summary>
        /// <param name="row"></param>
        /// <returns> the Signal object if success, null if failed</returns>
        private Signal NormalRowCheck(IRow row)
        {
            String name = "", insRef = "INS", io = "", con = "", misc = "";
            int start = 0, end = 0;
            ICell cell;

            cell = row.GetCell(0);
            if(SignalNameCellCheck(cell, ref name, ref end, ref start) == 1)
            {
                log.LogError("SignalName Cell Format Error");
                return null;
            }
            cell = row.GetCell(1);
            insRef = cell.CellType == CellType.Blank ? "INS" : cell.StringCellValue;
            cell = row.GetCell(2);
            io = cell.CellType == CellType.Blank ? "I": cell.StringCellValue;
            cell = row.GetCell(3);
            if(ConncetionCellCheck(cell, ref con, ref misc) == 1)
            {
                log.LogError("Connection Cell Format Error");
                return null;
            }
            return new Signal(name, insRef, io, start, end, con, misc);
        }

        /// <summary>
        /// check the signal name cell content
        /// the content contain the the signal name and signal width 
        /// the type is signalnameEndnum~startnum, example is address35~4
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="name">signal name</param>
        /// <param name="width_end">signal width </param>
        /// <param name="width_start">signal width</param>
        /// <returns>0 is ok</returns>
        private int SignalNameCellCheck(ICell cell, ref String name, ref int width_end, ref int width_start)
        {
            String content = cell.StringCellValue;
            Regex re = new Regex(@"(\w+)((\d{1,2})~(\d{1,2})?");

            Match m = re.Match(content);
            if(!m.Success)
            {
               log.LogError(String.Format("Row {0:d} Signal Name cell content format Error", cell.Row.RowNum)); 
               log.LogError("Signal Name content format is:"); 
               log.LogError("single bit signal only need signal name"); 
               log.LogError("multi-bits signal need signamewidth_end~width_start, just like address35~4"); 
               return 1;
            }
            else
            {
                name = m.Groups[0].Captures[0].Value;
                if(m.Groups.Count == 4) 
                {
                    width_end = Convert.ToInt32(m.Groups[2].Captures[0].Value);
                    width_start = Convert.ToInt32(m.Groups[3].Captures[0].Value);
                }
                return 0;
            }

        }

        /// <summary>
        /// check the connection cell content
        /// the format of the connect cell is Type%Misc 
        /// connection description: Type%misc
        ///Type is Con_e enume name, "" and "NA" means Con_e.DEFAULT
        ///misc: only "CH_NAME" and "EXPRESSION" has  property
        ///examples "NA", "CH_NAME"%"AA", "EXPRESSION"%"AA||BB"
        /// </summary>
        /// <param name="cell">Conncetion cell</param>
        /// <param name="connect">Conncetion Type</param>
        /// <param name="misc">Conncetion misc</param>
        /// <returns>0 is ok</returns>
        private int ConncetionCellCheck(ICell cell, ref String con, ref String misc)
        {
            String []sArray = cell.StringCellValue.Split('%');

            switch(sArray[0])
            {
                case "":
                case "ONE":
                case "ZERO":
                case "NA":
                    if(sArray.Length != 1)
                    {
                        log.LogError("Connection Type NA or ONE or ZERO don't need msic parameter");
                        log.LogError("Connection cell be Type");
                        return 1;
                    }
                    con = sArray[0];
                    misc = "";
                    break;
                case "CH_NAME":
                case "EXPRESSION":
                    if(sArray.Length != 2)
                    {
                        log.LogError("Connection Type CH_NAME or EXPRESSION need msic parameter");
                        log.LogError("Connection cell should be Type%MISC");
                    }
                    con = sArray[0];
                    misc = sArray[1];
                    break;
            }
            return 0;
        }

        /// <summary>
        ///Check the excel table format
        ///1. check the title line 
        ///2. check the empty line
        ///3. check the normal row
        /// </summary>
        /// <returns>return 0 is ok</returns>
        public int Check()
        {
            String sigName;
            String insRef;
            String io;
            int width_start, width_end;
            String con;
            String misc;


            //check title line
            if(TitleLineCheck(sheet.GetRow(0)) != 0)
            {
               log.LogError("Title Line Format ERROR"); 
               return 1;
            }

            foreach(var row in sheet)
            {
               IRow tmpRow = row as IRow;
               if(tmpRow.RowNum == 0)
               {
                   continue;
               }
               if(EmptyLineCheck(tmpRow)) 
               {
                    log.LogInformation("Ignore empty row");
               }
               else
               {
                    Signal sig = NormalRowCheck(tmpRow);
                    if(sig != null)
                    {
                        sigList.Add(sig);
                    }
               }
            } 
            return 0;
        }

        private void DumpJson()
        {

        }
        private void DumpXml()
        {

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
                Console.WriteLine("ERROR:this program can not support Dump to TXT");
                return;
            }
        }
    }
}