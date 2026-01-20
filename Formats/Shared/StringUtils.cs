using System.Globalization;
using System.Numerics;
using System.Text;
namespace MithrilToolbox.Formats.Shared;

/// <summary>
/// String parsing related helpers
/// </summary>
public class StringUtils
{
    public static string ReadNullTerminatedString(BinaryReader br)
    {
        List<byte> bytes = [];
        byte b;
        while ((b = br.ReadByte()) != 0)
            bytes.Add(b);
        return Encoding.UTF8.GetString([.. bytes]);
    }

    public static void WriteNullTerminatedString(BinaryWriter bw, string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        bw.Write(bytes);
        bw.Write((byte)0);
    }

    public static string EncodeBinaryString(byte[] rawData)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        string encodedString = Encoding.UTF8.GetString(rawData);

        if (encodedString.Contains('\uFFFD'))
        {
            Encoding sjis = Encoding.GetEncoding("shift-jis");
            byte[] rawEncodedData = Encoding.Convert(sjis, Encoding.UTF8, rawData);
            encodedString = Encoding.UTF8.GetString(rawEncodedData);
            encodedString = encodedString.Replace("\u3000", " ");
        }

        return encodedString;
    }

    public static byte[] EncodeJsonString(string jsonString)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        char utfCharacter = jsonString[0];

        if (
            (utfCharacter >= '\u3040' && utfCharacter <= '\u309F') || // Hiragana
            (utfCharacter >= '\u30A0' && utfCharacter <= '\u30FF') || // Katakana
            (utfCharacter >= '\u4E00' && utfCharacter <= '\u9FFF') || // Kanji
            (utfCharacter >= '\uFF66' && utfCharacter <= '\uFF9F')    // Halfwidth Katakana
        )
        {
            Encoding sjis = Encoding.GetEncoding("shift-jis");
            jsonString = jsonString.Replace(" ", "\u3000");
            byte[] byteData = Encoding.UTF8.GetBytes(jsonString);
            return Encoding.Convert(Encoding.UTF8, sjis, byteData);
        }

        return Encoding.UTF8.GetBytes(jsonString);
    }
}
