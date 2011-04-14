﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Pundit.Core.Model;

namespace Pundit.Test
{
   [TestFixture]
   public class ModelTest
   {
      [Test]
      public void EqualityTest()
      {
         var pkg1 = new UnresolvedPackage("p1", null);
         var pkg2 = new UnresolvedPackage("p2", null);

         Assert.IsFalse(pkg1.Equals(pkg2));
         Assert.IsTrue(pkg1.Equals(pkg1));

         var dic = new Dictionary<UnresolvedPackage, bool>();
         dic[pkg1] = true;

         var pkg11 = new UnresolvedPackage("p1", null);
         Assert.IsTrue(dic.ContainsKey(pkg11));
      }
   }
}
