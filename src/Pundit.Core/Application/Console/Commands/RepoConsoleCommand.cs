﻿using System;
using System.Linq;
using Pundit.Core.Exceptions;
using Pundit.Core.Model;
using Pundit.Core.Utils;

namespace Pundit.Core.Application.Console.Commands
{
   class RepoConsoleCommand : BaseConsoleCommand
   {
      public RepoConsoleCommand(IConsoleOutput console, string currentDirectory, string[] args) : base(console, currentDirectory, args)
      {
      }

      public override void Execute()
      {
         if(NamelessParameters.Length == 0)
            throw new ArgumentException("no action");

         string action = NamelessParameters[0];

         if(action == "list")
            ListRepositories();
         else if(action == "delete")
            DeleteRepository();
         else if(action == "add")
            AddRepository();
         else if(action == "caps")
            UpdateCaps();
         else if(action == "update")
            Update();
         else if(action == "vacuum")
            VacuumRepository();
         else throw new ArgumentException("unknown action " + action);
      }

      private void VacuumRepository()
      {
         console.WriteLine("purging local repository data...");
         LocalConfiguration.RepositoryManager.ZapCache();
         console.WriteLine(ConsoleColor.Green, "complete");
      }

      private void ListRepositories()
      {
         LocalStats stats = LocalConfiguration.RepositoryManager.Stats;

         console.WriteLine("local repository");
         console.Write("env:".PadRight(20));
         //console.Write(Environment.GetEnvironmentVariable(LocalConfiguration.LocalRepositoryRootVar) ?? "<not set>");
         console.Write(" (");
         //console.Write(LocalConfiguration.LocalRepositoryRootVar);
         console.WriteLine(")");
         console.Write("occupied space:".PadRight(20));
         console.WriteLine(PathUtils.FileSizeToString(stats.OccupiedSpaceTotal));
         console.Write("binary cache:".PadRight(20));
         console.WriteLine(PathUtils.FileSizeToString(stats.OccupiedSpaceBinaries));
         console.Write("manifests:".PadRight(20));
         console.WriteLine(stats.TotalManifestsCount.ToString());

         console.WriteLine("");
         console.Write("configured repositories ({0}):", LocalConfiguration.RepositoryManager.AllRepositories.Count());
         foreach (Repo rr in LocalConfiguration.RepositoryManager.AllRepositories)
         {
            console.WriteLine("");

            console.Write("tag:".PadRight(20));
            console.WriteLine(rr.Tag);

            console.Write("enabled:".PadRight(20));
            console.WriteLine(rr.IsEnabled ? ConsoleColor.Green : ConsoleColor.Red, rr.IsEnabled ? "yes" : "no");

            console.Write("url:".PadRight(20));
            console.WriteLine(rr.Uri);

            console.Write("refresh:".PadRight(20));
            console.Write("every ");
            console.Write(rr.RefreshIntervalInHours.ToString());
            console.WriteLine(" hour(s)");

            console.Write("login:".PadRight(20));
            console.WriteLine(rr.Login);

            console.Write("api key:".PadRight(20));
            console.WriteLine(rr.ApiKey);

            console.Write("last refreshed:".PadRight(20));
            console.WriteLine(rr.LastRefreshed == DateTime.MinValue ? "never" : rr.LastRefreshed.ToString());

            console.Write("delta:".PadRight(20));
            console.WriteLine(rr.LastChangeId ?? "<null>");
         }

      }

      private void DeleteRepository()
      {
         string tag = GetParameter("tag:", 1);
         if (string.IsNullOrEmpty(tag)) throw new ApplicationException("repository tag required");

         console.Write("deleting repository...");

         try
         {
            Repo r = LocalConfiguration.RepositoryManager.GetRepositoryByTag(tag);
            if (r == null) throw new ApplicationException("repository not found");

            LocalConfiguration.RepositoryManager.Unregister(r.Id);
            console.Write(true);
         }
         catch
         {
            console.Write(false);
            throw;
         }
      }

