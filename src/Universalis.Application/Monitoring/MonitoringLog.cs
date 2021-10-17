using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Globalization;
using System.IO;

namespace Universalis.Application.Monitoring
{
    public class MonitoringLog<TLine, TLineClassMap>
        where TLine : class
        where TLineClassMap : ClassMap<TLine>
    {
        private const char Newline = '\n';

        private readonly string _filename;
        private readonly long _maxFileSizeBytes;

        public MonitoringLog(string filename, long maxFileSizeBytes)
        {
            _filename = filename ?? throw new ArgumentNullException(nameof(filename));
            _maxFileSizeBytes = maxFileSizeBytes switch
            {
                0 => throw new ArgumentException("Max log file size may not be 0."),
                _ => maxFileSizeBytes,
            };

            // Write the file header if the file doesn't exist
            if (File.Exists(filename)) return;
            CreateFile();
        }

        public void Append(TLine line)
        {
            if (_maxFileSizeBytes > 0 && new FileInfo(_filename).Length > _maxFileSizeBytes)
            {
                var rotatedFileName =
                    $"{Path.GetFileNameWithoutExtension(_filename)}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{Path.GetExtension(_filename)}";
                File.Move(_filename, rotatedFileName);
                CreateFile();
            }

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

        private void CreateFile()
        {
            using var writer = new StreamWriter(_filename);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture) { NewLine = Newline.ToString() };
            using var csv = new CsvWriter(writer, config);
            csv.Context.RegisterClassMap<TLineClassMap>();
            csv.WriteHeader<TLine>();
        }
    }
}
