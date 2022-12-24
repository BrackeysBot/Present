using CSharpVitamins;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Present.Data.ValueConverters;

/// <summary>
///     Converts a <see cref="ShortGuid" /> of <see cref="Uri" /> values to and from an array of bytes.
/// </summary>
internal sealed class ShortGuidToBytesConverter : ValueConverter<ShortGuid, byte[]>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ShortGuidToBytesConverter" /> class.
    /// </summary>
    public ShortGuidToBytesConverter()
        : this(null)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ShortGuidToBytesConverter" /> class.
    /// </summary>
    public ShortGuidToBytesConverter(ConverterMappingHints? mappingHints)
        : base(v => ToBytes(v), v => FromBytes(v), mappingHints)
    {
    }

    private static ShortGuid FromBytes(byte[] bytes)
    {
        return new ShortGuid(new Guid(bytes));
    }

    private static byte[] ToBytes(ShortGuid value)
    {
        return value.Guid.ToByteArray();
    }
}
