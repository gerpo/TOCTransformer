using Deedle;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using CsvHelper;
using System.Collections.Generic;

namespace TOCTransfomer
{
    internal class Transformer
    {
        private string _filePath;
        private IList<OutputRow> _dataRows;

        private readonly string[] ATD_HEAD = new[] {
            "[Main]",
            "Type=ATD",
            "[Table1]",
            "Name=BTS_TABLE"
        };


        internal void ReadCSV(string file)
        {
            try
            {
                _filePath = file;
                var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    Comment = '#',
                    AllowComments = true,
                    Delimiter = "|",
                    Mode = CsvMode.NoEscape,
                    BadDataFound = x =>
                    {
                        Console.WriteLine($"Bad data: <{x.RawRecord}>");
                    },
                };
                _dataRows = new List<OutputRow>();
                
                using var streamReader = File.OpenText(_filePath);
                using (var csvReader = new CsvReader(streamReader, config))
                {
                    csvReader.Read();
                    csvReader.ReadHeader();
                    while(csvReader.Read())
                    {
                        if (!FilterStations(csvReader)) continue;
                        var f = CreateOutputObject(csvReader);
                        
                        _dataRows.Add(f);
                    }
                }
                
                //_dataFrame = Frame.ReadCsv(file, hasHeaders: true, separators: "|");
            }
            catch (Exception e)
            {
                throw new ReadingException("Error reading file.", e);
            }
        }

        internal async Task WriteTransformedCSV(string filePath = null)
        {
            if (!_dataRows.Any())
                return;

            if (filePath is null)
                filePath = Path.ChangeExtension(_filePath, ".txt");

            try
            {
                await Task.Run(() =>
                {
                    var outputValues = _dataRows
                        .OrderBy(r => r.Name)
                        .Select((r, i) =>
                         {
                             r.UniqueId = i + 1;
                             return r;
                         });

                    outputValues = CheckForDirection(outputValues);


                    var outputFrame = Frame.FromRecords(outputValues);
                    outputFrame.SaveCsv(filePath, separator: ';', culture: new CultureInfo("en-US"));
                    outputFrame.SaveCsv($"{filePath}.csv", separator: ';', culture: new CultureInfo("de-DE"));
                });
            }
            catch (Exception)
            {
                throw new ColumnException();
            }

        }

        private static bool FilterStations(CsvReader r)
        {
            return r.GetField<string>("SITE_MANAGER.BS_TYPE") == "TVFZ" || r.GetField<string>("SITE_MANAGER.BS_TYPE") == "mBS";
        }
        private static IEnumerable<OutputRow> CheckForDirection(IEnumerable<OutputRow> outputValues)
        {
            foreach (var outputRow in outputValues)
            {
                if (outputValues.Count(v => v.Name == outputRow.Name) > 1)
                    outputRow.IsDirected = 1;
            }
            return outputValues;    
        }

        private static OutputRow CreateOutputObject(CsvReader r)
        {
            return new OutputRow()
            {
                Name = r.GetField<string>("SITE.NAME"),
                PosLongitude = InputStringToOutputFloat(r.GetField<string>("SITE.LONGITUDE")),
                PosLatitude = InputStringToOutputFloat(r.GetField<string>("SITE.LATITUDE")),
                Power = r.GetField<float>("ZONE.FS_POWER_DBM", 0),
                IsDirected = r.GetField<int>("ANTENNA_SYSTEM.DIRECTION_DEG") > 0 ? 1 : 0,
                Direction = r.GetField<int>("ANTENNA_SYSTEM.DIRECTION_DEG"),
                Channel = r.GetField<int>("CELL.G_BCCH") + 3599,
                LAC = r.GetField<int>("CELL.LAC"),
                LAC_Hex = r.GetField<int>("CELL.LAC").ToString("X").ToUpper(),
                CELL_NE_ID = r.GetField<string>("CELL.NE_ID"),
                SITE_BS_TYPE = r.GetField<string>("SITE_MANAGER.BS_TYPE"),
                ANTENNA_SYSTEM = r.GetField<string>("ANTENNA_SYSTEM.NAME"),
            };
        }

        private static float InputStringToOutputFloat(string input)
        {
            var index = input.IndexOf(".");
            if (index < 0) return float.Parse(input, new CultureInfo("en-US"));
            var output = input.Replace(".", "*").Remove(index, 1).Insert(index, ".").Replace("*", "");

            return float.Parse(output, new CultureInfo("en-US"));
        }

        public async Task WriteATD(string filePath = null)
        {
            if (!_dataRows.Any())
                return;

            if (filePath is null)
                filePath = Path.ChangeExtension(_filePath, ".ATD");

            await Task.Run(() =>
            {
                using StreamWriter writer = new(filePath);
                WriteATDHead(writer);

                writer.WriteLine($"File={Path.ChangeExtension(Path.GetFileName(_filePath), ".txt")}");
                writer.WriteLine($"Columns_Size={OutputRow.OUTPUT_TYPES.Count}");

                WriteATDBody(writer);
            });
        }

        private void WriteATDHead(StreamWriter writer)
        {
            foreach (var headLine in ATD_HEAD)
            {
                writer.WriteLine(headLine);
            }
        }

        private static void WriteATDBody(StreamWriter writer)
        {
            for (var i = 0; i < OutputRow.OUTPUT_TYPES.Count; i++)
            {
                var column = OutputRow.OUTPUT_TYPES.ElementAt(i);
                writer.WriteLine($"Columns{i}_Name={column.Key}");
                writer.WriteLine($"Columns{i}_Type={column.Value}");
            }
        }

        [Serializable]
        internal class ReadingException : Exception
        {
            public ReadingException()
            {
            }

            public ReadingException(string message) : base(message)
            {
            }

            public ReadingException(string message, Exception innerException) : base(message, innerException)
            {
            }

            protected ReadingException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }

        [Serializable]
        internal class ColumnException : Exception
        {
            public ColumnException()
            {
            }

            public ColumnException(string message) : base(message)
            {
            }

            public ColumnException(string message, Exception innerException) : base(message, innerException)
            {
            }

            protected ColumnException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }
    }
}
