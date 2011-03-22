﻿using System.Xml.Serialization;

namespace NGem.Core.Model
{
   [XmlRoot("package")]
   public class PackageDependency
   {
      [XmlAttribute("id")]
      public string PackageId { get; set; }

      [XmlAttribute("version")]
      public string VersionPattern { get; set; }
   }
}
