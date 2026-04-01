// ============================================================
//  Views/ListeView.xaml.cs
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

        // Dictionnaire de traduction FR/ES → EN
        private static readonly Dictionary<string, string> _traductions = new Dictionary<string, string>
        {
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

        // Nouvel événement : clic sur l'avatar → ouvrir le profil
        public delegate void OuvrirProfilHandler();
        public event OuvrirProfilHandler OuvrirProfil;

        public ListeView()
        {
            InitializeComponent();

            // Mettre à jour les cœurs quand l'utilisateur fait défiler la liste
            ListeRecettes.Loaded += (s2, e2) =>
            {
                ScrollViewer sv = TrouverScrollViewer(ListeRecettes);
                if (sv != null)
                    sv.ScrollChanged += (s3, e3) => MettreAJourCoeurs();
            };

            Loaded += async (s, e) =>
            {
                if (UserController.UtilisateurConnecte != null)
                {
                    string u = UserController.UtilisateurConnecte.Username;
                    TxtUsername.Text = u;
                    TxtAvatar.Text = u.Length > 0 ? u[0].ToString().ToUpper() : "?";
                }

                AppliquerLangue();

                // S'abonner au changement de couleur pour mettre à jour cette vue
                ThemeManager.CouleurChangee += AppliquerCouleurTheme;
                AppliquerCouleurTheme();

                await ChargerRecettes("chicken");
                _ = ChargerBaseCompleteAsync();
            };

            // Se désabonner quand la vue est déchargée (évite les fuites mémoire)
            Unloaded += (s, e) =>
            {
                ThemeManager.CouleurChangee -= AppliquerCouleurTheme;
            };
        }

        // ════════════════════════════════════════════════════════════
        //  MISE À JOUR DE LA COULEUR THÈME
        // ════════════════════════════════════════════════════════════
        private void AppliquerCouleurTheme()
        {
            SolidColorBrush brosse = ThemeManager.CouleurAccent;

            // Bouton favoris en haut à droite
            BordureFavorisHaut.Background = brosse;

            // Bouton de recherche
            BtnRechercher.Background = brosse;

            // Réappliquer la logique de langue / état visuel
            AppliquerLangue();
        }

        // ════════════════════════════════════════════════════════════
        //  TRADUCTION MOT CLÉ FR → EN
        // ════════════════════════════════════════════════════════════
        private string TraduireMotCle(string motCle)
        {
            string cle = motCle.Trim().ToLower();
            if (_traductions.ContainsKey(cle))
                return _traductions[cle];
            return motCle;
        }

        // ════════════════════════════════════════════════════════════
        //  TRADUCTION DE L'INTERFACE
        // ════════════════════════════════════════════════════════════
        private void AppliquerLangue()
        {
            string l = TraductionService.LangueActuelle;

            TxtMembre.Text = l == "en" ? "Member" : l == "es" ? "Miembro" : "Membre";

            TxtSectionTitle.Text = l == "en" ? "FEATURED COLLECTION"
                                 : l == "es" ? "COLECCIÓN DESTACADA"
                                 : "COLLECTION VEDETTE";

            SolidColorBrush accent = ThemeManager.CouleurAccent;
            SolidColorBrush transparent = new SolidColorBrush(Colors.Transparent);
            SolidColorBrush blanc = new SolidColorBrush(Colors.White);
            SolidColorBrush gris = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888888"));

            BtnLangEN.Background = l == "en" ? accent : transparent;
            BtnLangFR.Background = l == "fr" ? accent : transparent;
            BtnLangES.Background = l == "es" ? accent : transparent;

            TxtLangEN.Foreground = l == "en" ? blanc : gris;
            TxtLangFR.Foreground = l == "fr" ? blanc : gris;
            TxtLangES.Foreground = l == "es" ? blanc : gris;

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

        private static readonly HashSet<string> _paysConnus = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "American","British","Canadian","Chinese","Croatian","Dutch","Egyptian",
            "Filipino","French","Greek","Indian","Irish","Italian","Jamaican","Japanese",
            "Kenyan","Malaysian","Mexican","Moroccan","Polish","Portuguese","Russian",
            "Spanish","Thai","Tunisian","Turkish","Ukrainian","Algerian","Lebanese","Vietnamese"
        };

        // ════════════════════════════════════════════════════════════
        //  RECHERCHE API
        // ════════════════════════════════════════════════════════════
        private async Task ChargerRecettes(string motCle)
        {
            string motCleEN = TraduireMotCle(motCle);

            if (_paysConnus.Contains(motCleEN))
            {
                await _controller.RechercherAsync(motCleEN);
                _recettesActuelles = _controller.ListeRecettes ?? new List<MealListItem>();
            }
            else if (_baseChargee && _toutesLesRecettes.Any())
            {
                // D'abord StartsWith (résultats les plus pertinents en tête)
                _recettesActuelles = _toutesLesRecettes
                    .Where(r => r.Title.ToLower().StartsWith(motCleEN.ToLower())
                             || r.Title.ToLower().StartsWith(motCle.ToLower()))
                    .ToList();

                // Ensuite Contains si rien trouvé
                if (!_recettesActuelles.Any())
                {
                    _recettesActuelles = _toutesLesRecettes
                        .Where(r => r.Title.ToLower().Contains(motCleEN.ToLower())
                                 || r.Title.ToLower().Contains(motCle.ToLower()))
                        .ToList();
                }

                // Fallback API si toujours rien
                // (la base se charge lettre par lettre, un mot peut ne pas encore être indexé)
                if (!_recettesActuelles.Any())
                {
                    await _controller.RechercherAsync(motCleEN);
                    _recettesActuelles = _controller.ListeRecettes ?? new List<MealListItem>();
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

            await Task.Delay(500);
            if (motCle != SearchBox.Text) return;

            string l3 = TraductionService.LangueActuelle;
            TxtSectionTitle.Text = l3 == "es" ? "BUSCANDO..." : l3 == "fr" ? "RECHERCHE..." : "SEARCHING...";
            await ChargerRecettes(motCle);
            int n = _recettesActuelles?.Count ?? 0;
            string labelAucun = l3 == "es" ? "SIN RESULTADOS" : l3 == "fr" ? "AUCUN RÉSULTAT" : "NO RESULTS";
            TxtSectionTitle.Text = n > 0 ? $"RÉSULTATS : {n}" : labelAucun;
        }

        private void AfficherSuggestions(List<MealListItem> suggestions, string motCle)
        {
            ListeAutoComplete.Items.Clear();

            foreach (MealListItem recette in suggestions)
            {
                StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };

                TextBlock icone = new TextBlock
                {
                    Text = "🔍 ",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")),
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center
                };
                sp.Children.Add(icone);

                TextBlock txt = new TextBlock { FontSize = 13, VerticalAlignment = VerticalAlignment.Center };

                string titre = recette.Title;
                int longueur = motCle.Length;

                txt.Inlines.Add(new System.Windows.Documents.Run(titre.Substring(0, longueur))
                {
                    Foreground = ThemeManager.CouleurAccent,
                    FontWeight = FontWeights.Bold
                });

                if (longueur < titre.Length)
                {
                    txt.Inlines.Add(new System.Windows.Documents.Run(titre.Substring(longueur))
                    {
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC"))
                    });
                }

                sp.Children.Add(txt);

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

        private void ListeAutoComplete_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
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

            e.Handled = true;

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
            string l2 = TraductionService.LangueActuelle;
            string labelResultat2 = l2 == "es" ? "RESULTADOS PARA" : l2 == "fr" ? "RÉSULTATS POUR" : "RESULTS FOR";
            TxtSectionTitle.Text = $"{labelResultat2} \"{titre.ToUpper()}\"";
        }

        private void AutoComplete_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private async void BtnRechercher_Click(object sender, RoutedEventArgs e)
        {
            string motCle = SearchBox.Text;
            if (string.IsNullOrEmpty(motCle)) return;
            PanneauAutoComplete.Visibility = Visibility.Collapsed;
            await ChargerRecettes(motCle);
            // Texte traduit selon la langue active
            string l1 = TraductionService.LangueActuelle;
            string labelResultat1 = l1 == "es" ? "RESULTADOS PARA" : l1 == "fr" ? "RÉSULTATS POUR" : "RESULTS FOR";
            TxtSectionTitle.Text = $"{labelResultat1} \"{motCle.ToUpper()}\"";
        }

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
                BordureFavorisHaut.Background = ThemeManager.CouleurAccent;
            }
            else { AfficherFavoris(); }
        }

        private void AfficherFavoris()
        {
            _afficheFavoris = true;
            List<MealListItem> favoris = FavorisManager.GetFavoris();
            string lf = TraductionService.LangueActuelle;
            TxtSectionTitle.Text = favoris.Count > 0
                ? (lf == "es" ? $"❤️ MIS FAVORITOS ({favoris.Count})"
                 : lf == "fr" ? $"❤️ MES FAVORIS ({favoris.Count})"
                 : $"❤️ MY FAVORITES ({favoris.Count})")
                : (lf == "es" ? "❤️ SIN FAVORITOS"
                 : lf == "fr" ? "❤️ AUCUN FAVORI"
                 : "❤️ NO FAVORITES");
            ListeRecettes.ItemsSource = null;
            ListeRecettes.ItemsSource = favoris;
            BtnFavorisHaut.Text = "❤️";
            // Assombrir la couleur du thème pour indiquer que les favoris sont actifs
            Color couleurBase = (Color)ColorConverter.ConvertFromString(ThemeManager.CouleurHex);
            Color couleurFoncee = Color.FromRgb(
                (byte)(couleurBase.R * 0.6),
                (byte)(couleurBase.G * 0.6),
                (byte)(couleurBase.B * 0.6));
            BordureFavorisHaut.Background = new SolidColorBrush(couleurFoncee);
        }

        // ════════════════════════════════════════════════════════════
        //  TOUTES LES RECETTES
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
        //  SÉLECTION + DÉCONNEXION + PROFIL
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

        // Clic sur l'avatar → ouvrir le profil
        private void BtnAvatar_Click(object sender, MouseButtonEventArgs e)
        {
            if (OuvrirProfil != null)
                OuvrirProfil();
        }

        // ════════════════════════════════════════════════════════════
        //  CHAT IA
        // ════════════════════════════════════════════════════════════
        // ── Parcourt le VisualTree pour trouver le ScrollViewer interne ──────
        private ScrollViewer TrouverScrollViewer(DependencyObject parent)
        {
            if (parent is ScrollViewer sv) return sv;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                ScrollViewer result = TrouverScrollViewer(VisualTreeHelper.GetChild(parent, i));
                if (result != null) return result;
            }
            return null;
        }

        // ── Met à jour les icônes ❤ des recettes visibles à l'écran ─────────
        private void MettreAJourCoeurs()
        {
            ListeRecettes.UpdateLayout();
            foreach (object item in ListeRecettes.Items)
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

        // ── Parcourt le VisualTree pour trouver le bouton cœur d'un item ─────
        private Border TrouverBoutonCoeur(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is Border b && b.Tag is MealListItem) return b;
                Border result = TrouverBoutonCoeur(child);
                if (result != null) return result;
            }
            return null;
        }

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