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
using YUMAPI.Controllers;
using YUMAPI.Models;

namespace YUMAPI.Views
{
    public partial class ListeView : UserControl
    {
        private MealController _controller = new MealController();

        // Événement pour dire à MainWindow quelle recette a été cliquée
        public delegate void RecetteCliqueeHandler(string id);
        public event RecetteCliqueeHandler RecetteCliquee;

        public ListeView()
        {
            InitializeComponent();

            // Charger des recettes au démarrage
            Loaded += async (s, e) =>
            {
                await _controller.RechercherAsync("chicken");
                ListeRecettes.ItemsSource = _controller.ListeRecettes;
            };
        }

        // ── Touche Entrée dans la recherche ───────────────────────────────
        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                BtnRechercher_Click(sender, null);
        }

        // ── Clic bouton rechercher ─────────────────────────────────────────
        private async void BtnRechercher_Click(object sender, RoutedEventArgs e)
        {
            string motCle = SearchBox.Text;

            if (string.IsNullOrEmpty(motCle))
                return;

            await _controller.RechercherAsync(motCle);
            ListeRecettes.ItemsSource = _controller.ListeRecettes;
        }

        // ── Clic sur une recette → prévient MainWindow ─────────────────────
        private void ListeRecettes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MealListItem recette = ListeRecettes.SelectedItem as MealListItem;

            if (recette == null)
                return;

            // Envoyer l'ID à MainWindow (qui appellera DetailView)
            if (RecetteCliquee != null)
                RecetteCliquee(recette.Id);
        }
    }
}
