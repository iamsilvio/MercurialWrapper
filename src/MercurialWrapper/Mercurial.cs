using System.Diagnostics;
using System.IO;
using doe.Common.Diagnostics;
using doe.Common.Diagnostics.Model;
using doe.MercurialWrapper.Model;

namespace doe.MercurialWrapper
{
    public class Mercurial
    {
      private readonly string _hgPathExecutable;

      /// <summary>
      /// Initializes a new instance of the <see cref="Mercurial"/> class.
      /// </summary>
      /// <param name="hgPathExecutable">The path to the hg executable.</param>
      public Mercurial(string hgPathExecutable)
      {
        if (!File.Exists(hgPathExecutable))
        {
          Log.Error(string.Format("mercurial not found at {0}", hgPathExecutable));
          throw new FileNotFoundException("File not found", hgPathExecutable);
        }

        _hgPathExecutable = hgPathExecutable;
      }

      /// <summary>
      /// returns the output of a hg pull.
      /// </summary>
      /// <param name="repo">The repo.</param>
      /// <returns></returns>
      public BackgroundProcessResult HgPull(Repository repo)
      {
        var processInfo = new ProcessStartInfo
        {
          FileName = _hgPathExecutable,
          WorkingDirectory = repo.LocalPath,
          Arguments = "pull"
        };
        return BackgroundProcess.Execute(processInfo);
      }

      /// <summary>
      /// returns the output of a hg update.
      /// </summary>
      /// <param name="repo">The repo.</param>
      /// <param name="revision">The revision.</param>
      /// <returns></returns>
      public BackgroundProcessResult HgUpdate(Repository repo, string revision)
      {
        var processInfo = new ProcessStartInfo
        {
          FileName = _hgPathExecutable,
          WorkingDirectory = repo.LocalPath,
          Arguments = "update -r " + revision
        };
        return BackgroundProcess.Execute(processInfo);
      }

      /// <summary>
      /// returns the output of a hg clone.
      /// </summary>
      /// <param name="repoRemotePath">The repo remote path.</param>
      /// <param name="targetRoot">The target root.</param>
      /// <returns></returns>
      public BackgroundProcessResult HgClone(string repoRemotePath, string targetRoot)
      {
        if (!Directory.Exists(targetRoot))
        {
          Directory.CreateDirectory(targetRoot);
        }

        var processInfo = new ProcessStartInfo
        {
          FileName = _hgPathExecutable,
          WorkingDirectory = targetRoot,
          Arguments = "clone " + repoRemotePath
        };
        return BackgroundProcess.Execute(processInfo);
      }

      /// <summary>
      /// returns the output of a hg substates.
      /// </summary>
      /// <param name="repo">The repo.</param>
      /// <param name="fromRev">From rev.</param>
      /// <param name="toRev">To rev.</param>
      /// <returns></returns>
      public BackgroundProcessResult HgSubstates(Repository repo, int fromRev, int toRev)
      {
        var processInfo = new ProcessStartInfo
        {
          FileName = _hgPathExecutable,
          Arguments = string.Format("diff -r {0}:{1} .hgsubstate -U 0", fromRev, toRev),
          WorkingDirectory = repo.LocalPath,
        };
        return BackgroundProcess.Execute(processInfo);
      }

      /// <summary>
      /// returns the output of a hg substates.
      /// </summary>
      /// <param name="repo">The repo.</param>
      /// <param name="rev">The rev.</param>
      /// <returns></returns>
      public BackgroundProcessResult HgSubstates(Repository repo, int rev)
      {
        var processInfo = new ProcessStartInfo
        {
          FileName = _hgPathExecutable,
          Arguments = string.Format("diff -c {0} .hgsubstate -U 0", rev),
          WorkingDirectory = repo.LocalPath,
        };
        return BackgroundProcess.Execute(processInfo);
      }

      /// <summary>
      /// returns the output of a hg log.
      /// </summary>
      /// <param name="repo">The repo.</param>
      /// <returns></returns>
      public BackgroundProcessResult HgLog(Repository repo)
      {
        var processInfo = new ProcessStartInfo
        {
          FileName = _hgPathExecutable,
          Arguments = "log --template tag:{tags}\\nchangeset:{rev}:{node}\\nbranch:{branch}\\ndate:{date}\\nsummary:{desc}\\nuser:{author}\\nfiles:{files}\\nparents:{parents}\\n\\n",
          WorkingDirectory = repo.LocalPath,
        };
        return BackgroundProcess.Execute(processInfo);
      }
    }
}
