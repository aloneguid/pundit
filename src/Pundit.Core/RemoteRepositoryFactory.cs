﻿using System;
using Pundit.Core.Model;

namespace Pundit.Core
{
   /// <summary>
   /// 
   /// </summary>
   public static class RemoteRepositoryFactory
   {
      private const string HttpRepoPrefix = "http://";
      private const string DirRepoPrefix = "dir://";

      /// <summary>
      /// 
      /// </summary>
      /// <param name="uri"></param>
      /// <param name="login"></param>
      /// <param name="apiKey"></param>
      /// <returns></returns>
      /// <exception cref="ArgumentNullException"></exception>
      /// <exception cref="NotSupportedException"></exception>
      public static IRemoteRepository Create(string uri, string login, string apiKey)
      {
         if (uri == null) throw new ArgumentNullException("uri");

         /*if(uri.StartsWith(DirRepoPrefix))
            return new RemoteFolderRepository(uri.Substring(DirRepoPrefix.Length));

         if(uri.StartsWith(HttpRepoPrefix))
            return new HttpRestRemoteRepository(uri, login, apiKey);*/

         throw new NotSupportedException("repository " + uri + " not supported (typo?)");
      }
   }
}
