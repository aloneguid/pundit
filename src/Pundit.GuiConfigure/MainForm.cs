﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Pundit.GuiConfigure
{
   public partial class MainForm : Form
   {
      public MainForm()
      {
         InitializeComponent();
      }

      private void cmdAddDependency_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         SearchForm searchForm = new SearchForm();

         if(DialogResult.OK == searchForm.ShowDialog(this))
         {
            //...
         }
      }
   }
}