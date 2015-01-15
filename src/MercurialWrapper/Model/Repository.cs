using System.Collections.Generic;

namespace doe.MercurialWrapper.Model
{
  /// <summary>
  /// 
  /// </summary>
  public class Repository
  {
    /// <summary>
    /// Gets or sets the local path.
    /// </summary>
    /// <value>
    /// The local path.
    /// </value>
    public string LocalPath { get; set; }

    /// <summary>
    /// Gets or sets the change log entries.
    /// </summary>
    /// <value>
    /// The change log entries.
    /// </value>
    public List<ChangeSet> ChangeLogEntries { get; set; }

    public Repository(string localPath)
    {
      LocalPath = localPath;
    }
  }
}