using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using doe.Common.Diagnostics;

namespace MercurialWrapper.Model
{
  /// <summary>
  /// a representation of a Mercurial commit message
  /// </summary>
  [DebuggerDisplay("ChangeSetId = {ChangeSetId} | Repository = {Repository}")]
  public class ChangeSet
  {
    private int _changeSetId;
    private DateTime _date;

    /// <summary>
    /// Gets or sets the change set identifier.
    /// </summary>
    /// <value>
    /// The change set identifier.
    /// </value>
    /// <example>
    /// 1626
    /// </example>
    public int ChangeSetId
    {
      get { return _changeSetId; }
      set { _changeSetId = value; }
    }

    /// <summary>
    /// Gets or sets the change set hash.
    /// </summary>
    /// <value>
    /// The change set hash.
    /// </value>
    /// <example>
    /// 019b52c19f1d
    /// </example>
    public string ChangeSetHash { get; set; }
    /// <summary>
    /// Gets or sets the branch.
    /// </summary>
    /// <value>
    /// The branch.
    /// </value>
    public string Branch { get; set; }
    /// <summary>
    /// Gets or sets the tag.
    /// </summary>
    /// <value>
    /// The tag.
    /// </value>
    public string Tag { get; set; }
    /// <summary>
    /// Gets or sets the user.
    /// </summary>
    /// <value>
    /// The user.
    /// </value>
    public User User { get; set; }
    /// <summary>
    /// Gets or sets the files.
    /// </summary>
    /// <value>
    /// The files.
    /// </value>
    public string[] Files { get; set; }
    /// <summary>
    /// Gets or sets the sub repo changes.
    /// </summary>
    /// <value>
    /// The sub repo changes.
    /// </value>
    public Dictionary<string, List<ChangeSet>> SubRepoChanges { get; set; }
    /// <summary>
    /// Gets or sets the date.
    /// </summary>
    /// <value>
    /// The date.
    /// </value>
    public DateTime Date
    {
      get { return _date; }
      set { _date = value; }
    }
    /// <summary>
    /// Gets or sets the summary.
    /// </summary>
    /// <value>
    /// The summary.
    /// </value>
    public string Summary { get; set; }
    /// <summary>
    /// Gets or sets the parent ids.
    /// </summary>
    /// <value>
    /// The parent ids.
    /// </value>
    public Dictionary<int, string> ParentIds { get; set; }

    /// <summary>
    /// Gets or sets the parents.
    /// </summary>
    /// <value>
    /// The parents.
    /// </value>
    public List<ChangeSet> Parents { get; set; }

