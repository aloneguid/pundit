﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

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
	}
}
