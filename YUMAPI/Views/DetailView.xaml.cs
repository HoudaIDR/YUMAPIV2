// ============================================================
//  Views/DetailView.xaml.cs
// ============================================================

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using YUMAPI.Controllers;
using YUMAPI.Models;

namespace YUMAPI.Views
{
    public partial class DetailView : UserControl
    {
        private MealController _controller = new MealController();
        private MealListItem _recetteActuelle;
        private MealDto _recetteOriginale;
        private int _nbPersonnes = 4;
        private Storyboard _spinStory;
        private Storyboard _pulseStory;
        private List<string> _etapes = new List<string>();
        private int _etapeIndex = 0;

        public DetailView()
        {
            InitializeComponent();
            _spinStory = (Storyboard)Resources["SpinAnimation"];
            _pulseStory = (Storyboard)Resources["PulseAnimation"];

            Loaded += (s, e) =>
            {
                string l = TraductionService.LangueActuelle;
                TxtAccueilTitre.Text = l == "en" ? "Select a recipe"
                                     : l == "es" ? "Selecciona una receta"
                                     : "Sélectionnez une recette";
                TxtAccueilSous.Text = l == "en" ? "or search for a dish in the list"
                                     : l == "es" ? "o busca un plato en la lista"
                                     : "ou recherchez un plat dans la liste";

                // S'abonner au changement de couleur
                ThemeManager.CouleurChangee += AppliquerCouleur;
            };

            // Se désabonner quand la vue est déchargée
            Unloaded += (s, e) =>
            {
                ThemeManager.CouleurChangee -= AppliquerCouleur;
            };
        }

        public async void ChargerDetail(string id)
        {
            _nbPersonnes = 4;
            TxtPortions.Text = "4";
            bool traductionActive = TraductionService.LangueActuelle != "en";

            PanneauAccueil.Visibility = Visibility.Collapsed;
            PanneauDetail.Visibility = Visibility.Collapsed;
            PanneauChargement.Visibility = Visibility.Visible;
            BadgeCalories.Visibility = Visibility.Collapsed;
            PanneauGuide.Visibility = Visibility.Collapsed;
            PanneauInstructionsNormal.Visibility = Visibility.Visible;
            BoutonDemarrer.Visibility = Visibility.Visible;
            TxtBtnSuivant.Text = TraductionService.T("suivant");
            BtnSuivant.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B35"));

            LoadingText.Text = traductionActive ? TraductionService.T("traduction_en_cours") : TraductionService.T("chargement");
            LoadingSubText.Text = traductionActive ? TraductionService.T("ia_traduit") : TraductionService.T("recuperation");
            LoadingEmoji.Text = traductionActive ? "🌍" : "🍽️";

            _spinStory.Begin();
            _pulseStory.Begin();

            await _controller.ChargerDetailAsync(id);
            MealDto recette = _controller.DetailRecette;
            if (recette == null) { ArreterAnimations(); return; }

            _recetteOriginale = recette;
            _recetteActuelle = new MealListItem { Id = recette.idMeal, Title = recette.strMeal, Thumb = recette.strMealThumb };

            // Enregistrer la derniere recette consultee dans le profil
            UserController.EnregistrerDerniereRecette(_recetteActuelle);

            if (traductionActive)
                recette = await TraductionService.TraduireRecette(recette);

            ArreterAnimations();
            PanneauChargement.Visibility = Visibility.Collapsed;
            PanneauDetail.Visibility = Visibility.Visible;

            AppliquerCouleur();
            CoeurDetail.Text = FavorisManager.EstFavori(id) ? "❤️" : "🤍";
            DetailTitre.Text = recette.strMeal;
            TagCategorie.Text = recette.strCategory;
            TagPays.Text = recette.strArea;
            DetailInstructions.Text = recette.strInstructions;

            // Traduire les titres et boutons selon la langue
            TxtTitreIngredients.Text = TraductionService.T("ingredients");
            TxtTitreInstructions.Text = TraductionService.T("preparation");
            var btnTxt = ((System.Windows.Controls.StackPanel)BoutonDemarrer.Child).Children[1] as System.Windows.Controls.TextBlock;
            if (btnTxt != null) btnTxt.Text = TraductionService.T("demarrer_recette");

            if (!string.IsNullOrEmpty(recette.strMealThumb))
                DetailImage.Source = new BitmapImage(new Uri(recette.strMealThumb));

            AfficherIngredients(recette, null);
            _ = ChargerCaloriesAsync(_recetteOriginale);
        }