    /// <summary>
    /// Gets or sets the type of the change.
    /// </summary>
    /// <value>
    /// The type of the change.
    /// </value>
    public ChangeType ChangeType { get; set; }
    /// <summary>
    /// Gets or sets the repository.
    /// </summary>
    /// <value>
    /// The repository.
    /// </value>
    public string Repository { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogEntry"/> class.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="repo">The repo.</param>
    public ChangeSet(string item, string repo)
    {
      var entries = repo.Split(new[] {
        Path.DirectorySeparatorChar,  
      Path.AltDirectorySeparatorChar  
    }, StringSplitOptions.RemoveEmptyEntries);

      Parents = new List<ChangeSet>();

      Repository = entries.LastOrDefault();

      var changeset = Regex.Match(item, @"(?:changeset:)(\d{1,6})(?::)(\w{40})");
      if (changeset.Success)
      {
        if (!Int32.TryParse(changeset.Groups[1].Value, out _changeSetId))
        {
          Log.Warning(string.Format("'{0}' is not in the proper format.", changeset.Groups[1].Value));
        }
        ChangeSetHash = changeset.Groups[2].Value;
      }
      else
      {
        Log.Warning("changeset not found");
      }

      var branch = Regex.Match(item, @"(?:branch:)(.*)");
      if (branch.Success)
      {
        Branch = branch.Groups[1].Value;
      }
      else
      {
        Log.Warning("branch not found");
      }

      var tag = Regex.Match(item, @"(?:tag:)(.*)");
      if (tag.Success)
      {
        if (tag.Groups[1].Value != "tip")
        {
          Tag = tag.Groups[1].Value;
        }
      }
      else
      {
        Log.Warning("tag not found");
      }

      var date = Regex.Match(item, @"(?:date:)(.*)(?:\.0)(-|\+)(\d*)");
      if (date.Success)
      {
        Int32 unixTimeStamp;

        if (!int.TryParse(date.Groups[1].Value, out unixTimeStamp))
        {
          Log.Warning(string.Format("'{0}' is not in the proper format.", date.Groups[1].Value));
        }

        Date = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
          .AddSeconds(unixTimeStamp).ToLocalTime();
      }
      else
      {
        Log.Warning("date not found");
      }

      var summary = Regex.Match(item, @"(?:summary:)(F|S|B|R)(?::\s)(.*)");
      if (summary.Success)
      {
        Summary = summary.Groups[2].Value;

        if (String.Compare(summary.Groups[1].Value, "F", StringComparison.OrdinalIgnoreCase) == 0)
        {
          ChangeType = ChangeType.Feature;
        }
        else if (string.Compare(summary.Groups[1].Value, "S", StringComparison.OrdinalIgnoreCase) == 0)
        {
          ChangeType = ChangeType.Specification;
        }
        else if (string.Compare(summary.Groups[1].Value, "B", StringComparison.OrdinalIgnoreCase) == 0)
        {
          ChangeType = ChangeType.Bugfix;
        }
        else if (string.Compare(summary.Groups[1].Value, "R", StringComparison.OrdinalIgnoreCase) == 0)
        {
          ChangeType = ChangeType.Refactoring;
        }
      }
      else
      {
        summary = Regex.Match(item, @"(?:summary:)(Merge)");
        if (summary.Success)
        {
          ChangeType = ChangeType.Merge;
        }
        else
        {
          summary = Regex.Match(item, @"(?:summary:)(Added|Removed)(?:\stag\s)([v|r|t]\d{1,3}\.\d{1,3}\.\d{1,5}\.\d{1,5}(\.b\d{1,5})?)(\sfor\schangeset\s\w{12})");
          if (summary.Success)
          {
            ChangeType = ChangeType.System;
          }
          else
          {
            summary = Regex.Match(item, @"(?:summary:)(.*)");
            if (summary.Success)
            {
              ChangeType = ChangeType.Refactoring;
              Summary = summary.Groups[1].Value;
            }
            else
            {
              Log.Warning("summary not found");
            }
          }
        }
      }

      var user = Regex.Match(item, @"(?:user:)([^<:]*){1}(?:(?:<)([^>:]*)(?:>))?", RegexOptions.Multiline);
      if (user.Success)
      {
        var name = user.Groups[1].Value.EndsWith("files") ? user.Groups[1].Value.Substring(0, user.Groups[1].Value.Length - 5) : user.Groups[1].Value;
        User = new User(name, user.Groups[2].Value);
      }
      else
      {
        Log.Warning("user not found");
      }

      var files = Regex.Match(item, @"(?:files:)(.*)");
      if (files.Success)
      {
        Files = files.Groups[1].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
      }
      else
      {
        Log.Warning("files not found");
      }

      if (Files.Contains(".hgsubstate"))
      {
        SubRepoChanges = new Dictionary<string, List<ChangeSet>>();
      }

      var parents = Regex.Match(item, @"(?:parents:)(.*)");
      if (parents.Success)
      {
        ParentIds = new Dictionary<int, string>();

        var parentStrings = parents.Groups[1].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var par in parentStrings)
        {
          var valuePair = par.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

          int key;
          if (int.TryParse(valuePair[0], out key))
          {
            ParentIds.Add(key, valuePair[1]);
          }
        }
      }
      else
      {
        Log.Warning("parents not found");
      }
    }

    public List<ChangeSet> GetParentChain()
    {
      var result = new List<ChangeSet>();

      if (Parents.Count == 0) return result;

      foreach (var entry in Parents)
      {
        result.AddRange(entry.GetParentChain());
        if (!result.Contains(entry))
        {
          result.Add(entry);
        }
      }
      return result.Distinct().ToList();
    }
  }
}
