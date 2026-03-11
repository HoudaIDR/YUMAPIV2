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
        private List<MealListItem> _recettesActuelles = new List<MealListItem>();
        private bool _afficheFavoris = false;

        public delegate void RecetteCliqueeHandler(string id);
        public event RecetteCliqueeHandler RecetteCliquee;

        public delegate void DeconnexionHandler();
        public event DeconnexionHandler Deconnexion;

        public ListeView()
        {
            InitializeComponent();

            Loaded += async (s, e) =>
            {
                // Afficher le nom et l'initiale de l'utilisateur connecté
                if (UserController.UtilisateurConnecte != null)
                {
                    string u = UserController.UtilisateurConnecte.Username;
                    TxtUsername.Text = u;
                    TxtAvatar.Text = u.Length > 0 ? u[0].ToString().ToUpper() : "?";
                }

                await ChargerRecettes("chicken");
            };
        }

        // ── Charge des recettes et met à jour la liste ────────────────────
        private async Task ChargerRecettes(string motCle)
        {
            await _controller.RechercherAsync(motCle);
            _recettesActuelles = _controller.ListeRecettes ?? new List<MealListItem>();
            _afficheFavoris = false;
            ListeRecettes.ItemsSource = null;
            ListeRecettes.ItemsSource = _recettesActuelles;
        }

        // ── Recherche temps réel ──────────────────────────────────────────
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string motCle = SearchBox.Text;

            if (string.IsNullOrEmpty(motCle))
            {
                TxtSectionTitle.Text = "FEATURED COLLECTION";
                return;
            }

            await Task.Delay(400);
            if (motCle != SearchBox.Text) return;

            TxtSectionTitle.Text = "RECHERCHE...";
            await ChargerRecettes(motCle);

            int n = _recettesActuelles?.Count ?? 0;
            TxtSectionTitle.Text = n > 0 ? $"RÉSULTATS : {n}" : "AUCUN RÉSULTAT";
        }

        // ── Bouton → ─────────────────────────────────────────────────────
        private async void BtnRechercher_Click(object sender, RoutedEventArgs e)
        {
            string motCle = SearchBox.Text;
            if (string.IsNullOrEmpty(motCle)) return;

            await ChargerRecettes(motCle);
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
            e.Handled = true;

            Border bouton = sender as Border;
            MealListItem recette = bouton?.Tag as MealListItem;
            if (recette == null) return;

            FavorisManager.BasculerFavori(recette);

            // Mise à jour immédiate de l'emoji
            TextBlock coeur = bouton.Child as TextBlock;
            if (coeur != null)
                coeur.Text = FavorisManager.EstFavori(recette.Id) ? "❤️" : "🤍";

            // Si on est en mode favoris et qu'on retire → rafraîchir
            if (_afficheFavoris)
                AfficherFavoris();
        }

        // ── Bouton ❤ en haut : bascule favoris ───────────────────────────
        private void BtnFavorisHaut_Click(object sender, MouseButtonEventArgs e)
        {
            if (_afficheFavoris)
            {
                _afficheFavoris = false;
                TxtSectionTitle.Text = "FEATURED COLLECTION";
                ListeRecettes.ItemsSource = null;
                ListeRecettes.ItemsSource = _recettesActuelles;
                BtnFavorisHaut.Text = "🤍";
                BordureFavorisHaut.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FF6B35"));
            }
            else
            {
                AfficherFavoris();
            }
        }

        // ── Affiche les favoris de l'utilisateur connecté ─────────────────
        private void AfficherFavoris()
        {
            _afficheFavoris = true;
            List<MealListItem> favoris = FavorisManager.GetFavoris();

            TxtSectionTitle.Text = favoris.Count > 0
                ? $"❤️ MES FAVORIS ({favoris.Count})"
                : "❤️ AUCUN FAVORI";

            ListeRecettes.ItemsSource = null;
            ListeRecettes.ItemsSource = favoris;

            BtnFavorisHaut.Text = "❤️";
            BordureFavorisHaut.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#CC0000"));
        }

        // ── Toutes les recettes ───────────────────────────────────────────
        private async void BtnToutesLesRecettes_Click(object sender, RoutedEventArgs e)
        {
            BtnToutesLesRecettes.IsEnabled = false;
            TxtSectionTitle.Text = "CHARGEMENT...";

            var toutes = new List<MealListItem>();
            string abc = "abcdefghijklmnopqrstuvwxyz";

            foreach (char lettre in abc)
            {
                await _controller.RechercherAsync(lettre.ToString());
                if (_controller.ListeRecettes != null)
                    toutes.AddRange(_controller.ListeRecettes);
            }

            _recettesActuelles = toutes
                .GroupBy(r => r.Id)
                .Select(g => g.First())
                .OrderBy(r => r.Title)
                .ToList();

            _afficheFavoris = false;
            ListeRecettes.ItemsSource = null;
            ListeRecettes.ItemsSource = _recettesActuelles;
            TxtSectionTitle.Text = $"ALL RECIPES ({_recettesActuelles.Count})";
            BtnToutesLesRecettes.IsEnabled = true;
        }

        // ── Clic sur une recette → détail ─────────────────────────────────
        private void ListeRecettes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MealListItem recette = ListeRecettes.SelectedItem as MealListItem;
            if (recette == null) return;

            if (RecetteCliquee != null)
                RecetteCliquee(recette.Id);
        }

        // ── Déconnexion ───────────────────────────────────────────────────
        private void BtnDeconnecter_Click(object sender, MouseButtonEventArgs e)
        {
            UserController.SeDeconnecter();
            if (Deconnexion != null)
                Deconnexion();
        }
    }
}