using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace Universalis.Application.Monitoring
{
    public class MonitoringLog<TLine, TLineClassMap>
        where TLine : class
        where TLineClassMap : ClassMap<TLine>
    {
        private const char Newline = '\n';

        private readonly string _filename;

        public MonitoringLog(string filename)
        {
            _filename = filename ?? throw new ArgumentNullException(nameof(filename));
            
            // Write the file header if the file doesn't exist
            if (File.Exists(filename)) return;
            using var writer = new StreamWriter(filename);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture) { NewLine = Newline.ToString() };
            using var csv = new CsvWriter(writer, config);
            csv.Context.RegisterClassMap<TLineClassMap>();
            csv.WriteHeader<TLine>();
        }

        public void Append(TLine line)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                NewLine = Newline.ToString(),
                HasHeaderRecord = false,
            };

            using var fstream = File.Open(_filename, FileMode.Append);
            using var writer = new StreamWriter(fstream);
            writer.Write(Newline); // Newline for previous line of CSV, since we're appending
            using var csv = new CsvWriter(writer, config);
            csv.Context.RegisterClassMap<TLineClassMap>();
            csv.WriteRecord(line);
        }
    }
}
