using System;
using System.Collections.Generic;
using System.Linq;
using doe.Common.Diagnostic;
using MercurialWrapper.Model;

namespace MercurialWrapper
{
  class ChangeSetResolver
  {
    private readonly Mercurial _hg;
    private readonly Dictionary<string, Repository> _subRepositories =
      new Dictionary<string, Repository>();

    public ChangeSetResolver(Mercurial hg, Repository repository)
    {
      _hg = hg;
      ResolveRepository(repository);
    }

    public void ResolveRepository(Repository repository)
    {

      ResolveChangesets(repository);
      ResolveSubRepoChanges(repository);
      ResolveParentIds(repository);
    }

    private void ResolveChangesets(Repository repository)
    {
      repository.ChangeLogEntries = _hg.HgLog(repository)
        .Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
        .Select(item => new ChangeSet(item, repository.LocalPath))
        .ToList();
    }

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
            var subRepository = new Repository();
            ResolveRepository(subRepository);
          }

          if (!change.SubRepoChanges.ContainsKey(state.SubRepo.ToLower()))
          {
            change.SubRepoChanges.Add(state.SubRepo.ToLower(), 
              GetChangeSetsFromRange(_subRepositories[state.SubRepo.ToLower()],
                state.RemovedChangeset, state.AddedChangeset));
          }
          else
          {
            Log.Error("the key already exists in this dict");
          }
        }
      }
    }


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

    private List<ChangeSet> GetChangeSetsFromRange(Repository repository, 
      string fromRevision, string toRevision)
    {
      var f = repository.ChangeLogEntries.
        FirstOrDefault(x => x.ChangeSetHash == fromRevision);
      var l = repository.ChangeLogEntries.
        FirstOrDefault(x => x.ChangeSetHash == toRevision);

      if (f != null && l != null)
      {
        return repository.ChangeLogEntries.Where(x => x.Branch == f.Branch 
          && x.ChangeSetId <= l.ChangeSetId 
          && x.ChangeSetId > f.ChangeSetId).ToList();
      }
      return new List<ChangeSet>();
    }
  }
}
