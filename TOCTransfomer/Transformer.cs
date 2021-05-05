using Deedle;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace TOCTransfomer
{
    internal class Transformer
    {
        private string _filePath;
        private Frame<int, string> _dataFrame;

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
                _dataFrame = Frame.ReadCsv(file, hasHeaders: true, separators: ";");
            }
            catch (Exception)
            {
                throw new ReadingException();
            }
        }

        internal async Task WriteTransformedCSV(string filePath = null)
        {
            if (_dataFrame is null)
                return;

            if (filePath is null)
                filePath = Path.ChangeExtension(_filePath, ".txt");

            try
            {
                await Task.Run(() =>
                {
                    var outputValues = _dataFrame.Rows
                        .Where(r => FilterStations(r.Value))
                        .Select(r => CreateOutputObject(r.Value))
                        .SortBy(r => r.Name)
                        .IndexOrdinally()
                        .Select(r =>
                         {
                             r.Value.UniqueId = r.Key + 1;
                             return r.Value;
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

        private static bool FilterStations(ObjectSeries<string> r)
        {
            return r.GetAs<string>("SITE_MANAGER.BS_TYPE") == "TVFZ" || r.GetAs<string>("SITE_MANAGER.BS_TYPE") == "mBS";
        }
        private static Series<int, OutputRow> CheckForDirection(Series<int, OutputRow> outputValues)
        {
            foreach (var outputRow in outputValues.Values)
            {
                if (outputValues.Values.Count(v => v.Name == outputRow.Name) > 1)
                    outputRow.IsDirected = 1;
            }
            return outputValues;
        }

        private static OutputRow CreateOutputObject(ObjectSeries<string> r)
        {
            return new OutputRow()
            {
                Name = r.GetAs<string>("SITE.NAME"),
                PosLongitude = InputStringToOutputFloat(r.GetAs<string>("SITE.LONGITUDE")),
                PosLatitude = InputStringToOutputFloat(r.GetAs<string>("SITE.LATITUDE")),
                Power = r.GetAs<float>("ZONE.FS_POWER_DBM", 0),
                IsDirected = r.GetAs<int>("ANTENNA_SYSTEM.DIRECTION_DEG") > 0 ? 1 : 0,
                Direction = r.GetAs<int>("ANTENNA_SYSTEM.DIRECTION_DEG"),
                Channel = r.GetAs<int>("CELL.G_BCCH") + 3599,
                LAC = r.GetAs<int>("CELL.LAC"),
                LAC_Hex = r.GetAs<int>("CELL.LAC").ToString("X").ToUpper(),
                CELL_NE_ID = r.GetAs<string>("CELL.NE_ID"),
                SITE_BS_TYPE = r.GetAs<string>("SITE_MANAGER.BS_TYPE"),
                ANTENNA_SYSTEM = r.GetAs<string>("ANTENNA_SYSTEM.NAME"),
            };
        }

        private static float InputStringToOutputFloat(string input)
        {
            var index = input.IndexOf(".");
            var output = input.Replace(".", "*").Remove(index, 1).Insert(index, ".").Replace("*", "");

            return float.Parse(output, new CultureInfo("en-US"));
        }

        public async Task WriteATD(string filePath = null)
        {
            if (_dataFrame is null)
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
