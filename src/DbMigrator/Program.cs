using System;
using System.Linq;

namespace DbMigrator
{
    public static class Program
    {
        private static void WriteError(string message = null, Exception exception = null)
        {
            var lastConsoleColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            if (message != null)
                Console.WriteLine(message);
            if (exception != null)
                Console.WriteLine(exception);
            Console.ForegroundColor = lastConsoleColor;
        }

        public static int Main(string[] args)
        {
            try
            {
                ScriptStack.Apply(args.FirstOrDefault() ?? "server=localhost;database=universalis_test;user=dalamud;password=dalamud");
            }
            catch (InvalidOperationException e)
            {
                WriteError(exception: e);
                return -1;
            }

            return 0;
        }
    }
}