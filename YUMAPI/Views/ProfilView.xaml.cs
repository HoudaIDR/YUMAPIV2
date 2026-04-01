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

            TxtDateCreation.Text = "Membre depuis le " + user.DateCreation.ToString(
                "dd MMMM yyyy",
                new CultureInfo("fr-FR"));

            int nbFavoris = user.Favoris != null ? user.Favoris.Count : 0;
            TxtNbFavoris.Text = nbFavoris + (nbFavoris > 1 ? " recettes" : " recette");

            if (user.DerniereRecette != null)
                TxtDerniereRecette.Text = user.DerniereRecette.Title;
            else
                TxtDerniereRecette.Text = "Aucune recette consultée";

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

            TxtCouleurActive.Text = "Couleur active : " + nom;
            TxtCouleurActive.Foreground = brosse;
        }

        private string ObtenirNomCouleur(string hex)
        {
            hex = (hex ?? "").Trim().ToUpper();

            switch (hex)
            {
                case "#FF6B35": return "Orange";
                case "#E91E8C": return "Rose";
                case "#E53935": return "Rouge";
                case "#1E88E5": return "Bleu";
                case "#43A047": return "Vert";
                case "#8E24AA": return "Violet";
                default: return "Inconnue";
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