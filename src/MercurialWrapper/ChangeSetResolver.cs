using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using doe.Common.Diagnostics;
using doe.MercurialWrapper.Model;

namespace doe.MercurialWrapper
{
  /// <summary>
  /// 
  /// </summary>
  public class ChangeSetResolver
  {
    private readonly Mercurial _hg;
    private readonly Dictionary<string, Repository> _subRepositories =
      new Dictionary<string, Repository>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeSetResolver"/> class.
    /// </summary>
    /// <param name="hg">a mercurial instanze</param>
    /// <param name="repository">The repository to resolve.</param>
    public ChangeSetResolver(Mercurial hg, Repository repository)
    {
      _hg = hg;
      ResolveRepository(repository);
    }

    /// <summary>
    /// Resolves the repository.
    /// </summary>
    /// <param name="repository">The repository.</param>
    private void ResolveRepository(Repository repository)
    {
      ResolveChangesets(repository);
      ResolveSubRepoChanges(repository);
      ResolveParentIds(repository);
    }

    /// <summary>
    /// Resolves the changesets of the given repository.
    /// </summary>
    /// <param name="repository">The repository.</param>
    private void ResolveChangesets(Repository repository)
    {
      repository.ChangeLogEntries = _hg.HgLog(repository)
        .Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
        .Select(item => new ChangeSet(item, repository.LocalPath))
        .ToList();
    }

    /// <summary>
    /// Resolves the sub repo changes of the given repository.
    /// </summary>
    /// <param name="repository">The repository.</param>
    private void ResolveSubRepoChanges(Repository repository)
    {

      foreach (var change in repository.
        ChangeLogEntries.Where(x => x.Files.Contains(".hgsubstate")))
      {
        var result = _hg.HgSubstates(repository, change.ChangeSetId);

        var substates = result.Split(new[] {"\n"}, StringSplitOptions.None)
          .Select(item => new SubState(item)).ToList()
          .Where(x => !string.IsNullOrEmpty(x.SubRepo)).ToList();

        var mergedOrEmptySubstates = new List<SubState>();

        foreach (var substate in substates)
        {
          if (string.IsNullOrEmpty(substate.SubRepo))
          {
            mergedOrEmptySubstates.Add(substate);
            continue;
          }

          var target =
            substates.FirstOrDefault(
              x =>
                x.SubRepo == substate.SubRepo
                && x.AddedChangeset != substate.AddedChangeset
                && x.RemovedChangeset != substate.RemovedChangeset);

          if (target != null)
          {
            if (substate.TryMerge(target))
            {
              mergedOrEmptySubstates.Add(target);
            }
          }
        }

        foreach (var mergedSubstate in mergedOrEmptySubstates)
        {
          substates.Remove(mergedSubstate);
        }

        foreach (var state in substates)
        {
          if (!_subRepositories.ContainsKey(state.SubRepo.ToLower()))
          {
            var subRepository = new Repository(
              Path.Combine(repository.LocalPath,state.SubRepo));
            ResolveRepository(subRepository);

            _subRepositories.Add(state.SubRepo.ToLower(),subRepository);
          }

          if (!change.SubRepoChanges.ContainsKey(state.SubRepo.ToLower()))
          {
            var cs = GetChangeSetsFromRange(_subRepositories[state.SubRepo.ToLower()],
                state.RemovedChangeset, state.AddedChangeset);

            change.SubRepoChanges.Add(state.SubRepo.ToLower(), cs);
          }
          else
          {
            Log.Error(
              string.Format("the key {0} already exists in SubRepoChanges", 
                state.SubRepo.ToLower()));
          }
        }
      }
    }

    /// <summary>
    /// Resolves the parent ids of the given repository.
    /// </summary>
    /// <param name="repository">The repository.</param>
    private void ResolveParentIds(Repository repository)
    {
      foreach (var change in repository.ChangeLogEntries
        .Where(change => change.ParentIds != null))
      {
        if (change.ParentIds.Count != 0)
        {
          foreach (var parent in change.ParentIds.Keys
            .Select(id => repository.ChangeLogEntries
            .FirstOrDefault(x => x.ChangeSetId == id))
            .Where(parent => parent != null))
          {
            change.Parents.Add(parent);
          }
        }
        else
        {
          var parent = repository.ChangeLogEntries
            .FirstOrDefault(x => x.ChangeSetId == change.ChangeSetId - 1);

          if (parent != null)
          {
            change.Parents.Add(parent);
          }
        }
      }
    }

    /// <summary>
    /// Gets the change sets from range.
    /// </summary>
    /// <param name="repository">The repository.</param>
    /// <param name="fromRevision">From revision.</param>
    /// <param name="toRevision">To revision.</param>
    /// <returns></returns>
    private List<ChangeSet> GetChangeSetsFromRange(Repository repository, 
      string fromRevision, string toRevision)
    {
      var f = repository.ChangeLogEntries.
        FirstOrDefault(x => x.ChangeSetHash == fromRevision);
      var l = repository.ChangeLogEntries.
        FirstOrDefault(x => x.ChangeSetHash == toRevision);

      if (f != null && l != null)
      {
        return GetChangeSetsFromRange(repository, f, l);
      }
      return new List<ChangeSet>();
    }

    /// <summary>
    /// Gets the change sets from range.
    /// </summary>
    /// <param name="repository">The repository.</param>
    /// <param name="fromRevision">From revision.</param>
    /// <param name="toRevision">To revision.</param>
    /// <returns></returns>
    private List<ChangeSet> GetChangeSetsFromRange(Repository repository,
      ChangeSet fromRevision, ChangeSet toRevision)
    {
      if (fromRevision != null && toRevision != null)
      {
        return repository.ChangeLogEntries.Where(
          x => x.Branch == fromRevision.Branch
          && x.ChangeSetId <= toRevision.ChangeSetId
          && x.ChangeSetId > fromRevision.ChangeSetId).ToList();
      }
      return new List<ChangeSet>();
    }

    /// <summary>
    /// Gets the change sets between tags.
    /// </summary>
    /// <param name="repository">The repository.</param>
    /// <param name="tag">The tag.</param>
    /// <returns></returns>
    public List<ChangeSet> GetChangeSetsBetweenTags(Repository repository,
      string tag)
    {
      var tagedChangeSet = repository.ChangeLogEntries
        .FirstOrDefault(x => x.Tag == tag);

      if (tagedChangeSet != null)
      {
        var tagBeforeCurrent =
        repository.ChangeLogEntries.OrderByDescending(x => x.ChangeSetId)
          .FirstOrDefault(
          x => x.ChangeSetId < tagedChangeSet.ChangeSetId 
            && !string.IsNullOrEmpty(x.Tag) 
            && x.Branch == tagedChangeSet.Branch);

        if (tagBeforeCurrent != null)
        {
          return GetChangeSetsFromRange(repository, tagBeforeCurrent, tagedChangeSet);
        }
      }
      return new List<ChangeSet>();
    }
  }
}