      private void AddRepository()
      {
         string tag = GetParameter("tag:", 1);
         string uri = GetParameter("uri:", 2);
         int hours = GetIntParameter("refresh:", 3);

         if(string.IsNullOrEmpty(tag)) throw new ApplicationException("repository tag required");
         if(string.IsNullOrEmpty(uri)) throw new ApplicationException("repository uri required");
         if(hours < 1) throw new ApplicationException("positive number of hours required");
         if(LocalConfiguration.RepositoryManager.AllRepositories.Any(r => r.Tag == tag))
            throw new ApplicationException("repository '" + tag + "' already registered");

         console.WriteLine("adding repository '{0}' from {1}, refresh interval: {2} hour(s)...",
            tag, uri, hours);

         IRemoteRepository repository = RemoteRepositoryFactory.Create(uri, null, null);

         console.Write("fetching first snapshot...");
         string nextChangeId;
         var snapshot = repository.GetSnapshot(null);

         if(snapshot != null && snapshot.Changes.Length > 0)
         {
            console.Write(true);
            long repoId = 0;

            try
            {
               Repo newRepo;
               try
               {
                  console.Write("registering repository...");
                  Repo newRepo1 = new Repo(tag, uri);
                  newRepo1.RefreshIntervalInHours = hours;
                  newRepo1.LastRefreshed = DateTime.Now;
                  newRepo1.LastChangeId = snapshot.NextChangeId;
                  newRepo1.IsEnabled = true;
                  newRepo = LocalConfiguration.RepositoryManager.Register(newRepo1);
                  repoId = newRepo.Id;
                  console.Write(true);
               }
               catch
               {
                  console.Write(false);
                  throw;
               }

               console.Write("persisting {0} snapshot entries...", snapshot.Changes.Length);
               LocalConfiguration.RepositoryManager.PlaySnapshot(newRepo, snapshot);
               console.Write(true);
               console.WriteLine(ConsoleColor.Green, "repository added");
            }
            catch(Exception ex)
            {
               console.WriteLine(ConsoleColor.Red, "failed to register repository: " + ex.ToString());

               if(repoId != 0)
                  LocalConfiguration.RepositoryManager.Unregister(repoId);
            }
         }
         else
         {
            console.Write(false);
            console.WriteLine(ConsoleColor.Red, "cannot add empty repository");
         }
      }

      private void UpdateCaps()
      {
         string tag = GetParameter("tag:", 1);
         if (string.IsNullOrEmpty(tag)) throw new ApplicationException("repository tag required");

         Repo r = LocalConfiguration.RepositoryManager.GetRepositoryByTag(tag);
         if (r == null) throw new ApplicationException("repository not found");

         string enabled = GetParameter("enabled:");
         int hours = GetIntParameter("refresh:");
         string login = GetParameter("login:");
         string apiKey = GetParameter("api-key:");

         bool isEnabled;
         if (bool.TryParse(enabled, out isEnabled)) r.IsEnabled = isEnabled;
         if (hours > 0) r.RefreshIntervalInHours = hours;
         if (login != null) r.Login = login;
         if (apiKey != null) r.ApiKey = apiKey;

         console.Write("updating repository...");
         LocalConfiguration.RepositoryManager.Update(r);
         console.Write(true);
      }

      private void Update()
      {
         bool force = GetBoolParameter(false, "f|force");
         UpdateSnapshots(force);
      }

      private RemoteSnapshot GetSnapshot(Repo repo)
      {
         IRemoteRepository remote = RemoteRepositoryFactory.Create(repo.Uri, repo.Login, repo.ApiKey);

         //get changes
         try
         {
            console.Write("checking... ");

            try
            {
               RemoteSnapshot snapshot = remote.GetSnapshot(repo.LastChangeId);
               console.Write("(");
               console.Write(ConsoleColor.Green,
                             (snapshot == null || snapshot.Changes.Length == 0)
                                ? "no"
                                : snapshot.Changes.Length.ToString());
               console.Write(" changes)");
               console.Write(true);
               return snapshot;
            }
            catch(RepositoryOfflineException)
            {
               console.Write(ConsoleColor.Red, " [offline]");
               console.Write(false);
            }
         }
         catch
         {
            console.Write(false);
            throw;
         }

         return null;
      }

      private void PlaySnapshot(Repo repo, RemoteSnapshot snapshot)
      {
         try
         {
            console.Write("applying...");
            LocalConfiguration.RepositoryManager.PlaySnapshot(repo, snapshot);
            console.Write(true);
         }
         catch
         {
            console.Write(false);
            throw;
         }
      }

      public void UpdateSnapshots(bool force = false)
      {
         foreach(Repo repo in LocalConfiguration.RepositoryManager.ActiveRepositories)
         {
            DateTime now = DateTime.UtcNow;
            TimeSpan elapsed = now - repo.LastRefreshed;
            bool old = force | (elapsed >= TimeSpan.FromHours(repo.RefreshIntervalInHours));

            console.Write("repository: ");
            console.Write(ConsoleColor.Yellow, repo.Tag);
            console.Write(", age: ");
            console.WriteLine(old ? ConsoleColor.Red : ConsoleColor.Green, elapsed.ToString());

            if(old)
            {
               var snapshot = GetSnapshot(repo);

               //write to db even if no changes detected, the call will update timings
               PlaySnapshot(repo, snapshot);
            }
         }
      }
   }
}
