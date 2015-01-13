using System.Collections.Generic;

namespace MercurialWrapper.Model
{
  public class Repository
  {
    public string LocalPath { get; set; }

    public List<ChangeSet> ChangeLogEntries { get; set; }
  }
}