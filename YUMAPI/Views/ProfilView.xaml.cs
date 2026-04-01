using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YUMAPI.Controllers;
using YUMAPI.Models;

namespace YUMAPI.Views
{
    public partial class ProfilView : UserControl
    {
        public delegate void FermerHandler();
        public event FermerHandler Fermer;

        public ProfilView()
        {
            InitializeComponent();
        }

        public void Initialiser()
        {
            User user = UserController.UtilisateurConnecte;
            if (user == null) return;

            TxtNomProfil.Text = user.Username;
            TxtAvatarGrand.Text = user.Username.Length > 0
                ? user.Username[0].ToString().ToUpper()
                : "?";

            // Texte et format de date selon la langue active
            string l = TraductionService.LangueActuelle;
            string formatDate = l == "fr" ? "dd MMMM yyyy" : l == "es" ? "dd MMMM yyyy" : "MMMM dd, yyyy";
            CultureInfo culture = l == "fr" ? new CultureInfo("fr-FR")
                                : l == "es" ? new CultureInfo("es-ES")
                                : new CultureInfo("en-US");
            string labelMembre = l == "fr" ? "Membre depuis le "
                               : l == "es" ? "Miembro desde el "
                               : "Member since ";
            TxtDateCreation.Text = labelMembre + user.DateCreation.ToString(formatDate, culture);

            int nbFavoris = user.Favoris != null ? user.Favoris.Count : 0;
            string labelRecette = TraductionService.LangueActuelle == "es"
                ? (nbFavoris > 1 ? " recetas" : " receta")
                : TraductionService.LangueActuelle == "fr"
                    ? (nbFavoris > 1 ? " recettes" : " recette")
                    : (nbFavoris > 1 ? " recipes" : " recipe");
            TxtNbFavoris.Text = nbFavoris + labelRecette;

            if (user.DerniereRecette != null)
                TxtDerniereRecette.Text = user.DerniereRecette.Title;
            else
                TxtDerniereRecette.Text = TraductionService.LangueActuelle == "es"
                    ? "Ninguna receta consultada"
                    : TraductionService.LangueActuelle == "fr"
                        ? "Aucune recette consultée"
                        : "No recipe viewed yet";

            // Traduire les labels fixes selon la langue active
            string lang = TraductionService.LangueActuelle;
            TxtLabelFavoris.Text = lang == "es" ? "Mis favoritos"
                                 : lang == "fr" ? "Mes favoris"
                                 : "My favorites";
            TxtLabelDerniereRecette.Text = lang == "es" ? "Última receta consultada"
                                         : lang == "fr" ? "Dernière recette consultée"
                                         : "Last viewed recipe";
            TxtLabelCouleur.Text = lang == "es" ? "🎨  Color del tema"
                                 : lang == "fr" ? "🎨  Couleur du thème"
                                 : "🎨  Theme color";

            ThemeManager.ChangerCouleur(user.CouleurTheme ?? "#FF6B35");
            AppliquerCouleurSurInterface();
        }

        private void BtnCouleur_Click(object sender, MouseButtonEventArgs e)
        {
            Border cercle = sender as Border;
            if (cercle == null) return;

            string hex = cercle.Tag as string;
            if (string.IsNullOrWhiteSpace(hex)) return;

            hex = hex.Trim().ToUpper();

            ThemeManager.ChangerCouleur(hex);

            User user = UserController.UtilisateurConnecte;
            if (user != null)
            {
                user.CouleurTheme = hex;
                UserController.Sauvegarder();
            }

            AppliquerCouleurSurInterface();
        }

        private void AppliquerCouleurSurInterface()
        {
            SolidColorBrush brosse = ThemeManager.CouleurAccent;

            AvatarBorder.Background = brosse;

            SupprimerEffetsTousLesCercles();

            string hex = (ThemeManager.CouleurHex ?? "").Trim().ToUpper();
            string nom = ObtenirNomCouleur(hex);

            if (hex == "#FF6B35") AjouterEffet(CerclOrange, hex);
            else if (hex == "#E91E8C") AjouterEffet(CerclRose, hex);
            else if (hex == "#E53935") AjouterEffet(CerclRouge, hex);
            else if (hex == "#1E88E5") AjouterEffet(CerclBleu, hex);
            else if (hex == "#43A047") AjouterEffet(CerclVert, hex);
            else if (hex == "#8E24AA") AjouterEffet(CerclViolet, hex);

            string labelCouleur = TraductionService.LangueActuelle == "es" ? "Color activo : "
                             : TraductionService.LangueActuelle == "fr" ? "Couleur active : "
                             : "Active color: ";
            TxtCouleurActive.Text = labelCouleur + nom;
            TxtCouleurActive.Foreground = brosse;
        }

        private string ObtenirNomCouleur(string hex)
        {
            hex = (hex ?? "").Trim().ToUpper();
            string l = TraductionService.LangueActuelle;

            switch (hex)
            {
                case "#FF6B35": return l == "es" ? "Naranja" : l == "fr" ? "Orange" : "Orange";
                case "#E91E8C": return l == "es" ? "Rosa" : l == "fr" ? "Rose" : "Pink";
                case "#E53935": return l == "es" ? "Rojo" : l == "fr" ? "Rouge" : "Red";
                case "#1E88E5": return l == "es" ? "Azul" : l == "fr" ? "Bleu" : "Blue";
                case "#43A047": return l == "es" ? "Verde" : l == "fr" ? "Vert" : "Green";
                case "#8E24AA": return l == "es" ? "Violeta" : l == "fr" ? "Violet" : "Purple";
                default: return l == "es" ? "Desconocido" : l == "fr" ? "Inconnue" : "Unknown";
            }
        }

        private void AjouterEffet(Border cercle, string hex)
        {
            cercle.BorderBrush = new SolidColorBrush(Colors.White);
            cercle.BorderThickness = new Thickness(3);
            cercle.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString(hex),
                BlurRadius = 14,
                Opacity = 0.8,
                ShadowDepth = 0
            };
        }

        private void SupprimerEffetsTousLesCercles()
        {
            foreach (Border b in new[] { CerclOrange, CerclRose, CerclRouge, CerclBleu, CerclVert, CerclViolet })
            {
                b.BorderThickness = new Thickness(0);
                b.BorderBrush = null;
                b.Effect = null;
            }
        }

        private void BtnFermer_Click(object sender, MouseButtonEventArgs e)
        {
            if (Fermer != null)
                Fermer();
        }
    }
}