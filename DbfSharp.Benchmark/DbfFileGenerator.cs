using System.Text;

namespace DbfSharp.Benchmark
{
    public class DbfFieldInfo(string name, char type, int length, int decimalCount = 0)
    {
        public string Name { get; } = name;
        public char Type { get; } = type;
        public int Length { get; } = length;
        public int DecimalCount { get; } = decimalCount;
    }

    public class DbfFileGenerator
    {
        public static void Generate(string path, int rowCount, List<DbfFieldInfo> fields)
        {
            using var stream = new FileStream(path, FileMode.Create);
            using var writer = new BinaryWriter(stream);

            // Header
            writer.Write((byte)0x03); // Version: dBase III plus, no memo
            writer.Write((byte)(DateTime.Now.Year - 1900));
            writer.Write((byte)DateTime.Now.Month);
            writer.Write((byte)DateTime.Now.Day);
            writer.Write(rowCount);

            var headerLength = (short)(32 * (fields.Count + 1) + 1);
            writer.Write(headerLength);

            short recordLength = 1; // Deletion flag
            foreach (var field in fields)
            {
                recordLength += (short)field.Length;
            }
            writer.Write(recordLength);

            for (var i = 0; i < 20; i++)
            {
                writer.Write((byte)0x00);
            }

            // Field descriptors
            foreach (var field in fields)
            {
                var fieldNameBytes = new byte[11];
                Encoding.ASCII.GetBytes(field.Name, 0, field.Name.Length, fieldNameBytes, 0);
                writer.Write(fieldNameBytes);

                writer.Write((byte)field.Type);
                writer.Write(0); // Field data address
                writer.Write((byte)field.Length);
                writer.Write((byte)field.DecimalCount);

                for (var i = 0; i < 14; i++)
                {
                    writer.Write((byte)0x00);
                }
            }

            writer.Write((byte)0x0D); // Header terminator

            // Data records
            for (var i = 0; i < rowCount; i++)
            {
                writer.Write((byte)0x20); // Deletion flag: not deleted

                foreach (var field in fields)
                {
                    var value = "";
                    switch (field.Type)
                    {
                        case 'C':
                            value = $"Row {i + 1} Col {field.Name}".PadRight(field.Length, ' ');
                            if (value.Length > field.Length)
                                value = value[..field.Length];
                            break;
                        case 'N':
                            value = (i + 1).ToString().PadLeft(field.Length, ' ');
                            break;
                        case 'D':
                            value = DateTime.Now.ToString("yyyyMMdd");
                            break;
                        case 'L':
                            value = (i % 2 == 0) ? "T" : "F";
                            break;
                    }

                    var buffer = Encoding.ASCII.GetBytes(value);
                    writer.Write(buffer);
                }
            }

            writer.Write((byte)0x1A); // End of file marker
        }
    }
}