        private async System.Threading.Tasks.Task ChargerCaloriesAsync(MealDto recette)
        {
            string calories = await TraductionService.EstimerCalories(recette);
            if (!string.IsNullOrEmpty(calories))
            {
                TxtCalories.Text = $"{calories} / pers.";
                BadgeCalories.Visibility = Visibility.Visible;
            }
        }

        private async void BtnMoins_Click(object sender, MouseButtonEventArgs e)
        {
            if (_nbPersonnes <= 1 || _recetteOriginale == null) return;
            _nbPersonnes--;
            TxtPortions.Text = _nbPersonnes.ToString();
            await MettreAJourPortions();
        }

        private async void BtnPlus_Click(object sender, MouseButtonEventArgs e)
        {
            if (_nbPersonnes >= 12 || _recetteOriginale == null) return;
            _nbPersonnes++;
            TxtPortions.Text = _nbPersonnes.ToString();
            await MettreAJourPortions();
        }

        private async System.Threading.Tasks.Task MettreAJourPortions()
        {
            MealDto recette = TraductionService.LangueActuelle != "en"
                ? await TraductionService.TraduireRecette(_recetteOriginale)
                : _recetteOriginale;
            string[] mesures = await TraductionService.AjusterPortions(_recetteOriginale, _nbPersonnes);
            if (mesures == null) return;
            AfficherIngredients(recette, mesures);
        }

        private void AfficherIngredients(MealDto recette, string[] mesuresAjustees)
        {
            PanneauIngredients.Children.Clear();
            string[] ingredients = { recette.strIngredient1, recette.strIngredient2, recette.strIngredient3, recette.strIngredient4, recette.strIngredient5, recette.strIngredient6, recette.strIngredient7, recette.strIngredient8, recette.strIngredient9, recette.strIngredient10 };
            string[] mesuresOri = { recette.strMeasure1, recette.strMeasure2, recette.strMeasure3, recette.strMeasure4, recette.strMeasure5, recette.strMeasure6, recette.strMeasure7, recette.strMeasure8, recette.strMeasure9, recette.strMeasure10 };
            for (int i = 0; i < 10; i++)
            {
                if (string.IsNullOrWhiteSpace(ingredients[i])) continue;
                string mesure = (mesuresAjustees != null && i < mesuresAjustees.Length) ? mesuresAjustees[i] : mesuresOri[i];
                AjouterCarteIngredient(mesure, ingredients[i]);
            }
        }

        // Longueur max du texte dans le badge orange avant découpage
        private const int LONGUEUR_MAX_BADGE = 18;

