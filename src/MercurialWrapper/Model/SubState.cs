using System.Text.RegularExpressions;

namespace MercurialWrapper.Model
{
  /// <summary>
  /// The State of a SubRepository
  /// </summary>
  public class SubState
  {
    /// <summary>
    /// Gets or sets the removed changeset.
    /// </summary>
    /// <value>
    /// The removed changeset.
    /// </value>
    public string RemovedChangeset { get; set; }
    /// <summary>
    /// Gets or sets the added changeset.
    /// </summary>
    /// <value>
    /// The added changeset.
    /// </value>
    public string AddedChangeset { get; set; }
    /// <summary>
    /// Gets or sets the sub repo.
    /// </summary>
    /// <value>
    /// The sub repo.
    /// </value>
    public string SubRepo { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SubState"/> class.
    /// </summary>
    /// <param name="item">The content of a .substate file.<example></example></param>
    public SubState(string item)
    {
      var removed = Regex.Match(item, @"(?:-)(\w{40})(?:\s)(.*)");
      if (removed.Success)
      {
        RemovedChangeset = removed.Groups[1].Value;
        SubRepo = removed.Groups[2].Value;
      }

      var added = Regex.Match(item, @"(?:\+)(\w{40})(?:\s)(.*)");
      if (!added.Success) return;

      AddedChangeset = added.Groups[1].Value;
      SubRepo = added.Groups[2].Value;
    }

    /// <summary>
    /// Tries the merge.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <returns>true if the merge was successful</returns>
    public bool TryMerge(SubState source)
    {
      if (SubRepo != source.SubRepo) return false;

      if (string.IsNullOrEmpty(AddedChangeset)
          && !string.IsNullOrEmpty(source.AddedChangeset))
      {
        AddedChangeset = source.AddedChangeset;
        return true;
      }

      if (string.IsNullOrEmpty(RemovedChangeset)
          && !string.IsNullOrEmpty(source.RemovedChangeset))
      {
        RemovedChangeset = source.RemovedChangeset;
        return true;
      }
      return false;
    }
  }
}