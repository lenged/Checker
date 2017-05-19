using System;
using Npoi.Core.SS;
using Npoi.Core.SS.UserModel;
using Npoi.Core.Util;
using Npoi.Core.POIFS;
using Npoi.Core.XSSF.UserModel;
using Npoi.Core.HSSF.UserModel;
using System.IO;
using System.Collections;

namespace SignalsChecker
{
    public enum Dump_e {TXT, XML, JSON};
    
    public interface IChecker
    {
        /// <summary>
        /// check the execl file and read the content to memory
        /// </summary>
        /// <returns>return 0 if check ok</returns>
        int Check();

        /// <summary>
        ///  dump the memory content to file
        /// </summary>
        /// <param name="t">dump file type</param>
        void Dump(Dump_e t);
    }

    struct SignalWidth
    {
       public int start;
       public int end; 
    }

    /// <summary>
    /// systemverilog interface 
    /// </summary>
    class IF
    {
        Signal[] sigList;

        public IF(int size) 
        {
            sigList = new Signal[size];
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
    class Signal
    {
        enum Con_e {DEFAULT, CH_NAME, ONE, ZERO, EXPRESSION};
        enum IO_e {INPUT, OUTPUT}
        string name;
        string instanceDef;
        IO_e io;
        SignalWidth width;
        Con_e connect;
        String misc; //for CH_NAME and EXPRESSION Con_e

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
        public Signal(String name, String instanceDef = "INS", String io = "I", int width_start=0, int width_end=39, String Con="")
        {
            this.name = name;
            this.instanceDef = instanceDef;
            if(io == "I")
            {
                this.io = IO_e.INPUT;
            }
            else
            {
                this.io = IO_e.OUTPUT;
            }
            this.width.start = width_start;
            this.width.end = width_end;
            switch(Con.Split('%')[0].ToUpper())
            {
                case "NA":
                case "":
                    this.connect = Con_e.DEFAULT;
                    break;
                case "CH_NAME":
                    this.connect = Con_e.CH_NAME;
                    break;
                case "EXPRESSION":
                    this.connect = Con_e.EXPRESSION;
                    break;
                default:
                    this.connect = Con_e.DEFAULT;
                    break;
            }
            if(this.connect == Con_e.CH_NAME || this.connect == Con_e.EXPRESSION)
            {
                this.misc = Con.Split('%')[1];
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
            switch(connect)
            {
                case Con_e.DEFAULT: //empty or NA in table
                    ret = JoinSignalWithWidth(width, instanceDef, name);
                    return ret;
                case Con_e.CH_NAME: 
                    ret = JoinSignalWithWidth(width, instanceDef, misc);
                    return ret;
                case Con_e.ONE:
                    ret = String.Format("'h{0:x}", Math.Pow(2, width.end+1)-1);
                    return ret;
                case Con_e.ZERO:
                    ret = String.Format("'h0");
                    return ret;
                case Con_e.EXPRESSION:
                    ret = misc;
                    return ret;
                defaut:
                    return "";
            }
            return "";
        }

        public void DumpJson()
        {

        }

    }
    class SignalsChecker: IChecker
    {
        private String filename;

        private IF[] IF_List;
        public SignalsChecker(String filename)
        {
           this.filename = filename;
        }

        /// <summary>
        ///Check the excel table format
        ///1. check the title line 
        ///2. check the connection String  
        /// </summary>
        /// <returns>return 0 is ok</returns>
        public int Check()
        {
            IWorkbook wb;

            using(var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                wb = new XSSFWorkbook(fs); 
                for
                ISheet sheet = wb.GetSheetAt();
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
    class Program
    {
        static void Main(string[] args)
        {
            
        }
    }
}
