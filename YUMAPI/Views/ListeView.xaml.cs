using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YUMAPI.Controllers;
using YUMAPI.Models;

namespace YUMAPI.Views
{
    public partial class ListeView : UserControl
    {
        private MealController _controller = new MealController();

        public delegate void RecetteCliqueeHandler(string id);
        public event RecetteCliqueeHandler RecetteCliquee;

        public ListeView()
        {
            InitializeComponent();

            Loaded += async (s, e) =>
            {
                await _controller.RechercherAsync("chicken");
                ListeRecettes.ItemsSource = _controller.ListeRecettes;
            };
        }

        // ── Recherche en temps réel ────────────────────────────────────────
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string motCle = SearchBox.Text;

            if (string.IsNullOrEmpty(motCle))
            {
                TxtSectionTitle.Text = "FEATURED COLLECTION";
                return;
            }

            await Task.Delay(400);

            if (motCle != SearchBox.Text)
                return;

            TxtSectionTitle.Text = "RECHERCHE...";
            await _controller.RechercherAsync(motCle);
            ListeRecettes.ItemsSource = _controller.ListeRecettes;

            int nombre = _controller.ListeRecettes?.Count ?? 0;
            TxtSectionTitle.Text = nombre > 0 ? $"RÉSULTATS : {nombre}" : "AUCUN RÉSULTAT";
        }

        // ── Clic bouton → ─────────────────────────────────────────────────
        private async void BtnRechercher_Click(object sender, RoutedEventArgs e)
        {
            string motCle = SearchBox.Text;
            if (string.IsNullOrEmpty(motCle)) return;

            await _controller.RechercherAsync(motCle);
            ListeRecettes.ItemsSource = _controller.ListeRecettes;
            TxtSectionTitle.Text = $"RÉSULTATS POUR \"{motCle.ToUpper()}\"";
        }

        // ── Touche Entrée ─────────────────────────────────────────────────
        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                BtnRechercher_Click(sender, null);
        }

        // ── Clic ❤ sur une recette ────────────────────────────────────────
        private void BtnCoeur_Click(object sender, MouseButtonEventArgs e)
        {
            // Le sender est le Border orange sur lequel on a cliqué
            Border bouton = sender as Border;
            // Le Tag contient la recette (Tag="{Binding}" dans le XAML)
            MealListItem recette = bouton.Tag as MealListItem;

            if (recette == null) return;

            // On ajoute ou retire des favoris (et ça sauvegarde automatiquement)
            FavorisManager.BasculerFavori(recette);

            // On cherche le TextBlock ❤ à l'intérieur du Border pour changer l'emoji
            TextBlock coeur = bouton.Child as TextBlock;
            coeur.Text = FavorisManager.EstFavori(recette.Id) ? "❤️" : "🤍";

            // Empêche le clic de sélectionner la recette dans la liste
            e.Handled = true;
        }

        // ── Clic ❤ en haut : bascule affichage favoris ───────────────────
        private void BtnFavorisHaut_Click(object sender, MouseButtonEventArgs e)
        {
            if (TxtSectionTitle.Text == "❤️ MES FAVORIS")
            {
                // On revient aux recettes normales
                TxtSectionTitle.Text = "FEATURED COLLECTION";
                ListeRecettes.ItemsSource = _controller.ListeRecettes;
                BtnFavorisHaut.Text = "🤍";
                BordureFavorisHaut.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B35"));
            }
            else
            {
                // On affiche les favoris
                TxtSectionTitle.Text = "❤️ MES FAVORIS";
                ListeRecettes.ItemsSource = FavorisManager.Favoris;
                BtnFavorisHaut.Text = "❤️";
                // On passe le fond en rouge foncé pour montrer qu'on est en mode favoris
                BordureFavorisHaut.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC0000"));
            }
        }

        // ── Clic bouton "Toutes les recettes" ────────────────────────────
        private async void BtnToutesLesRecettes_Click(object sender, RoutedEventArgs e)
        {
            BtnToutesLesRecettes.IsEnabled = false;
            TxtSectionTitle.Text = "CHARGEMENT...";

            var toutesLesRecettes = new List<MealListItem>();
            string alphabet = "abcdefghijklmnopqrstuvwxyz";

            foreach (char lettre in alphabet)
            {
                await _controller.RechercherAsync(lettre.ToString());
                if (_controller.ListeRecettes != null)
                    toutesLesRecettes.AddRange(_controller.ListeRecettes);
            }

            var sansDoublons = toutesLesRecettes
                .GroupBy(r => r.Id)
                .Select(g => g.First())
                .OrderBy(r => r.Title)
                .ToList();

            ListeRecettes.ItemsSource = sansDoublons;
            TxtSectionTitle.Text = $"ALL RECIPES ({sansDoublons.Count})";
            BtnToutesLesRecettes.IsEnabled = true;
        }

        // ── Clic sur une recette → ouvre le détail ───────────────────────
        private void ListeRecettes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MealListItem recette = ListeRecettes.SelectedItem as MealListItem;
            if (recette == null) return;

            if (RecetteCliquee != null)
                RecetteCliquee(recette.Id);
        }
    }
}