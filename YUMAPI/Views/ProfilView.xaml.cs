// ============================================================
//  Views/ProfilView.xaml.cs
//  Affiche les infos du profil + choix de la couleur thème
// ============================================================

using System;
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
        // Événement pour fermer le profil
        public delegate void FermerHandler();
        public event FermerHandler Fermer;

        public ProfilView()
        {
            InitializeComponent();
        }

        // ── Appelée depuis MainWindow pour remplir les infos ─────────────
        public void Initialiser()
        {
            User user = UserController.UtilisateurConnecte;
            if (user == null) return;

            // Nom et initiale
            TxtNomProfil.Text = user.Username;
            TxtAvatarGrand.Text = user.Username.Length > 0
                ? user.Username[0].ToString().ToUpper()
                : "?";

            // Date de création du compte
            TxtDateCreation.Text = "Membre depuis le " + user.DateCreation.ToString("dd MMMM yyyy",
                new System.Globalization.CultureInfo("fr-FR"));

            // Nombre de favoris
            int nbFavoris = user.Favoris != null ? user.Favoris.Count : 0;
            TxtNbFavoris.Text = nbFavoris + (nbFavoris > 1 ? " recettes" : " recette");

            // Dernière recette consultée
            if (user.DerniereRecette != null)
                TxtDerniereRecette.Text = user.DerniereRecette.Title;
            else
                TxtDerniereRecette.Text = "Aucune recette consultée";

            // Appliquer la couleur du thème sauvegardée
            ThemeManager.ChangerCouleur(user.CouleurTheme ?? "#FF6B35");

            // Mettre à jour l'avatar et l'indicateur de couleur
            AppliquerCouleurSurInterface();
        }

        // ── Clic sur un cercle de couleur ────────────────────────────────
        private void BtnCouleur_Click(object sender, MouseButtonEventArgs e)
        {
            Border cercle = sender as Border;
            if (cercle == null) return;

            // Récupérer la couleur stockée dans le Tag du cercle
            string hex = cercle.Tag as string;
            if (string.IsNullOrEmpty(hex)) return;

            // Changer la couleur dans ThemeManager (notifie toutes les vues)
            ThemeManager.ChangerCouleur(hex);

            // Sauvegarder dans le profil de l'utilisateur
            User user = UserController.UtilisateurConnecte;
            if (user != null)
            {
                user.CouleurTheme = hex;
                UserController.Sauvegarder();
            }

            // Mettre à jour l'interface du profil
            AppliquerCouleurSurInterface();
        }

        // ── Met à jour l'avatar et l'indicateur de couleur active ────────
        private void AppliquerCouleurSurInterface()
        {
            SolidColorBrush brosse = ThemeManager.CouleurAccent;

            // Couleur de l'avatar
            AvatarBorder.Background = brosse;

            // Enlever l'ombre sur tous les cercles
            SupprimerEffetsTousLesCercles();

            // Ajouter l'ombre sur le cercle actif et mettre à jour le texte
            string hex = ThemeManager.CouleurHex;
            string nom = "Orange (par défaut)";

            if (hex == "#FF6B35") { AjouterEffet(CerclOrange, hex); nom = "Orange (par défaut)"; }
            else if (hex == "#F48FB1") { AjouterEffet(CerclRose, hex); nom = "Rose"; }
            else if (hex == "#C62828 ") { AjouterEffet(CerclRouge, hex); nom = "Rouge"; }
            else if (hex == "#90CAF9") { AjouterEffet(CerclBleu, hex); nom = "Bleu"; }
            else if (hex == "#A5D6A7 ") { AjouterEffet(CerclVert, hex); nom = "Vert"; }
            else if (hex == "#6A1B9A ") { AjouterEffet(CerclViolet, hex); nom = "Violet"; }

            TxtCouleurActive.Text = "Couleur active : " + nom;
            TxtCouleurActive.Foreground = brosse;
        }

        // ── Ajoute un anneau blanc autour du cercle actif ────────────────
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

        // ── Retire les effets de tous les cercles ────────────────────────
        private void SupprimerEffetsTousLesCercles()
        {
            foreach (Border b in new[] { CerclOrange, CerclRose, CerclRouge,
                                         CerclBleu, CerclVert, CerclViolet })
            {
                b.BorderThickness = new Thickness(0);
                b.BorderBrush = null;
                b.Effect = null;
            }
        }

        // ── Clic bouton fermer ───────────────────────────────────────────
        private void BtnFermer_Click(object sender, MouseButtonEventArgs e)
        {
            if (Fermer != null)
                Fermer();
        }
    }
}