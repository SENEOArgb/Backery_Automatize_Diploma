﻿using App_Automatize_Backery.Models;
using App_Automatize_Backery.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace App_Automatize_Backery.View.UserControls_Pages_
{
    /// <summary>
    /// Логика взаимодействия для UserUC.xaml
    /// </summary>
    public partial class UserUC : UserControl
    {
        public UserUC(User user)
        {
            InitializeComponent();
        }
    }
}
