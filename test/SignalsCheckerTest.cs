using System;
using Xunit;
using Newtonsoft.Json;
using STU.SignalsChecker;
using System.IO;
using System.Text;

namespace STU.SignalsChecker.Test
{
    public class SignalsChecker_TestCase
    {
        private Signal s;
        public SignalsChecker_TestCase()
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
}
