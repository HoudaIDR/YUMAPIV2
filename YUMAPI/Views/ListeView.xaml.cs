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
    /// <summary>
    /// Vue qui affiche la liste des recettes.
    /// Gère la recherche, les favoris et la sélection d’une recette.
    /// </summary>
    public partial class ListeView : UserControl
    {
        // Contrôleur qui communique avec l’API et récupère les recettes
        private MealController _controller = new MealController();

        // Délégué utilisé pour notifier qu’une recette a été cliquée
        public delegate void RecetteCliqueeHandler(string id);

        // Événement déclenché lorsqu’on sélectionne une recette
        public event RecetteCliqueeHandler RecetteCliquee;

        /// <summary>
        /// Constructeur : initialise la vue et charge des recettes par défaut.
        /// </summary>
        public ListeView()
        {
            InitializeComponent();

            // Au chargement du contrôle
            Loaded += async (s, e) =>
            {
                // Recherche initiale par défaut
                await _controller.RechercherAsync("chicken");

                // Liaison des données à la liste
                ListeRecettes.ItemsSource = _controller.ListeRecettes;
            };
        }

        // ─────────────────────────────────────────────────────────────
        // 🔎 Recherche en temps réel (quand on tape dans la barre)
        // ─────────────────────────────────────────────────────────────
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string motCle = SearchBox.Text;

            // Si champ vide → retour à l’état initial
            if (string.IsNullOrEmpty(motCle))
            {
                TxtSectionTitle.Text = "FEATURED COLLECTION";
                return;
            }

            // Petit délai pour éviter trop d’appels API (anti-spam)
            await Task.Delay(400);

            // Si le texte a changé pendant le délai → on annule
            if (motCle != SearchBox.Text) return;

            TxtSectionTitle.Text = "RECHERCHE...";

            // Appel API
            await _controller.RechercherAsync(motCle);

            // Mise à jour de la liste
            ListeRecettes.ItemsSource = _controller.ListeRecettes;

            // Affichage du nombre de résultats
            int nombre = _controller.ListeRecettes?.Count ?? 0;
            TxtSectionTitle.Text = nombre > 0
                ? $"RÉSULTATS : {nombre}"
                : "AUCUN RÉSULTAT";
        }

        // ─────────────────────────────────────────────────────────────
        // 🔍 Bouton Rechercher (clic manuel)
        // ─────────────────────────────────────────────────────────────
        private async void BtnRechercher_Click(object sender, RoutedEventArgs e)
        {
            string motCle = SearchBox.Text;

            if (string.IsNullOrEmpty(motCle)) return;

            // Recherche via API
            await _controller.RechercherAsync(motCle);

            // Mise à jour affichage
            ListeRecettes.ItemsSource = _controller.ListeRecettes;
            TxtSectionTitle.Text = $"RÉSULTATS POUR \"{motCle.ToUpper()}\"";
        }

        // ─────────────────────────────────────────────────────────────
        // ⌨️ Touche Entrée = lance la recherche
        // ─────────────────────────────────────────────────────────────
        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                BtnRechercher_Click(sender, null);
        }

        // ─────────────────────────────────────────────────────────────
        // ❤️ Clic sur le cœur d’une recette
        // ─────────────────────────────────────────────────────────────
        private void BtnCoeur_Click(object sender, MouseButtonEventArgs e)
        {
            // Récupère le bouton cliqué
            Border bouton = sender as Border;

            // Récupère la recette associée (stockée dans Tag)
            MealListItem recette = bouton.Tag as MealListItem;
            if (recette == null) return;

            // Ajoute ou retire des favoris
            FavorisManager.BasculerFavori(recette);

            // Met à jour l’icône cœur
            TextBlock coeur = bouton.Child as TextBlock;
            coeur.Text = FavorisManager.EstFavori(recette.Id)
                ? "❤️"
                : "🤍";

            // Empêche le clic de déclencher la sélection de la recette
            e.Handled = true;
        }

        // ─────────────────────────────────────────────────────────────
        // ❤️ Bouton favoris en haut : affiche/masque les favoris
        // ─────────────────────────────────────────────────────────────
        private void BtnFavorisHaut_Click(object sender, MouseButtonEventArgs e)
        {
            // Si on est déjà en mode favoris → revenir à la liste normale
            if (TxtSectionTitle.Text == "❤️ MES FAVORIS")
            {
                TxtSectionTitle.Text = "FEATURED COLLECTION";
                ListeRecettes.ItemsSource = _controller.ListeRecettes;

                BtnFavorisHaut.Text = "🤍";

                // Couleur orange par défaut
                BordureFavorisHaut.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FF6B35")
                );
            }
            else
            {
                // Sinon → afficher uniquement les favoris
                TxtSectionTitle.Text = "❤️ MES FAVORIS";
                ListeRecettes.ItemsSource = FavorisManager.Favoris;

                BtnFavorisHaut.Text = "❤️";

                // Couleur rouge pour indiquer actif
                BordureFavorisHaut.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#CC0000")
                );
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 📚 Charger toutes les recettes (A → Z)
        // ─────────────────────────────────────────────────────────────
        private async void BtnToutesLesRecettes_Click(object sender, RoutedEventArgs e)
        {
            BtnToutesLesRecettes.IsEnabled = false;
            TxtSectionTitle.Text = "CHARGEMENT...";

            var toutesLesRecettes = new List<MealListItem>();
            string alphabet = "abcdefghijklmnopqrstuvwxyz";

            // Recherche pour chaque lettre
            foreach (char lettre in alphabet)
            {
                await _controller.RechercherAsync(lettre.ToString());

                if (_controller.ListeRecettes != null)
                    toutesLesRecettes.AddRange(_controller.ListeRecettes);
            }

            // Suppression des doublons (même ID)
            var sansDoublons = toutesLesRecettes
                .GroupBy(r => r.Id)
                .Select(g => g.First())
                .OrderBy(r => r.Title)
                .ToList();

            // Mise à jour affichage
            ListeRecettes.ItemsSource = sansDoublons;
            TxtSectionTitle.Text = $"ALL RECIPES ({sansDoublons.Count})";

            BtnToutesLesRecettes.IsEnabled = true;
        }

        // ─────────────────────────────────────────────────────────────
        // 📖 Sélection d’une recette → ouvre le détail
        // ─────────────────────────────────────────────────────────────
        private void ListeRecettes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MealListItem recette = ListeRecettes.SelectedItem as MealListItem;
            if (recette == null) return;

            // Déclenche l’événement pour afficher la vue détail
            RecetteCliquee?.Invoke(recette.Id);
        }
    }
}