﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Pundit.Core.Model;
using Pundit.WinForms.Core;
using Package = Microsoft.VisualStudio.Shell.Package;

namespace Pundit.Vsix
{
	public partial class PunditPackage : Package
	{
      private DirectoryInfo GetPropAsDir(__VSPROPID prop)
      {
         IVsSolution solution = GetService(typeof(SVsSolution)) as IVsSolution;

         if (solution != null)
         {
            object objSolutionDir;

            solution.GetProperty((int)prop, out objSolutionDir);

            if (objSolutionDir != null)
            {
               string solutionDir = objSolutionDir.ToString();

               return new DirectoryInfo(solutionDir);
            }
         }

         return null;
      }

      private DirectoryInfo SolutionDirectory
      {
         get { return GetPropAsDir(__VSPROPID.VSPROPID_SolutionDirectory); }
      }

	   private DirectoryInfo ManifestDirectory
	   {
	      get
	      {
	         DirectoryInfo solutionDir = SolutionDirectory;

            if (solutionDir != null && solutionDir.Parent != null)
            {
               return solutionDir.Parent;
            }

	         return null;
	      }
	   }

	   private string ManifestPath
	   {
	      get
	      {
	         DirectoryInfo md = ManifestDirectory;

            if(md != null)
            {
               string manifestPath = Path.Combine(
                  md.FullName,
                  Pundit.Core.Model.Package.DefaultManifestFileName);

               return manifestPath;
            }

            return null;
	      }
	   }

	   private bool SolutionHasManifest
	   {
	      get
         {
            string path = ManifestPath;

            return path != null && File.Exists(path);
         }
	   }

	   private DevPackage InstantManifest
	   {
	      get
	      {
	         using(Stream s = File.OpenRead(ManifestPath))
	         {
	            return DevPackage.FromStream(s);
	         }
	      }
	   }

      private void Test()
      {
         IVsProject proj = GetService(typeof (IVsProject)) as IVsProject;

         //proj.AddItem()
      }

	   private bool IsInValidState
	   {
	      get
	      {
            if (SolutionDirectory == null)
            {
               Alert.Error(Strings.SolutionNotAccessible);

               return false;
            }

            if(!SolutionHasManifest)
            {
               Alert.Error(string.Format(Strings.SolutionHasNoManifest, ManifestPath));

               return false;
            }

	         return true;
	      }
	   }

	   private const string SettingsRoot = "Pundit\\Common";

      private WritableSettingsStore GetWritableSettingsStore(string settingsRoot)
      {
         SettingsManager settingsManager = new ShellSettingsManager(this);
         WritableSettingsStore userSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
         if (!userSettingsStore.CollectionExists(settingsRoot))
         {
            userSettingsStore.CreateCollection(settingsRoot);
         }
         return userSettingsStore;
      }

      private void SaveSetting(string key, string value)
      {
         WritableSettingsStore store = GetWritableSettingsStore(SettingsRoot);
         store.SetString(SettingsRoot, key, value);
      }

      private string ReadSetting(string key)
      {
         WritableSettingsStore store = GetWritableSettingsStore(SettingsRoot);
         return store.GetString(SettingsRoot, key);
      }
	}
}
