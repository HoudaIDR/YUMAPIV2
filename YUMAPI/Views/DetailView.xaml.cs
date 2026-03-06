using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using YUMAPI.Controllers;
using YUMAPI.Models;

namespace YUMAPI.Views
{
    /// <summary>
    /// Vue qui affiche le détail complet d’une recette.
    /// Elle charge les informations depuis l’API et met à jour l’interface.
    /// </summary>
    public partial class DetailView : UserControl
    {
        // Contrôleur qui récupère les données depuis l’API
        private MealController _controller = new MealController();

        // Recette actuellement affichée (utile pour la gestion des favoris)
        private MealListItem _recetteActuelle;

        /// <summary>
        /// Constructeur : initialise la vue.
        /// </summary>
        public DetailView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Charge le détail d’une recette à partir de son ID.
        /// </summary>
        public async void ChargerDetail(string id)
        {
            // Appel API pour récupérer le détail
            await _controller.ChargerDetailAsync(id);

            MealDto recette = _controller.DetailRecette;
            if (recette == null) return;

            // On mémorise la recette pour le bouton Favori
            _recetteActuelle = new MealListItem
            {
                Id = recette.idMeal,
                Title = recette.strMeal,
                Thumb = recette.strMealThumb
            };

            // Appliquer la couleur du thème actif
            AppliquerCouleur();

            // Mettre à jour l’icône cœur selon si déjà en favori
            CoeurDetail.Text = FavorisManager.EstFavori(id) ? "❤️" : "🤍";

            // Afficher le panneau détail et masquer l’écran d’accueil
            PanneauAccueil.Visibility = Visibility.Collapsed;
            PanneauDetail.Visibility = Visibility.Visible;

            // Remplissage des champs texte
            DetailTitre.Text = recette.strMeal;
            TagCategorie.Text = recette.strCategory;
            TagPays.Text = recette.strArea;
            DetailInstructions.Text = recette.strInstructions;

            // Chargement de l’image si disponible
            if (!string.IsNullOrEmpty(recette.strMealThumb))
                DetailImage.Source = new BitmapImage(new Uri(recette.strMealThumb));

            // Nettoyer les anciens ingrédients
            PanneauIngredients.Children.Clear();

            // Ajouter les cartes ingrédients (limité ici à 10)
            AjouterCarteIngredient(recette.strMeasure1, recette.strIngredient1);
            AjouterCarteIngredient(recette.strMeasure2, recette.strIngredient2);
            AjouterCarteIngredient(recette.strMeasure3, recette.strIngredient3);
            AjouterCarteIngredient(recette.strMeasure4, recette.strIngredient4);
            AjouterCarteIngredient(recette.strMeasure5, recette.strIngredient5);
            AjouterCarteIngredient(recette.strMeasure6, recette.strIngredient6);
            AjouterCarteIngredient(recette.strMeasure7, recette.strIngredient7);
            AjouterCarteIngredient(recette.strMeasure8, recette.strIngredient8);
            AjouterCarteIngredient(recette.strMeasure9, recette.strIngredient9);
            AjouterCarteIngredient(recette.strMeasure10, recette.strIngredient10);
        }

        // ─────────────────────────────────────────────────────────────
        // 🎨 Applique la couleur accent du thème
        // ─────────────────────────────────────────────────────────────
        public void AppliquerCouleur()
        {
            // Récupère la couleur définie dans le ThemeManager
            SolidColorBrush brosse = ThemeManager.CouleurAccent;

            // Applique la couleur au tag catégorie
            TagCategorieBorder.Background = brosse;

            // Applique la couleur au bouton Favori
            BoutonFavoriDetail.Background = brosse;
        }

        // ─────────────────────────────────────────────────────────────
        // ❤️ Clic sur le bouton Favori
        // ─────────────────────────────────────────────────────────────
        private void BtnFavoriDetail_Click(object sender, MouseButtonEventArgs e)
        {
            if (_recetteActuelle == null) return;

            // Ajoute ou retire la recette des favoris
            FavorisManager.BasculerFavori(_recetteActuelle);

            // Met à jour l’icône cœur
            CoeurDetail.Text = FavorisManager.EstFavori(_recetteActuelle.Id)
                ? "❤️"
                : "🤍";
        }

        // ─────────────────────────────────────────────────────────────
        // 🥕 Crée dynamiquement une carte ingrédient
        // ─────────────────────────────────────────────────────────────
        private void AjouterCarteIngredient(string mesure, string ingredient)
        {
            // Si l’ingrédient est vide → on ne crée rien
            if (string.IsNullOrWhiteSpace(ingredient)) return;

            // Carte principale (fond sombre, coins arrondis)
            Border carte = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 8),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2A2A")),
                BorderThickness = new Thickness(1)
            };

            // Grille interne à 2 colonnes
            Grid grille = new Grid();
            grille.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
            grille.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Badge rond coloré pour la mesure
            Border badge = new Border
            {
                Background = ThemeManager.CouleurAccent,
                CornerRadius = new CornerRadius(22),
                Width = 44,
                Height = 44
            };

            // Texte de la mesure
            TextBlock txMesure = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(mesure) ? "—" : mesure.Trim(),
                Foreground = Brushes.White,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };

            badge.Child = txMesure;
            Grid.SetColumn(badge, 0);
            grille.Children.Add(badge);

            // Texte du nom d’ingrédient
            TextBlock txIngredient = new TextBlock
            {
                Text = ingredient.Trim(),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC")),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };

            Grid.SetColumn(txIngredient, 1);
            grille.Children.Add(txIngredient);

            // Ajout de la grille à la carte
            carte.Child = grille;

            // Ajout de la carte au panneau
            PanneauIngredients.Children.Add(carte);
        }
    }
}