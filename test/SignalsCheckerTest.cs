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
            row.CreateCell(1).SetCellValue("Instance");
            row.CreateCell(2).SetCellValue("IO");
            row.CreateCell(3).SetCellValue("Connection");
            // 1st row
            row = _sheet.CreateRow(1);
            row.CreateCell(0).SetCellValue("LTA39~2");
            row.CreateCell(1).SetCellValue("CPUIF");
            row.CreateCell(2).SetCellValue("I");
            row.CreateCell(3).SetCellValue("NA");
            
            _checker = new SignalsChecker(sheet:_sheet, log:_logger, writer:_writer);
        }

        [Fact]
        public void Test_SignalsChecker_Check()
        {
           IList<Signal> sigList = new List<Signal>();
           sigList.Add(new Signal(name:"LTA", instanceDef:"CPUIF", io:"I", width_start:2, width_end:39, Con:"NA"));
           _checker.Check(); 
           Assert.Equal(sigList, (_checker as SignalsChecker).SigList);
        }
    }

}
