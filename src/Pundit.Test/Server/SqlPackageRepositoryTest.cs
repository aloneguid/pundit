﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Pundit.Core.Model;
using Pundit.Core.Server.Application;
using Pundit.Core.Server.Model;

namespace Pundit.Test.Server
{
   [TestFixture]
   public class SqlPackageRepositoryTest
   {
      public const string TestConnectionString = "Server=localhost;Database=pundit;Uid=pundit;Pwd=nopassword";

      private IPackageRepository _repo;

      [SetUp]
      public void SetUp()
      {
         _repo = new MySqlPackageRepository(TestConnectionString);
         _repo.DeletePackage(new PackageKey("fake", new Version("1.2.11.0"), "noarch"));
      }

      [TearDown]
      public void TearDown()
      {
         if(_repo != null)
         {
            _repo.Dispose();
         }
      }

      [Test]
      public void WriteReadPackageTest()
      {
         var p = new Package("fake", new Version("1.2.11"));
         p.Dependencies.Add(new PackageDependency("rhino", "1.2.3"));
         p.Dependencies.Add(new PackageDependency("mocks", "4.5.6"));
         long packageId = _repo.SavePackage(p, 123, true);
         Assert.Greater(packageId, 0);

         DbPackage p1 = _repo.GetPackage(packageId);
         DbPackage p2 = _repo.GetPackage(p.Key);
         Assert.IsNotNull(p1);
         Assert.IsNotNull(p2);
         Assert.AreEqual(p1, p2);
         Assert.AreEqual(2, p1.Package.Dependencies.Count);
         Assert.AreEqual(2, p2.Package.Dependencies.Count);
      }
   }
}
