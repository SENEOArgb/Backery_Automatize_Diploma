using App_Automatize_Backery.Models;
using App_Automatize_Backery.ViewModels.RecipesVM;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace App_Automatize_Backery.View.Windows.Recipes
{
    /// <summary>
    /// Логика взаимодействия для RecipeIngridientCreateEditWindow.xaml
    /// </summary>
    public partial class RecipeIngridientCreateEditWindow : Window
    {
        public RecipeIngridientCreateEditWindow(RawMaterialMeasurementUnitRecipe ingredient)
        {
            InitializeComponent();

            var vm = new RMMeasurementUnitRecipeCreateEditViewModel(ingredient)
            {
                Window = this
            };

            DataContext = vm;
        }
    }
}
