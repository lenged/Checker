namespace ZX.Checker
{
    public enum Dump_e {TXT, JSON, XML};
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
}


    