﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NDesk.Options;
using Pundit.Core.Model;

namespace Pundit.Core.Application.Console.Commands
{
   class ResolveConsoleCommand : BaseConsoleCommand
   {
      public ResolveConsoleCommand(IConsoleOutput console, string currentDirectory, string[] args)
         : base(console, currentDirectory, args)
      {
      }

      private void ResolveParameters(out int depthIndex, out BuildConfiguration configuration, out bool force, out bool ping)
      {
         depthIndex = GetDepth();
         configuration = BuildConfiguration.Release;
         force = GetBoolParameter(false, "f|force");
         ping = GetBoolParameter(false, "p|ping");

         string sconfig = GetParameter("c:|configuration:");
         if (sconfig != null)
         {
            if (sconfig == "any")
               configuration = BuildConfiguration.Any;
            else if (sconfig == "debug")
               configuration = BuildConfiguration.Debug;
         }
      }

      private IEnumerable<Repo> GetRepositories(int depth)
      {
         return LocalConfiguration.RepositoryManager.ActiveRepositories
            //.Where(r => r.Tag != LocalConfiguration.LocalRepositoryTag)
            .OrderBy(r => r.Priority)
            .Take(depth);
      }

      private void PrintConflicts(DependencyResolution dr, VersionResolutionTable tbl, DependencyNode rootNode)
      {
         foreach(UnresolvedPackage conflict in tbl.GetConflictedPackages())
         {
            console.WriteLine(ConsoleColor.Red, dr.DescribeConflict(rootNode, conflict));
         }
      }

      private void PrintDiff(IEnumerable<PackageKeyDiff> diffs, ConsoleColor diffColor, DiffType diffType, string diffWord)
      {
         bool isMod = diffType == DiffType.Mod;

         foreach(PackageKeyDiff d in diffs.Where(pd => pd.DiffType == diffType))
         {
            console.Write(diffColor, diffWord + " ");
            console.Write("{0} v{1} ({2})", d.PackageId,
               isMod ? d.OldPackageKey.Version : d.Version, d.Platform);

            if(isMod)
            {
               console.Write(" to v");
               console.Write(ConsoleColor.Green, d.Version.ToString());
            }

            console.WriteLine("");
         }
      }

      private void PrintSuccess(IEnumerable<PackageKeyDiff> diffs)
      {
         PrintDiff(diffs, ConsoleColor.Yellow, DiffType.Add, "added");
         PrintDiff(diffs, ConsoleColor.Green, DiffType.Mod, "changed");
         PrintDiff(diffs, ConsoleColor.White, DiffType.NoChange, "no change for");
         PrintDiff(diffs, ConsoleColor.Red, DiffType.Del, "deleted");
      }

      public override void Execute()
      {
         //parse command and read manifest
         string manifestPath = GetLocalManifest();
         string projectRoot = new FileInfo(manifestPath).Directory.FullName;
         int depth;
         bool force;
         bool ping;
         BuildConfiguration configuration;
         ResolveParameters(out depth, out configuration, out force, out ping);

         console.Write(ConsoleColor.White, "manifest:\t");
         console.WriteLine(manifestPath);

         console.Write(ConsoleColor.White, "depth:\t\t");
         console.WriteLine(depth == int.MaxValue ? "max" : depth.ToString());

         console.Write(ConsoleColor.White, "configuration:\t");
         console.WriteLine(configuration == BuildConfiguration.Debug ? ConsoleColor.Yellow : ConsoleColor.Green,
                            configuration.ToString().ToLower());

         console.Write("force:\t\t");
         if(force)
            console.WriteLine(ConsoleColor.Red, "yes");
         else
            console.WriteLine("no");

         console.Write("ping only:\t");
         if(ping)
            console.WriteLine(ConsoleColor.Green, "yes");
         else
            console.WriteLine("no");

         console.WriteLine("");


         console.Write("reading manifest...\t\t");
         DevPackage devPackage = DevPackage.FromStream(File.OpenRead(manifestPath));
         console.Write(true);

         //get the repository list
         IEnumerable<Repo> repositories = GetRepositories(depth);

         //resolve dependencies
         console.Write("resolving...\t\t\t");

         var dr = new DependencyResolution(devPackage, LocalConfiguration.RepositoryManager.LocalRepository);
         var resolutionResult = dr.Resolve();

         console.Write(!resolutionResult.Item1.HasConflicts);

         if (ping) return;

         if(resolutionResult.Item1.HasConflicts)
         {
            PrintConflicts(dr, resolutionResult.Item1, resolutionResult.Item2);

            throw new ApplicationException("could not resolve manifest because of conflicts");
         }

         //ensure that all packages exist in local repository
         LocalConfiguration.PackageDownloadToLocalRepository += LocalRepository_PackageDownloadToLocalRepository;
         LocalConfiguration.DownloadLocally(resolutionResult.Item1.GetPackages());

         //install all packages
         using(PackageInstaller installer = new PackageInstaller(projectRoot,
            resolutionResult.Item1,
            devPackage,
            LocalConfiguration.RepositoryManager.LocalRepository))
         {
            installer.BeginInstallPackage += installer_BeginInstallPackage;
            installer.FinishInstallPackage += installer_FinishInstallPackage;

            if (force)
            {
               console.WriteLine("reinstalling all packages");

               installer.Reinstall(configuration);
            }
            else
            {
               IEnumerable<PackageKeyDiff> diff = installer.GetDiffWithCurrent(resolutionResult.Item1.GetPackages());

               PrintSuccess(diff);

               installer.Upgrade(configuration, diff);
            }
         }
      }

      void installer_FinishInstallPackage(object sender, Core.Model.EventArguments.PackageKeyDiffEventArgs e)
      {
         console.Write(e.Succeeded);
      }

      void installer_BeginInstallPackage(object sender, Core.Model.EventArguments.PackageKeyDiffEventArgs e)
      {
         console.Write("installing {0} v{1} ({2})...", e.PackageKeyDiff.PackageId, e.PackageKeyDiff.Version, e.PackageKeyDiff.Platform);
      }

      void LocalRepository_PackageDownloadToLocalRepository(object sender, Core.Model.EventArguments.PackageDownloadEventArgs e)
      {
         console.Write("download: {0}, b: {1}, total: {2}, now: {3}", e.PackageKey.PackageId, e.Succeeded, e.TotalSize, e.DownloadedSize);
      }
   }
}
