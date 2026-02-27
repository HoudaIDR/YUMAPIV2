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
    public partial class DetailView : UserControl
    {
        private MealController _controller = new MealController();

        // On garde la recette actuelle en mémoire pour le bouton ❤
        private MealListItem _recetteActuelle;

        public DetailView()
        {
            InitializeComponent();
        }

        public async void ChargerDetail(string id)
        {
            await _controller.ChargerDetailAsync(id);

            MealDto recette = _controller.DetailRecette;
            if (recette == null) return;

            // On crée un MealListItem pour les favoris (Id + Title + Thumb)
            _recetteActuelle = new MealListItem
            {
                Id = recette.idMeal,
                Title = recette.strMeal,
                Thumb = recette.strMealThumb
            };

            // On met à jour l'emoji ❤ selon si elle est déjà en favori
            CoeurDetail.Text = FavorisManager.EstFavori(id) ? "❤️" : "🤍";

            // Afficher le panneau détail
            PanneauAccueil.Visibility = Visibility.Collapsed;
            PanneauDetail.Visibility = Visibility.Visible;

            // Remplir les champs
            DetailTitre.Text = recette.strMeal;
            TagCategorie.Text = recette.strCategory;
            TagPays.Text = recette.strArea;
            DetailInstructions.Text = recette.strInstructions;

            if (!string.IsNullOrEmpty(recette.strMealThumb))
                DetailImage.Source = new BitmapImage(new Uri(recette.strMealThumb));

            // Ingrédients
            PanneauIngredients.Children.Clear();
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

        // ── Clic ❤ dans la page détail ───────────────────────────────────
        private void BtnFavoriDetail_Click(object sender, MouseButtonEventArgs e)
        {
            if (_recetteActuelle == null) return;

            FavorisManager.BasculerFavori(_recetteActuelle);

            // Met à jour l'emoji
            CoeurDetail.Text = FavorisManager.EstFavori(_recetteActuelle.Id) ? "❤️" : "🤍";
        }

        // ── Carte ingrédient ──────────────────────────────────────────────
        private void AjouterCarteIngredient(string mesure, string ingredient)
        {
            if (string.IsNullOrWhiteSpace(ingredient)) return;

            Border carte = new Border();
            carte.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
            carte.CornerRadius = new CornerRadius(10);
            carte.Padding = new Thickness(12, 10, 12, 10);
            carte.Margin = new Thickness(0, 0, 0, 8);
            carte.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2A2A"));
            carte.BorderThickness = new Thickness(1);

            Grid grille = new Grid();
            grille.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
            grille.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border badge = new Border();
            badge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B35"));
            badge.CornerRadius = new CornerRadius(22);
            badge.Width = 44;
            badge.Height = 44;

            TextBlock txMesure = new TextBlock();
            txMesure.Text = string.IsNullOrWhiteSpace(mesure) ? "—" : mesure.Trim();
            txMesure.Foreground = Brushes.White;
            txMesure.FontSize = 11;
            txMesure.FontWeight = FontWeights.Bold;
            txMesure.HorizontalAlignment = HorizontalAlignment.Center;
            txMesure.VerticalAlignment = VerticalAlignment.Center;
            txMesure.TextAlignment = TextAlignment.Center;
            badge.Child = txMesure;

            Grid.SetColumn(badge, 0);
            grille.Children.Add(badge);

            TextBlock txIngredient = new TextBlock();
            txIngredient.Text = ingredient.Trim();
            txIngredient.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC"));
            txIngredient.FontSize = 14;
            txIngredient.VerticalAlignment = VerticalAlignment.Center;
            txIngredient.Margin = new Thickness(12, 0, 0, 0);
            txIngredient.TextWrapping = TextWrapping.Wrap;

            Grid.SetColumn(txIngredient, 1);
            grille.Children.Add(txIngredient);

            carte.Child = grille;
            PanneauIngredients.Children.Add(carte);
        }
    }
}