        private void AjouterCarteIngredient(string mesure, string ingredient)
        {
            if (string.IsNullOrWhiteSpace(ingredient)) return;

            // Si la mesure est trop longue, on coupe proprement sur un espace
            // et on met le surplus en petit texte gris sous l'ingrédient
            string mesureBrute = string.IsNullOrWhiteSpace(mesure) ? "—" : mesure.Trim();
            string texteBadge;
            string mesureSurplus = "";

            if (mesureBrute.Length > LONGUEUR_MAX_BADGE)
            {
                int coupure = mesureBrute.LastIndexOf(' ', LONGUEUR_MAX_BADGE);
                if (coupure < 5) coupure = LONGUEUR_MAX_BADGE;
                texteBadge = mesureBrute.Substring(0, coupure).Trim();
                mesureSurplus = mesureBrute.Substring(coupure).Trim();
            }
            else
            {
                texteBadge = mesureBrute;
            }

            Border carte = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 8),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2A2A")),
                BorderThickness = new Thickness(1)
            };

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Badge orange — taille limitée pour éviter le débordement
            Border badge = new Border
            {
                Background = ThemeManager.CouleurAccent,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 10, 8),
                MaxWidth = 130,
                MinWidth = 50,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };

            badge.Child = new TextBlock
            {
                Text = texteBadge,
                Foreground = Brushes.White,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            Grid.SetColumn(badge, 0);
            g.Children.Add(badge);

            // Colonne droite : nom de l'ingrédient + surplus de mesure si besoin
            StackPanel colonneTexte = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0)
            };

            colonneTexte.Children.Add(new TextBlock
            {
                Text = ingredient.Trim(),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC")),
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap
            });

            if (!string.IsNullOrEmpty(mesureSurplus))
            {
                colonneTexte.Children.Add(new TextBlock
                {
                    Text = mesureSurplus,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888888")),
                    FontSize = 11,
                    FontStyle = FontStyles.Italic,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 2, 0, 0)
                });
            }

            Grid.SetColumn(colonneTexte, 1);
            g.Children.Add(colonneTexte);

            carte.Child = g;
            PanneauIngredients.Children.Add(carte);
        }

        // ════════════════════════════════════════════════════════════
        //  MODE GUIDÉ
        // ════════════════════════════════════════════════════════════
        private async void BtnDemarrerRecette_Click(object sender, MouseButtonEventArgs e)
        {
            if (_recetteOriginale == null) return;

            // Désactiver le bouton pendant le chargement
            BoutonDemarrer.IsEnabled = false;
            var txt = ((System.Windows.Controls.StackPanel)BoutonDemarrer.Child).Children[1] as System.Windows.Controls.TextBlock;
            if (txt != null) txt.Text = TraductionService.T("preparation_ia");

            // Demander à l'IA de découper les étapes
            _etapes = await TraductionService.DecouperEtapesIA(_recetteOriginale);

            // Fallback si l'IA échoue
            if (_etapes == null || _etapes.Count == 0)
                _etapes = DecouperEnEtapes(DetailInstructions.Text);

            BoutonDemarrer.IsEnabled = true;
            if (txt != null) txt.Text = TraductionService.T("demarrer_recette");

            if (_etapes.Count == 0) return;

            _etapeIndex = 0;
            PanneauInstructionsNormal.Visibility = Visibility.Collapsed;
            PanneauGuide.Visibility = Visibility.Visible;
            BoutonDemarrer.Visibility = Visibility.Collapsed;
            AfficherEtape(0);
        }

        private void BtnSuivantEtape_Click(object sender, MouseButtonEventArgs e)
        {
            if (_etapeIndex < _etapes.Count - 1)
            {
                _etapeIndex++;
                AfficherEtape(_etapeIndex);
            }
            else
            {
                TxtEtapeActuelle.Text = TraductionService.T("bonne_degustation");
                TxtNumeroEtape.Text = TraductionService.T("termine");
                TxtBtnSuivant.Text = "✓ " + TraductionService.T("termine");
                BtnSuivant.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                if (BarreEtapes.Parent is Border p) BarreEtapes.Width = p.ActualWidth;
            }
        }

        private void BtnPrecedentEtape_Click(object sender, MouseButtonEventArgs e)
        {
            if (_etapeIndex > 0) { _etapeIndex--; AfficherEtape(_etapeIndex); }
        }

        private void BtnQuitterGuide_Click(object sender, MouseButtonEventArgs e)
        {
            PanneauGuide.Visibility = Visibility.Collapsed;
            PanneauInstructionsNormal.Visibility = Visibility.Visible;
            BoutonDemarrer.Visibility = Visibility.Visible;
            TxtBtnSuivant.Text = TraductionService.T("suivant");
            BtnSuivant.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B35"));
        }

        private void AfficherEtape(int index)
        {
            if (index >= _etapes.Count) return;
            TxtEtapeActuelle.Text = _etapes[index];
            TxtNumeroEtape.Text = TraductionService.T("etape") + " " + (index + 1) + "/" + _etapes.Count;
            TxtBtnSuivant.Text = index == _etapes.Count - 1 ? TraductionService.T("terminer") : TraductionService.T("suivant");
            BtnSuivant.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B35"));
            BtnPrecedent.Opacity = index == 0 ? 0.4 : 1.0;
            if (BarreEtapes.Parent is Border parent)
                BarreEtapes.Width = parent.ActualWidth * (double)(index + 1) / _etapes.Count;
        }

        private List<string> DecouperEnEtapes(string instructions)
        {
            var etapes = new List<string>();
            if (string.IsNullOrEmpty(instructions)) return etapes;

            string[] lignes = instructions.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            string enCours = "";

            foreach (string ligne in lignes)
            {
                string l = ligne.Trim();
                if (string.IsNullOrEmpty(l)) continue;

                // Détecte les headers "step 1", "STEP 2", "1.", "1)" etc.
                bool debutEtape = Regex.IsMatch(l, @"^(step\s*\d+|étape\s*\d+|\d+[\.]|\d+\))", RegexOptions.IgnoreCase);

                if (debutEtape)
                {
                    // Sauvegarder l'étape précédente si elle existe
                    if (!string.IsNullOrEmpty(enCours))
                        etapes.Add(enCours.Trim());

                    // Retirer le header "step X" du texte → garder seulement ce qui suit
                    string sansHeader = Regex.Replace(l, @"^(step\s*\d+\s*:?|étape\s*\d+\s*:?|\d+[\.]\s*|\d+\)\s*)", "", RegexOptions.IgnoreCase).Trim();
                    enCours = sansHeader; // Commence la nouvelle étape SANS le header
                }
                else
                {
                    enCours += (string.IsNullOrEmpty(enCours) ? "" : "\n") + l;
                }
            }

            if (!string.IsNullOrEmpty(enCours))
                etapes.Add(enCours.Trim());

            // Fallback : découper par phrases si pas d'étapes détectées
            if (etapes.Count <= 1)
            {
                etapes.Clear();
                string[] phrases = instructions.Split(new[] { ". " }, StringSplitOptions.RemoveEmptyEntries);
                string groupe = "";
                foreach (string p in phrases)
                {
                    groupe += p.Trim() + ". ";
                    if (groupe.Length > 200) { etapes.Add(groupe.Trim()); groupe = ""; }
                }
                if (!string.IsNullOrEmpty(groupe)) etapes.Add(groupe.Trim());
            }

            return etapes;
        }

        private void ArreterAnimations()
        {
            try { _spinStory?.Stop(); } catch { }
            try { _pulseStory?.Stop(); } catch { }
        }

        public void AppliquerCouleur()
        {
            SolidColorBrush b = ThemeManager.CouleurAccent;
            TagCategorieBorder.Background = b;
            BoutonFavoriDetail.Background = b;

            // Mettre a jour le texte de chargement
            try { LoadingText.Foreground = b; } catch { }

            // Mettre a jour la barre de progression
            try { BarreProgression.Background = b; } catch { }

            // Mettre a jour le badge calories
            try { BadgeCalories.BorderBrush = b; } catch { }

            // Mettre a jour la barre d etapes mode guide
            try { BarreEtapes.Background = b; } catch { }
            try { TxtNumeroEtape.Foreground = b; } catch { }
            try { BtnSuivant.Background = b; } catch { }

            // Mettre a jour le bouton demarrer
            try { BoutonDemarrer.Background = b; } catch { }

            // Mettre a jour le bouton +
            try { BtnPlus.Background = b; } catch { }
        }

        private void BtnFavoriDetail_Click(object sender, MouseButtonEventArgs e)
        {
            if (_recetteActuelle == null) return;
            FavorisManager.BasculerFavori(_recetteActuelle);
            CoeurDetail.Text = FavorisManager.EstFavori(_recetteActuelle.Id) ? "❤️" : "🤍";
        }
    }
}