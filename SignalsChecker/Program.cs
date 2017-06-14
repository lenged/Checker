using System;
using Npoi.Core.SS;
using Npoi.Core.SS.UserModel;
using Npoi.Core.Util;
using Npoi.Core.POIFS;
using Npoi.Core.XSSF.UserModel;
using Npoi.Core.HSSF.UserModel;
using System.IO;
using System.Collections;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using STU.Checker;

namespace STU.SignalsChecker
{
    class Program
    {
        static readonly String Usage = @"this program is used to checker the interface table and generate json file
                                     Usage: ifchecker -i inputfile [-o outputfile]
                                        i: input filename, 
                                        o: output filename
                                     Example: ifchecker -i a";
                                    
        /// <summary>
        /// handle command line arguments 
        /// </summary>
        /// <param name="args">command line arguments</param>
        /// <param name="log">loger implements ILogger</param>
        /// <param name="input">input file name</param>
        /// <param name="output">output file name</param>
        /// <returns>0 is ok, 1 is error</returns>
        static int HandleArgs(string[] args, ILogger log, out String input, out String output)
        {
            if(args.Length != 2 && args.Length != 4)
            {
                log.LogError("Argument ERROR");
                log.LogDebug(Usage);
                input = null;
                output = null;
                return 1;
            }
            else if(args.Length == 2)
            {
                if("-i".CompareTo(args[0]) != 0)
                {
                    log.LogError("Argument ERROR");
                    log.LogDebug(Usage);
                    input = null;
                    output = null;
                    return 1;
                }
                if(!File.Exists(args[1]))
                {
                    log.LogError(String.Format("Argument ERROR file {0} can not find", args[1]));
                    input = null;
                    output = null;
                    return 1;
                }
                input = args[1];
                output = null;
                return 0;
            }
            else
            {
                if("-i".CompareTo(args[0]) != 0)
                {
                    log.LogError("Argument ERROR");
                    log.LogDebug(Usage);
                    input = null;
                    output = null;
                    return 1;
                }
                else if("-o".CompareTo(args[2]) != 0)
                {
                    log.LogError("Argument ERROR");
                    log.LogDebug(Usage);
                    input = null;
                    output = null;
                    return 1;
                }
                if(!File.Exists(args[1]))
                {
                    log.LogError(String.Format("Argument ERROR file {0} can not find", args[1]));
                    input = null;
                    output = null;
                    return 1;
                }
                input = args[1];
                output = args[3];
                return 0;
            }
        }
        static void Main(string[] args)
        {
            String inputName, outputName;
            JsonWriter writer;
            IWorkbook wb;
            IFChecker checker;
            ILogger log;

            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole(LogLevel.Trace);
            log = loggerFactory.CreateLogger("IFChecker");

            if(HandleArgs(args, log, out inputName, out outputName) != 0)
            {
                return;
            }
            if(outputName == null)
            {
                outputName = Path.GetDirectoryName(inputName) + "/" + Path.GetFileNameWithoutExtension(inputName)+".json";
            }
            using(FileStream inputFs = File.Open(inputName, FileMode.Open, FileAccess.Read)) 
            {
                using(FileStream outputFs = File.Open(outputName, FileMode.Create, FileAccess.Write))
                {
                    writer = new JsonTextWriter(new StreamWriter(outputFs));
                    wb = new XSSFWorkbook(inputFs);
                    checker = new IFChecker(wb, log, writer);
                    checker.Check();
                    checker.Dump(Dump_e.JSON);
                }
            }
        }
    }
}
