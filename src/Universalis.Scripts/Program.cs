using System;
using System.Linq;

namespace Universalis.Scripts
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
            if (args.Length == 0)
            {
                WriteError("error: expecting connection string");
                return -1;
            }

            try
            {
                ScriptStack.Apply(args.First());
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