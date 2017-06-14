using System;
using Xunit;
using Newtonsoft.Json;
using Npoi.Core.SS.UserModel;
using Npoi.Core.HSSF.UserModel;
using Npoi.Core.XSSF.UserModel;
using STU.SignalsChecker;
using STU.Checker;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Collections.Generic;

namespace STU.SignalsChecker.Test
{
    public class SignalComparetor: IEqualityComparer<Signal>
    {
       public bool Equals(Signal lsh, Signal rsh)
       {
            return lsh.Equals(rsh);
       } 

       public int GetHashCode(Signal obj)
       {
           return obj.GetHashCode();
       }
    }
    public class Signal_TestCase
    {
        private Signal s;
        public Signal_TestCase()
        {
            s = new Signal(name: "LTA", width_end:3, Con :"NA");
        }
        [Fact]
        public void Test_JsonSerializer()
        {
            String jsonString;
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            JsonTextWriter writer = new JsonTextWriter(sw);

            s.DumpJson(writer);
            jsonString = sb.ToString();
            Assert.Equal("{\"Name\":\"LTA\",\"IO\":\"INPUT\",\"InstanceDef\":\"INS\",\"Width\":\"4\",\"Connection\":\"{`INS.LTA3,`INS.LTA2,`INS.LTA1,`INS.LTA0}\"}",
            jsonString);            
        }
    }

    public class SignalsChecker_TestCase
    {
        private IChecker _checker;
        private JsonWriter _writer;
        private ISheet _sheet;

        private ILogger _logger;
        public SignalsChecker_TestCase()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            IRow row;
            _writer = new JsonTextWriter(sw);

            _logger = new LoggerFactory().CreateLogger("Test"); 
            
            _sheet = new XSSFWorkbook().CreateSheet("test");
            //Title Row
            row = _sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("SignalName");
            row.CreateCell(1).SetCellValue("Width");
            row.CreateCell(2).SetCellValue("Instance");
            row.CreateCell(3).SetCellValue("IO");
            row.CreateCell(4).SetCellValue("Connection");
            // 1st row
            row = _sheet.CreateRow(1);
            row.CreateCell(0).SetCellValue("LTA");
            row.CreateCell(1).SetCellValue("NA");
            row.CreateCell(2).SetCellValue("CPUIF");
            row.CreateCell(3).SetCellValue("I");
            row.CreateCell(4).SetCellValue("NA");
            
            _checker = new SignalsChecker(sheet:_sheet, log:_logger, writer:_writer);
        }

        [Fact]
        public void Test_SignalsChecker_Check()
        {
           IList<Signal> sigList = new List<Signal>();
           sigList.Add(new Signal(name:"LTA", instanceDef:"CPUIF", io:"I", width_start:0, width_end:0, Con:"NA"));
           _checker.Check(); 
           Assert.Equal(sigList, (_checker as SignalsChecker).SigList);
        }
    }

    public class IFChecker_TestCase
    {
        private IChecker _checker;
        private JsonWriter _writer;
        private IWorkbook _workBook;
        private StringBuilder _sb;
        private ILogger _logger;
        public IFChecker_TestCase()
        {
            IRow row;
            ISheet sheet;
            StringWriter sw;

            _sb = new StringBuilder();
            sw = new StringWriter(_sb);
            _writer = new JsonTextWriter(sw);

            _logger = new LoggerFactory().CreateLogger("Test");

            _workBook = new XSSFWorkbook(); 
            
            sheet = _workBook.CreateSheet("testIF");
            //Title Row
            row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("SignalName");
            row.CreateCell(1).SetCellValue("Width");
            row.CreateCell(2).SetCellValue("Instance");
            row.CreateCell(3).SetCellValue("IO");
            row.CreateCell(4).SetCellValue("Connection");
            // 1st row
            row = sheet.CreateRow(1);
            row.CreateCell(0).SetCellValue("LTA");
            row.CreateCell(1).SetCellValue("39~2");
            row.CreateCell(2).SetCellValue("CPUIF");
            row.CreateCell(3).SetCellValue("I");
            row.CreateCell(4).SetCellValue("NA");

            //2nd row
            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue("LTADS");
            row.CreateCell(1).SetCellValue("NA");
            row.CreateCell(2).SetCellValue("CPUIF");
            row.CreateCell(3).SetCellValue("I");
            row.CreateCell(4).SetCellValue("NA");
            
            //3rd row
            row = sheet.CreateRow(3);
            row.CreateCell(0).SetCellValue("LTNA");
            row.CreateCell(1).SetCellValue("NA");
            row.CreateCell(2).SetCellValue("CPUIF");
            row.CreateCell(3).SetCellValue("I");
            row.CreateCell(4).SetCellValue("NA");

           
            _checker = new IFChecker(wb:_workBook, log:_logger, writer:_writer);
        }

        [Fact]
        public void Test_IFChecker_Check()
        {
           IList<IF> ifList = new List<IF>();
           IList<Signal> sigList = new List<Signal>();
           sigList.Add(new Signal(name:"LTA", instanceDef:"CPUIF", io:"I", width_start:2, width_end:39, Con:"NA"));
           sigList.Add(new Signal(name:"LTADS", instanceDef:"CPUIF", io:"I", width_start:0, width_end:0, Con:"NA"));
           sigList.Add(new Signal(name:"LTNA", instanceDef:"CPUIF", io:"I", width_start:0, width_end:0, Con:"NA"));
           ifList.Add(new IF(sigList, "testIF"));
           _checker.Check(); 
           Assert.Equal(ifList, (_checker as IFChecker).IfList);
        }

        [Fact]
        public void Test_IFChecker_Dump()
        {
            _checker.Check();
            _checker.Dump(Dump_e.JSON);
            Console.WriteLine(_sb.ToString());
        }
    }


}
