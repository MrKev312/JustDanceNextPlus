using JustDanceNextPlus.Utilities;

namespace JustDanceNextPlus.JustDanceClasses.Database;

public class JustDancePlaylist
{
  public Guid Guid { get; set; } = Guid.Empty;
  public string PlaylistName { get; set; } = "";
  public string ListSource { get; set; } = "editorial";
  public OasisTag LocalizedTitle { get; set; } = new();
  public OasisTag LocalizedDescription { get; set; } = new();
  public string DefaultLanguage { get; set; } = "en";
  public string CoverUrl { get; set; } = "";
  public string CoverDetailsUrl { get; set; } = "";
  public List<GuidTag> Tags { get; set; } = [];
  public List<GuidTag> OffersTags { get; set; } = [];
  public bool Hidden { get; set; } = false;

  // The maps
  public List<MapTag> ItemList { get; set; } = [];
}
