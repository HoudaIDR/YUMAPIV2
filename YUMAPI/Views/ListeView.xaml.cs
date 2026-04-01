// ============================================================
//  Views/ListeView.xaml.cs
//  + Autocomplétion (base complète en arrière-plan)
//  + Recherche IA étendue (filtre local)
// ============================================================

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

        private List<MealListItem> _toutesLesRecettes = new List<MealListItem>();
        private bool _baseChargee = false;
        private bool _ignorerTextChanged = false;

        // Dictionnaire de traduction FR/ES → EN pour la recherche
        private static readonly Dictionary<string, string> _traductions = new Dictionary<string, string>
        {
            // Pays / origines
            {"marocain","Moroccan"}, {"maroc","Moroccan"}, {"moroccan","Moroccan"},
            {"algerien","Algerian"}, {"algérien","Algerian"}, {"algerie","Algerian"}, {"algérie","Algerian"},
            {"tunisien","Tunisian"}, {"tunisie","Tunisian"},
            {"libanais","Lebanese"}, {"liban","Lebanese"},
            {"japonais","Japanese"}, {"japon","Japanese"},
            {"chinois","Chinese"}, {"chine","Chinese"},
            {"indien","Indian"}, {"inde","Indian"},
            {"italien","Italian"}, {"italie","Italian"},
            {"francais","French"}, {"français","French"}, {"france","French"},
            {"espagnol","Spanish"}, {"espagne","Spanish"},
            {"grec","Greek"}, {"grece","Greek"}, {"grèce","Greek"},
            {"mexicain","Mexican"}, {"mexique","Mexican"},
            {"americain","American"}, {"américain","American"},
            {"thai","Thai"}, {"thaï","Thai"}, {"thailande","Thai"}, {"thaïlande","Thai"},
            {"britannique","British"}, {"anglais","British"},
            {"canadien","Canadian"}, {"canada","Canadian"},
            // Ingrédients
            {"poulet","chicken"}, {"boeuf","beef"}, {"bœuf","beef"},
            {"agneau","lamb"}, {"porc","pork"}, {"poisson","fish"},
            {"crevettes","shrimp"}, {"fruits de mer","seafood"},
            {"pates","pasta"}, {"pâtes","pasta"},
            {"chocolat","chocolate"}, {"vanille","vanilla"},
            {"legumes","vegetable"}, {"légumes","vegetable"},
            {"riz","rice"}, {"soupe","soup"}, {"salade","salad"},
            {"dessert","dessert"}, {"gateau","cake"}, {"gâteau","cake"},
        };

        public delegate void RecetteCliqueeHandler(string id);
        public event RecetteCliqueeHandler RecetteCliquee;

        public delegate void DeconnexionHandler();
        public event DeconnexionHandler Deconnexion;

        public delegate void OuvrirProfilHandler();
        public event OuvrirProfilHandler OuvrirProfil;

        public ListeView()
        {
            InitializeComponent();

            // Mettre à jour les cœurs au chargement de la liste
            ListeRecettes.Loaded += (s2, e2) =>
            {
                // Accéder au ScrollViewer interne de la ListView
                var sv = FindScrollViewer(ListeRecettes);
                if (sv != null)
                    sv.ScrollChanged += (s3, e3) => MettreAJourCoeursInternal();
            };

            Loaded += async (s, e) =>
            {
                if (UserController.UtilisateurConnecte != null)
                {
                    string u = UserController.UtilisateurConnecte.Username;
                    TxtUsername.Text = u;
                    TxtAvatar.Text = u.Length > 0 ? u[0].ToString().ToUpper() : "?";
                }

                // Synchroniser toute l'interface avec la langue choisie au login
                AppliquerLangue();

                await ChargerRecettes("chicken");
                _ = ChargerBaseCompleteAsync();
            };
        }

        // ════════════════════════════════════════════════════════════
        //  TRADUCTION MOT CLÉ FR → EN
        // ════════════════════════════════════════════════════════════
        private string TraduireMotCle(string motCle)
        {
            string cle = motCle.Trim().ToLower();
            if (_traductions.ContainsKey(cle))
                return _traductions[cle];
            return motCle; // Pas de traduction trouvée → garder tel quel
        }

        // ════════════════════════════════════════════════════════════
        //  TRADUCTION DE L'INTERFACE
        // ════════════════════════════════════════════════════════════
        private void AppliquerLangue()
        {
            string l = TraductionService.LangueActuelle;

            // Membre
            TxtMembre.Text = l == "en" ? "Member" : l == "es" ? "Miembro" : "Membre";

            // Titre section
            TxtSectionTitle.Text = l == "en" ? "FEATURED COLLECTION"
                                 : l == "es" ? "COLECCIÓN DESTACADA"
                                 : "COLLECTION VEDETTE";

            // Boutons langue — actif = orange, inactif = transparent
            SolidColorBrush orange = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B35"));
            SolidColorBrush transparent = new SolidColorBrush(Colors.Transparent);
            SolidColorBrush blanc = new SolidColorBrush(Colors.White);
            SolidColorBrush gris = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888888"));

            BtnLangEN.Background = l == "en" ? orange : transparent;
            BtnLangFR.Background = l == "fr" ? orange : transparent;
            BtnLangES.Background = l == "es" ? orange : transparent;

            TxtLangEN.Foreground = l == "en" ? blanc : gris;
            TxtLangFR.Foreground = l == "fr" ? blanc : gris;
            TxtLangES.Foreground = l == "es" ? blanc : gris;

            // Bouton toutes les recettes
            TxtToutesRecettes.Text = l == "en" ? "All recipes"
                                   : l == "es" ? "Todas las recetas"
                                   : "Toutes les recettes";
        }

        // ════════════════════════════════════════════════════════════
        //  BASE COMPLÈTE EN ARRIÈRE-PLAN
        // ════════════════════════════════════════════════════════════
        private async Task ChargerBaseCompleteAsync()
        {
            var toutes = new List<MealListItem>();
            var ctrl = new MealController();

            foreach (char lettre in "abcdefghijklmnopqrstuvwxyz")
            {
                await ctrl.RechercherAsync(lettre.ToString());
                if (ctrl.ListeRecettes != null)
                    toutes.AddRange(ctrl.ListeRecettes);
            }

            _toutesLesRecettes = toutes
                .GroupBy(r => r.Id)
                .Select(g => g.First())
                .OrderBy(r => r.Title)
                .ToList();

            _baseChargee = true;
        }

        // ════════════════════════════════════════════════════════════
        //  RECHERCHE API
        // ════════════════════════════════════════════════════════════
        // Pays reconnus par TheMealDB
        private static readonly HashSet<string> _paysConnus = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "American","British","Canadian","Chinese","Croatian","Dutch","Egyptian",
            "Filipino","French","Greek","Indian","Irish","Italian","Jamaican","Japanese",
            "Kenyan","Malaysian","Mexican","Moroccan","Polish","Portuguese","Russian",
            "Spanish","Thai","Tunisian","Turkish","Ukrainian","Algerian","Lebanese","Vietnamese"
        };

        private async Task ChargerRecettes(string motCle)
        {
            // Traduire le mot clé si en français
            string motCleEN = TraduireMotCle(motCle);

            // Si c'est un pays → appel API direct (filter.php?a=)
            if (_paysConnus.Contains(motCleEN))
            {
                await _controller.RechercherAsync(motCleEN);
                _recettesActuelles = _controller.ListeRecettes ?? new List<MealListItem>();
            }
            // Si base chargée → filtrer localement par StartsWith
            else if (_baseChargee && _toutesLesRecettes.Any())
            {
                _recettesActuelles = _toutesLesRecettes
                    .Where(r => r.Title.ToLower().StartsWith(motCleEN.ToLower())
                             || r.Title.ToLower().StartsWith(motCle.ToLower()))
                    .ToList();

                // Si aucun résultat → essayer Contains
                if (!_recettesActuelles.Any())
                {
                    _recettesActuelles = _toutesLesRecettes
                        .Where(r => r.Title.ToLower().Contains(motCleEN.ToLower())
                                 || r.Title.ToLower().Contains(motCle.ToLower()))
                        .ToList();
                }
            }
            else
            {
                await _controller.RechercherAsync(motCleEN);
                _recettesActuelles = _controller.ListeRecettes ?? new List<MealListItem>();
            }

            _afficheFavoris = false;
            ListeRecettes.ItemsSource = null;
            ListeRecettes.ItemsSource = _recettesActuelles;
        }

        // ════════════════════════════════════════════════════════════
        //  AUTOCOMPLÉTION
        // ════════════════════════════════════════════════════════════
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_ignorerTextChanged) { _ignorerTextChanged = false; return; }
            string motCle = SearchBox.Text;

            if (string.IsNullOrEmpty(motCle))
            {
                string l = TraductionService.LangueActuelle;
                TxtSectionTitle.Text = l == "en" ? "FEATURED COLLECTION"
                                               : l == "es" ? "COLECCIÓN DESTACADA"
                                               : "COLLECTION VEDETTE";
                PanneauAutoComplete.Visibility = Visibility.Collapsed;
                return;
            }

            // Suggestions instantanées — le panneau reste ouvert jusqu'au clic ou Entrée
            if (_baseChargee && motCle.Length >= 1)
            {
                var suggestions = _toutesLesRecettes
                    .Where(r => r.Title.ToLower().StartsWith(motCle.ToLower()))
                    .Take(7)
                    .ToList();

                if (suggestions.Any())
                {
                    AfficherSuggestions(suggestions, motCle);
                    PanneauAutoComplete.Visibility = Visibility.Visible;
                }
                else
                {
                    PanneauAutoComplete.Visibility = Visibility.Collapsed;
                }
            }

            // Debounce — ON NE FERME PLUS LE PANNEAU ICI
            await Task.Delay(500);
            if (motCle != SearchBox.Text) return;

            TxtSectionTitle.Text = "RECHERCHE...";
            await ChargerRecettes(motCle);
            int n = _recettesActuelles?.Count ?? 0;
            TxtSectionTitle.Text = n > 0 ? $"RÉSULTATS : {n}" : "AUCUN RÉSULTAT";
            // Le panneau reste visible tant que l'utilisateur n'a pas cliqué ou appuyé Entrée
        }

        // ── Affiche les suggestions avec la partie tapée en orange ─────────
        private void AfficherSuggestions(List<MealListItem> suggestions, string motCle)
        {
            ListeAutoComplete.Items.Clear();

            foreach (MealListItem recette in suggestions)
            {
                // Créer un StackPanel avec icône + TextBlock coloré
                StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };

                TextBlock icone = new TextBlock
                {
                    Text = "🔍 ",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")),
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center
                };
                sp.Children.Add(icone);

                // TextBlock avec la partie tapée en orange et le reste en blanc
                TextBlock txt = new TextBlock { FontSize = 13, VerticalAlignment = VerticalAlignment.Center };

                string titre = recette.Title;
                int longueur = motCle.Length;

                // Partie tapée → orange + gras
                txt.Inlines.Add(new System.Windows.Documents.Run(titre.Substring(0, longueur))
                {
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B35")),
                    FontWeight = FontWeights.Bold
                });

                // Reste du titre → blanc normal
                if (longueur < titre.Length)
                {
                    txt.Inlines.Add(new System.Windows.Documents.Run(titre.Substring(longueur))
                    {
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC"))
                    });
                }

                sp.Children.Add(txt);

                // Wrapper ListViewItem avec le MealListItem en Tag
                ListViewItem lvi = new ListViewItem
                {
                    Content = sp,
                    Tag = recette,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(14, 9, 14, 9)
                };

                ListeAutoComplete.Items.Add(lvi);
            }
        }

        // ── Clic sur une suggestion (PreviewMouseDown pour éviter la perte de focus) ──
        private void ListeAutoComplete_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Trouver quel item a été cliqué via le Tag du ListViewItem
            var element = e.OriginalSource as FrameworkElement;
            MealListItem item = null;

            while (element != null)
            {
                if (element is ListViewItem lvi && lvi.Tag is MealListItem m)
                {
                    item = m;
                    break;
                }
                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
            }

            if (item == null) return;

            e.Handled = true; // Empêche la perte de focus du SearchBox

            _ignorerTextChanged = true;
            PanneauAutoComplete.Visibility = Visibility.Collapsed;
            SearchBox.Text = item.Title;
            SearchBox.CaretIndex = item.Title.Length;
            SearchBox.Focus();

            _ = ChargerEtAfficher(item.Title);
        }

        private async Task ChargerEtAfficher(string titre)
        {
            await ChargerRecettes(titre);
            TxtSectionTitle.Text = $"RÉSULTATS POUR \"{titre.ToUpper()}\"";
        }

        // Garder SelectionChanged vide pour éviter les conflits
        private void AutoComplete_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        // ── Bouton → ─────────────────────────────────────────────────────
        private async void BtnRechercher_Click(object sender, RoutedEventArgs e)
        {
            string motCle = SearchBox.Text;
            if (string.IsNullOrEmpty(motCle)) return;
            PanneauAutoComplete.Visibility = Visibility.Collapsed;
            await ChargerRecettes(motCle);
            TxtSectionTitle.Text = $"RÉSULTATS POUR \"{motCle.ToUpper()}\"";
        }

        // ── Touche Entrée / Escape ────────────────────────────────────────
        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PanneauAutoComplete.Visibility = Visibility.Collapsed;
                BtnRechercher_Click(sender, null);
            }
            else if (e.Key == Key.Escape)
            {
                PanneauAutoComplete.Visibility = Visibility.Collapsed;
            }
        }

        // ════════════════════════════════════════════════════════════
        //  FAVORIS
        // ════════════════════════════════════════════════════════════
        private void BtnCoeur_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Border bouton = sender as Border;
            MealListItem recette = bouton?.Tag as MealListItem;
            if (recette == null) return;
            FavorisManager.BasculerFavori(recette);
            TextBlock coeur = bouton.Child as TextBlock;
            if (coeur != null)
                coeur.Text = FavorisManager.EstFavori(recette.Id) ? "❤️" : "🤍";
            if (_afficheFavoris) AfficherFavoris();
        }

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
            else { AfficherFavoris(); }
        }

        private void AfficherFavoris()
        {
            _afficheFavoris = true;
            List<MealListItem> favoris = FavorisManager.GetFavoris();
            TxtSectionTitle.Text = favoris.Count > 0
                ? $"❤️ MES FAVORIS ({favoris.Count})"
                : "❤️ AUCUN FAVORI";

            // Désactiver la virtualisation pour les favoris
            ListeRecettes.SetValue(VirtualizingPanel.IsVirtualizingProperty, false);
            ListeRecettes.ItemsSource = null;
            ListeRecettes.ItemsSource = favoris;
            BtnFavorisHaut.Text = "❤️";
            BordureFavorisHaut.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#CC0000"));

            Dispatcher.InvokeAsync(async () => { await System.Threading.Tasks.Task.Delay(200); MettreAJourCoeursInternal(); });
        }

        // ── Met à jour tous les boutons ❤ selon les favoris ──────────────
        private System.Windows.Controls.ScrollViewer FindScrollViewer(System.Windows.DependencyObject d)
        {
            if (d is System.Windows.Controls.ScrollViewer sv) return sv;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(d); i++)
            {
                var result = FindScrollViewer(System.Windows.Media.VisualTreeHelper.GetChild(d, i));
                if (result != null) return result;
            }
            return null;
        }

        private void MettreAJourCoeurs()
        {
            MettreAJourCoeursInternal();
        }

        private void MettreAJourCoeursInternal()
        {
            ListeRecettes.UpdateLayout();
            foreach (var item in ListeRecettes.Items)
            {
                MealListItem recette = item as MealListItem;
                if (recette == null) continue;
                ListViewItem lvi = ListeRecettes.ItemContainerGenerator
                    .ContainerFromItem(item) as ListViewItem;
                if (lvi == null) continue;
                Border bouton = TrouverBoutonCoeur(lvi);
                if (bouton == null) continue;
                TextBlock coeur = bouton.Child as TextBlock;
                if (coeur != null)
                    coeur.Text = FavorisManager.EstFavori(recette.Id) ? "❤️" : "🤍";
            }
        }

        private Border TrouverBoutonCoeur(System.Windows.DependencyObject parent)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is Border b && b.Tag is MealListItem) return b;
                var result = TrouverBoutonCoeur(child);
                if (result != null) return result;
            }
            return null;
        }

        // ════════════════════════════════════════════════════════════
        //  TOUTES LES RECETTES (instantané si base déjà chargée)
        // ════════════════════════════════════════════════════════════
        private async void BtnToutesLesRecettes_Click(object sender, MouseButtonEventArgs e)
        {
            if (_baseChargee && _toutesLesRecettes.Any())
            {
                _recettesActuelles = _toutesLesRecettes;
                _afficheFavoris = false;
                ListeRecettes.ItemsSource = null;
                ListeRecettes.ItemsSource = _recettesActuelles;
                TxtSectionTitle.Text = $"ALL RECIPES ({_recettesActuelles.Count})";
                return;
            }

            BtnToutesLesRecettes.IsEnabled = false;
            TxtSectionTitle.Text = "CHARGEMENT...";
            await ChargerBaseCompleteAsync();
            _recettesActuelles = _toutesLesRecettes;
            _afficheFavoris = false;
            ListeRecettes.ItemsSource = null;
            ListeRecettes.ItemsSource = _recettesActuelles;
            TxtSectionTitle.Text = $"ALL RECIPES ({_recettesActuelles.Count})";
            BtnToutesLesRecettes.IsEnabled = true;
        }

        // ════════════════════════════════════════════════════════════
        //  LANGUE
        // ════════════════════════════════════════════════════════════
        private void BtnLangEN_Click(object sender, MouseButtonEventArgs e)
        {
            TraductionService.LangueActuelle = "en";
            AppliquerLangue();
        }

        private void BtnLangFR_Click(object sender, MouseButtonEventArgs e)
        {
            TraductionService.LangueActuelle = "fr";
            AppliquerLangue();
        }

        private void BtnLangES_Click(object sender, MouseButtonEventArgs e)
        {
            TraductionService.LangueActuelle = "es";
            AppliquerLangue();
        }

        // ════════════════════════════════════════════════════════════
        //  SÉLECTION + DÉCONNEXION
        // ════════════════════════════════════════════════════════════
        private void ListeRecettes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MealListItem recette = ListeRecettes.SelectedItem as MealListItem;
            if (recette == null) return;
            if (RecetteCliquee != null) RecetteCliquee(recette.Id);
        }

        private void BtnDeconnecter_Click(object sender, MouseButtonEventArgs e)
        {
            UserController.SeDeconnecter();
            if (Deconnexion != null) Deconnexion();
        }

        private void BtnAvatar_Click(object sender, MouseButtonEventArgs e)
        {
            if (OuvrirProfil != null)
                OuvrirProfil();
        }

        // ════════════════════════════════════════════════════════════
        //  CHAT IA → filtre local pour + de résultats
        // ════════════════════════════════════════════════════════════
        public async void LancerRecherche(string motCle)
        {
            SearchBox.Text = motCle;
            PanneauAutoComplete.Visibility = Visibility.Collapsed;

            if (_baseChargee && _toutesLesRecettes.Any())
            {
                _recettesActuelles = _toutesLesRecettes
                    .Where(r => r.Title.ToLower().StartsWith(motCle.ToLower()))
                    .ToList();

                _afficheFavoris = false;
                ListeRecettes.ItemsSource = null;
                ListeRecettes.ItemsSource = _recettesActuelles;
                TxtSectionTitle.Text = _recettesActuelles.Any()
                    ? $"RÉSULTATS : {_recettesActuelles.Count}"
                    : "AUCUN RÉSULTAT";
            }
            else
            {
                TxtSectionTitle.Text = $"RÉSULTATS POUR \"{motCle.ToUpper()}\"";
                await ChargerRecettes(motCle);
            }
        }
    }
}