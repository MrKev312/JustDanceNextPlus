using JustDanceServer.JustDanceClasses.Database;

using System.Globalization;
using System.Text;

namespace JustDanceServer.Services;

internal static class JduCatalogSelection
{
    public static IEnumerable<JustDanceSongDBEntry> From(IEnumerable<JustDanceSongDBEntry> songs)
        => songs;

    public static IEnumerable<JustDanceSongDBEntry> FromClassic(IEnumerable<JustDanceSongDBEntry> songs)
		=> songs.Where(IsClassicCompatible);

	public static bool IsClassicCompatible(JustDanceSongDBEntry song)
		=> IsClassicCompatibleText(song.MapName)
			&& IsClassicCompatibleText(song.Title)
			&& IsClassicCompatibleText(song.Artist);

    public static bool IsClassicCompatibleText(string value)
    {
        foreach (Rune rune in value.EnumerateRunes())
        {
            if (rune.IsAscii)
                continue;

            UnicodeCategory category = Rune.GetUnicodeCategory(rune);
            if (category is UnicodeCategory.NonSpacingMark
                or UnicodeCategory.SpacingCombiningMark
                or UnicodeCategory.EnclosingMark)
            {
                continue;
            }

            if ((category is UnicodeCategory.UppercaseLetter
                or UnicodeCategory.LowercaseLetter
                or UnicodeCategory.TitlecaseLetter
                or UnicodeCategory.ModifierLetter
                or UnicodeCategory.OtherLetter)
                && IsLatinCodePoint(rune.Value))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static bool IsLatinCodePoint(int codePoint)
        => codePoint is (>= 0x00C0 and <= 0x024F)
            or (>= 0x1D00 and <= 0x1D7F)
            or (>= 0x1E00 and <= 0x1EFF)
            or (>= 0x2C60 and <= 0x2C7F)
            or (>= 0xA720 and <= 0xA7FF)
            or (>= 0xAB30 and <= 0xAB6F)
            or (>= 0xFB00 and <= 0xFB06);
}
