﻿using System.Windows.Forms;
using Pundit.Core;

namespace Pundit.WinForms.Core
{
   public static class Alert
   {
      private static readonly string DefaultAlertTitle;

      static Alert()
      {
         DefaultAlertTitle = "Pundit v" + typeof (LocalConfiguration).Assembly.GetName().Version;
      }

      public static void Message(string msg)
      {
         MessageBox.Show(msg, DefaultAlertTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
      }

      public static DialogResult AskYesNo(string msg)
      {
         return MessageBox.Show(msg, DefaultAlertTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
      }

      public static void Error(string msg)
      {
         MessageBox.Show(msg, DefaultAlertTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
   }
}
