﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pundit.Core.Model
{
   public class DependencyNode : ICloneable
   {
      private readonly DependencyNode _parentNode;
      private readonly string _packageId;
      private readonly string _platform;
      private readonly VersionPattern _versionPattern;
      private bool _hasVersions;   //if true _versions are populated from _versionPattern
      private Version[] _versions; //versions satisfying version pattern
      private int _activeVersionIndex = -1;  //active version index in _versions

      //node's dependencies
      private bool _hasManifest;
      private readonly List<DependencyNode> _children = new List<DependencyNode>();

      public DependencyNode(DependencyNode parentNode,
         string packageId, string platform, VersionPattern versionPattern)
      {
         _parentNode = parentNode;
         _packageId = packageId;
         _platform = platform;
         _versionPattern = versionPattern;
      }

      public string PackageId { get { return _packageId; } }
      public string Platform { get { return _platform; } }
      public VersionPattern VersionPattern { get { return _versionPattern; } }
      public IEnumerable<DependencyNode> Children { get { return _children; } }

      public string Path
      {
         get
         {
            var pp = new List<string>();

            var current = this;

            do
            {
               pp.Add(current.PackageId);
               current = current._parentNode;
            } while (current != null);

            pp.Reverse();
            return "/" + string.Join("/", pp);
         }
      }

      public void MarkAsRoot(Package rootManifest)
      {
         SetVersions(new[] {rootManifest.Version});
         SetManifest(rootManifest);
      }

      public void SetVersions(Version[] versions)
      {
         if (versions == null) throw new ArgumentNullException("versions");

         _versions = versions;
         Array.Sort(_versions);

         if(_versions.Length > 0) _activeVersionIndex = _versions.Length - 1;

         _hasVersions = true;

         _children.Clear();
         _hasManifest = false;
      }

      public void SetManifest(Package thisManifest)
      {
         if (thisManifest == null) throw new ArgumentNullException("thisManifest");

         _children.Clear();

         foreach(PackageDependency pd in thisManifest.Dependencies)
         {
            _children.Add(new DependencyNode(this, pd.PackageId, pd.Platform, new VersionPattern(pd.VersionPattern)));
         }

         _hasManifest = true;
      }

      public bool HasVersions
      {
         get { return _hasVersions; }
      }

      public bool HasManifest
      {
         get { return _hasManifest; }
      }

      public bool IsFull
      {
         get
         {
            foreach(DependencyNode child in _children)
            {
               if(!child.IsFull)
               {
                  return false;
               }
            }

            return _hasVersions && _hasManifest;
         }
      }

      public Version ActiveVersion
      {
         get
         {
            if (_activeVersionIndex == -1) return null;

            return _versions[_activeVersionIndex];
         }
      }

      public PackageKey ActiveVersionKey
      {
         get
         {
            if(ActiveVersion == null)
               throw new ArgumentException("node has no active version");

            return new PackageKey(_packageId, ActiveVersion, _platform);
         }
      }

      public bool CanDowngrade
      {
         get { return _activeVersionIndex > 0; }
      }

      public object Clone()
      {
         DependencyNode node = new DependencyNode(_parentNode, _packageId, _platform, _versionPattern);

         //versions

         node._hasVersions = _hasVersions;

         if(_versions != null)
         {
            node._versions = new Version[_versions.Length];

            if(_versions.Length > 0)
            {
               Array.Copy(_versions, node._versions, _versions.Length);
            }
         }

         node._activeVersionIndex = _activeVersionIndex;

         //manifest

         node._hasManifest = _hasManifest;

         foreach(DependencyNode child in _children)
         {
            node._children.Add((DependencyNode)child.Clone());
         }

         return node;
      }
   }
}