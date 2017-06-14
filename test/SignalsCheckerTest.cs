using System;
using Xunit;
using Newtonsoft.Json;

namespace STU.SignalsChecker.test
{
    public class SignalsChecker_TestCase
    {
        private Signal s;
        public SignalsChecker_TestCase()
        {
            s = new Signal(name: "LTA", width_start:3, Con :"NA");
        }
        [Fact]
        public void Test_JsonSerializer()
        {
            Console.WriteLine(JsonConvert.SerializeObject(s, new SignalJsonConverter()));
        }
    }
}
