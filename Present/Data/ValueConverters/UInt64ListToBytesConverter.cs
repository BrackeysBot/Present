using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Present.Data.ValueConverters;

/// <summary>
///     Converts a <see cref="List{T}" /> of <see cref="Uri" /> values to and from an array of bytes.
/// </summary>
internal sealed class UInt64ListToBytesConverter : ValueConverter<List<ulong>, byte[]>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UInt64ListToBytesConverter" /> class.
    /// </summary>
    public UInt64ListToBytesConverter()
        : this(null)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="UInt64ListToBytesConverter" /> class.
    /// </summary>
    public UInt64ListToBytesConverter(ConverterMappingHints? mappingHints)
        : base(v => ToBytes(v), v => FromBytes(v), mappingHints)
    {
    }

    private static List<ulong> FromBytes(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var reader = new BinaryReader(stream);
        int listCount = reader.Read7BitEncodedInt();

        var list = new List<ulong>(listCount);

        for (var index = 0; index < list.Count; index++)
            list.Add(reader.ReadUInt64());

        return list;
    }

    private static byte[] ToBytes(List<ulong> values)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write7BitEncodedInt(values.Count);

        for (var index = 0; index < values.Count; index++)
        {
            ulong value = values[index];
            writer.Write(value);
        }

        return stream.ToArray();
    }
}
