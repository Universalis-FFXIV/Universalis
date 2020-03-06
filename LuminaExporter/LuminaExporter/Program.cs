using Lumina.Excel.GeneratedSheets;
using LuminaExporter.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cyalume = Lumina.Lumina;

namespace LuminaExporter
{
    class Program
    {
        private readonly static string[] _gameFolders = {
            @"SquareEnix\FINAL FANTASY XIV - A Realm Reborn",
            @"FINAL FANTASY XIV - A Realm Reborn",
            @"SquareEnix\FINAL FANTASY XIV - KOREA",
            @"FINAL FANTASY XIV - KOREA",
            @"上海数龙科技有限公司\最终幻想XIV",
            @"最终幻想XIV",
        };

        private Cyalume _cyalume;

        private readonly string _outputPath = Path.Combine("..", "..", "..", "..", "..", "publicgen");

        static void Main(string[] args) => new Program().MainAsync(args).GetAwaiter().GetResult();

        public async Task MainAsync(string[] args)
        {
            if (Directory.Exists(_outputPath))
                Directory.Delete(_outputPath, true);

            Directory.CreateDirectory(_outputPath);

            if (args.Length == 2)
            {
                _cyalume = new Cyalume(args[0]);
            }
            else if (args.Length == 1)
            {
                foreach (string folder in _gameFolders)
                {
                    if (Directory.Exists(Path.Combine(Util.ProgramFilesx86(), folder, @"\game\sqpack")))
                    {
                        _cyalume = new Cyalume(folder);
                        break;
                    }
                }

                if (_cyalume == null)
                {
                    throw new DirectoryNotFoundException("sqpack folder not found!");
                }
            }
            else
            {
                throw new ArgumentException("Not enough arguments! Please provide a [sqpack path] --<operation>");
            }

            string operation = args.Length == 2 ? args[1] : args[0];
            if (operation == "--marketableitems" || operation == "-MI")
            {
                await GetMarketableItems();
            }
            else
            {
                throw new ArgumentException("The selected operation matches no known operations!");
            }
        }

        private async Task GetMarketableItems()
        {
            var fileName = @"json\item.json";
            var fileContent = new MarketableItems();
            var marketableitems = _cyalume.GetExcelSheet<Item>()
                .GetRows()
                .Where(item => item.ItemSearchCategory > 8)
                .Select(item => item.RowId);
            fileContent.itemID = marketableitems.ToArray();
            await File.WriteAllTextAsync(Path.Combine(_outputPath, fileName), JsonConvert.SerializeObject(fileContent));
            Console.WriteLine("Operation completed!");
        }
    }
}
