using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using YUMAPI.Models;

namespace YUMAPI.Views
{
    public partial class RegisterView : UserControl
    {
        public delegate void InscriptionReussieHandler(string username);
        public event InscriptionReussieHandler InscriptionReussie;

        public delegate void AllerConnexionHandler();
        public event AllerConnexionHandler AllerConnexion;

        public RegisterView()
        {
            InitializeComponent();
        }

        // ── Vide les champs (appelé à chaque affichage) ───────────────────
        public void Vider()
        {
            UsernameBox.Text = "";
            PasswordBox.Password = "";
            ConfirmPasswordBox.Password = "";
            MessageRetour.Visibility = Visibility.Collapsed;
        }

        // ── Clic "Créer mon compte" ────────────────────────────────────────
        private void BtnInscrire_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string motDePasse = PasswordBox.Password;
            string confirmation = ConfirmPasswordBox.Password;

            // 1. Champs vides
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(motDePasse) ||
                string.IsNullOrEmpty(confirmation))
            {
                AfficherErreur("Veuillez remplir tous les champs.");
                return;
            }

            // 2. Pseudo trop court
            if (username.Length < 3)
            {
                AfficherErreur("Le nom d'utilisateur doit contenir au moins 3 caractères.");
                return;
            }

            // 3. Mot de passe trop court
            if (motDePasse.Length < 8)
            {
                AfficherErreur("Mot de passe trop court. Minimum 8 caractères requis.");
                return;
            }

            // 4. Confirmation
            if (motDePasse != confirmation)
            {
                AfficherErreur("Les mots de passe ne correspondent pas.");
                return;
            }

            // 5. Inscription
            bool succes = UserController.Inscrire(username, motDePasse);

            if (succes)
            {
                UserController.SeConnecter(username, motDePasse);

                if (InscriptionReussie != null)
                    InscriptionReussie(username);
            }
            else
            {
                AfficherErreur(UserController.DerniereErreur);
            }
        }

        // ── Affiche un message d'erreur en rouge ──────────────────────────
        private void AfficherErreur(string message)
        {
            MessageRetour.Text = message;
            MessageRetour.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#FF5555"));
            MessageRetour.Visibility = Visibility.Visible;
        }

        // ── Clic "Se connecter" ────────────────────────────────────────────
        private void LienConnexion_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AllerConnexion != null)
                AllerConnexion();
        }
    }
}