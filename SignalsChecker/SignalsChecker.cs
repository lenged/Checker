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
using System.Diagnostics;

namespace STU.SignalsChecker
{
    struct SignalWidth
    {
       public int start;
       public int end; 
    }
    public class Signal
    {
        enum Con_e {DEFAULT, CH_NAME, ONE, ZERO, EXPRESSION};
        enum IO_e {INPUT, OUTPUT}
        string _name;
        string _instanceDef;
        IO_e _io;
        SignalWidth _width;
        Con_e _connect;
        String _misc; //for CH_NAME and EXPRESSION Con_e

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
            if(width.end == 0 && width.start == 0)
            {
                ret = String.Format("`{0}.{1}", instanceDef, name);
                return ret;
            }
            else
            {
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
                ret += "}";
                return ret;
            }
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
                    ret = String.Format("{0:d}'h{1:x}", _width.end+1, Math.Pow(2, _width.end+1)-1);
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

        public override bool Equals(object obj)
        {
            Signal rsh;

            rsh = obj as Signal;
            if(rsh == null) // obj is not Signal object
            {
                return false;
            }

            return (this._name == rsh._name) &&
                   (this._width.end == rsh._width.end) &&
                   (this._width.start == rsh._width.start) &&
                   (this._instanceDef == rsh._instanceDef) &&
                   (this._io == rsh._io) &&
                   (this._connect == rsh._connect);
        }

        public override String ToString()
        {
            return String.Format("Name: {0}, Instance: {1}, IO: {2}, width-start {3:d} -- end {4:d}, con: {5}", _name, _instanceDef, _io, _width.start, _width.end, _connect);
        }

        public void DumpJson(JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Name");
            writer.WriteValue(this._name);
            writer.WritePropertyName("IO");
            writer.WriteValue(this._io.ToString());
            writer.WritePropertyName("InstanceDef");
            writer.WriteValue(this._instanceDef);
            writer.WritePropertyName("Width");
            writer.WriteValue(String.Format("{0:d}", (this._width.end+1)));
            writer.WritePropertyName("Connection");
            writer.WriteValue(this.JoinSignalWithWidth(_width, _instanceDef, _name));
            writer.WriteEndObject();
        }

        public void DumpXml()
        {

        }
    }


    public class SignalsChecker: IChecker
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
            if(titileRow.LastCellNum < 5)
            {
                log.LogError("Table column number error");
                log.LogError("there are Five columns: SignalName|width|Instance|IO|Connection");
                return 1;
            }
            foreach(var cell in titileRow.Cells)
            {
                switch(cell.ColumnIndex)
                {
                    case 0:
                        if(cell.CellType != CellType.String && cell.StringCellValue != "SignalName")
                        {
                           log.LogError("The columnA of the title line must be SignalName");
                           return 1; 
                        }
                        break;
                    case 1:
                        if(cell.CellType != CellType.String && cell.StringCellValue != "Width")
                        {
                            log.LogError("The columnB of the title line must be Width");
                            return 1;
                        }
                        break;
                    case 2:
                        if(cell.CellType != CellType.String && cell.StringCellValue != "Instance")
                        {
                            log.LogError("The columnC of the title line must be Instance");
                            return 1;
                        }
                        break;

                    case 3:
                        if(cell.CellType != CellType.String && cell.StringCellValue != "IO")
                        {
                            log.LogError("The columnD of the title line must be IO");
                            return 1;
                        }
                        break;
                    case 4:
                        if(cell.CellType != CellType.String && cell.StringCellValue != "Conncetion")
                        {
                            log.LogError("The columnE of the title line must be Conncetion");
                            return 1;
                        }
                        break;
                    default:
                        if(cell.CellType != CellType.Blank)
                        {
                            log.LogError("Table column number error");
                            log.LogError("there are Five columns: SignalName|Width|Instance|IO|Connection");
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

            cell = row.GetCell(0, MissingCellPolicy.CREATE_NULL_AS_BLANK);
            if(cell.StringCellValue == "")
            {
                log.LogError(String.Format("Row {0}, ColumnA SignalName Cell Format Error", cell.RowIndex));
                return null;
            }
            name = cell.StringCellValue;
            cell = row.GetCell(1, MissingCellPolicy.CREATE_NULL_AS_BLANK);
            if(WidthCellCheck(cell, ref end, ref start) == 1)
            {
                log.LogError(String.Format("Row {0}, ColumnB Width Cell Format Error", cell.RowIndex));
                return null;
            }
            cell = row.GetCell(2);
            insRef = cell.CellType == CellType.Blank ? "INS" : cell.StringCellValue;
            cell = row.GetCell(3, MissingCellPolicy.CREATE_NULL_AS_BLANK);
            io = cell.CellType == CellType.Blank ? "I": cell.StringCellValue;
            cell = row.GetCell(4, MissingCellPolicy.CREATE_NULL_AS_BLANK);
            if(ConncetionCellCheck(cell, ref con, ref misc) == 1)
            {
                log.LogError(String.Format("Row {0}, ColumnE connect Cell Format Error", cell.RowIndex));
                return null;
            }
            return new Signal(name, insRef, io, start, end, con, misc);
        }

        /// <summary>
        /// this function is used check width cell of the signal 
        /// the cell format is end:sart, example: 39:2
        /// single bit signal, this cell can be 0:0, NA or Empty
        /// </summary>
        /// <param name="cell"> the width cell</param>
        /// <param name="width_end">width_end value</param>
        /// <param name="width_start">width_start value</param>
        /// <returns></returns>
        private int WidthCellCheck(ICell cell, ref int width_end, ref int width_start)
        {
            Regex widthRe = new Regex(@"(\d{1,2})~(\d{1,2})");

            if(cell.CellType == CellType.Blank)
            {
                width_end = 0;
                width_start = 0;
                return 0;
            }
            else if(cell.StringCellValue == String.Empty)
            {
                width_end = 0;
                width_start = 0;
                return 0;
            }
            else if(cell.StringCellValue == "NA")
            {
                width_end = 0;
                width_start = 0;
                return 0;
            }
            else
            { 
                Match widthm = widthRe.Match(cell.StringCellValue);
                if(!widthm.Success)
                {
                   log.LogError(String.Format("Row {0:d} Width cell content format Error\n", cell.Row.RowNum)); 
                   log.LogDebug("Width cell format is:\nsingle-bit signal this cell can be 0~0, NA, or Empty\nmulti-bits signal need width_end:width_start, just like 35:4"); 
                   return 1;
                }
                if(widthm.Groups[1].Captures.Count > 0)
                {
                    width_end = Convert.ToInt32(widthm.Groups[1].Captures[0].Value);
                }
                else
                {
                    log.LogError("multi-bits signal need width_end:width_start, 35~4"); 
                    return 1;
                }
                if(widthm.Groups[2].Captures.Count > 0)
                {
                    width_start = Convert.ToInt32(widthm.Groups[2].Captures[0].Value);
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
            String []sArray;

            if(cell.CellType == CellType.Blank)
            {
                sArray = new String[1]{""};
            }
            sArray = cell.StringCellValue.Split('%');
            switch(sArray[0])
            {
                case "":
                case "ONE":
                case "ZERO":
                case "NA":
                    if(sArray.Length != 1)
                    {
                        log.LogError("Connection Type NA or ONE or ZERO don't need msic parameter");
                        log.LogError("Connection cell be Type%");
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
                    log.LogWarning("Ignore empty row");
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
            writer.WriteStartArray();
            foreach(var sig in sigList)
            {
                sig.DumpJson(writer);
            }
            writer.WriteEndArray();
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