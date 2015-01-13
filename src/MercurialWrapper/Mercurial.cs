using System;
using System.Diagnostics;
using System.IO;
using doe.Common.Diagnostic;
using MercurialWrapper.Model;

namespace MercurialWrapper
{
    public class Mercurial
    {
      private readonly string _hgPathExecutable;

      public Mercurial(string hgPathExecutable)
      {
        _hgPathExecutable = hgPathExecutable;
      }

      public string HgPull(Repository repo)
      {
        var processInfo = new ProcessStartInfo
        {
          FileName = _hgPathExecutable,
          WorkingDirectory = repo.LocalPath,
          Arguments = "pull"
        };
        return ExecuteProcess(processInfo);
      }

      public string HgUpdate(Repository repo, int revision)
      {
        var processInfo = new ProcessStartInfo
        {
          FileName = _hgPathExecutable,
          WorkingDirectory = repo.LocalPath,
          Arguments = "update -r " + revision
        };
        return ExecuteProcess(processInfo);
      }

      public string HgClone(string repoRemotePath, string targetRoot)
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
        return ExecuteProcess(processInfo);
      }

      public string HgSubstates(Repository repo, int fromRev, int toRev)
      {
        var processInfo = new ProcessStartInfo
        {
          FileName = _hgPathExecutable,
          Arguments = string.Format("diff -r {0}:{1} .hgsubstate -U 0", fromRev, toRev),
          WorkingDirectory = repo.LocalPath,
        };
        return ExecuteProcess(processInfo);
      }
      public string HgSubstates(Repository repo, int rev)
      {
        var processInfo = new ProcessStartInfo
        {
          FileName = _hgPathExecutable,
          Arguments = string.Format("diff -c {0} .hgsubstate -U 0", rev),
          WorkingDirectory = repo.LocalPath,
        };
        return ExecuteProcess(processInfo);
      }

      public string HgLog(Repository repo)
      {
        var processInfo = new ProcessStartInfo
        {
          FileName = _hgPathExecutable,
          Arguments = "log --template tag:{tags}\\nchangeset:{rev}:{node}\\nbranch:{branch}\\ndate:{date}\\nsummary:{desc}\\nuser:{author}\\nfiles:{files}\\nparents:{parents}\\n\\n",
          WorkingDirectory = repo.LocalPath,
        };
        return ExecuteProcess(processInfo);
      }
      
      /// <summary>
      /// Executes the process.
      /// </summary>
      /// <param name="startInfo">Takes a ProcessStartInfo object to start</param>
      /// <returns></returns>
      private static string ExecuteProcess(ProcessStartInfo startInfo)
      {
        try
        {
          startInfo.RedirectStandardError = true;
          startInfo.RedirectStandardOutput = true;
          startInfo.UseShellExecute = false;

          using (var process = Process.Start(startInfo))
          {
            var output = string.Empty;
            string error = null;

            if (process != null)
            {
              output = process.StandardOutput.ReadToEnd();
              error = process.StandardError.ReadToEnd();
              process.WaitForExit();  
            }

            if (string.IsNullOrEmpty(error))
            {
              return output;
            }

            Log.Error(error);
            return string.Format("{0}\n{1}", output, error);
          }
        }
        catch (Exception e)
        {
          Log.Error(e);
          return e.Message;
        }
      }
    }
}
