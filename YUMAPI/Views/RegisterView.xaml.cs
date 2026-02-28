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
using YUMAPI.Controllers;

namespace YUMAPI.Views
{
    public partial class RegisterView : UserControl
    {
        private UserController _controller = new UserController();

        public delegate void InscriptionReussieHandler(string username);
        public event InscriptionReussieHandler InscriptionReussie;

        public delegate void AllerConnexionHandler();
        public event AllerConnexionHandler AllerConnexion;

        public RegisterView()
        {
            InitializeComponent();
        }

        // ── Clic sur "Créer mon compte" ────────────────────────────────────
        private void BtnInscrire_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string motDePasse = PasswordBox.Password;
            string confirmation = ConfirmPasswordBox.Password;

            // ── 1. Champs vides ────────────────────────────────────────────
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(motDePasse) ||
                string.IsNullOrEmpty(confirmation))
            {
                AfficherErreur("Veuillez remplir tous les champs.");
                return;
            }

            // ── 2. Nom d'utilisateur trop court ───────────────────────────
            if (username.Length < 3)
            {
                AfficherErreur("Le nom d'utilisateur doit contenir au moins 3 caractères.");
                return;
            }

            // ── 3. Mot de passe minimum 8 caractères ──────────────────────
            if (motDePasse.Length < 8)
            {
                AfficherErreur("Mot de passe trop court.\nMinimum 8 caractères requis.");
                return;
            }

            // ── 4. Confirmation mot de passe ───────────────────────────────
            if (motDePasse != confirmation)
            {
                AfficherErreur("Les mots de passe ne correspondent pas.");
                return;
            }

            // ── 5. Inscription via le contrôleur ──────────────────────────
            bool succes = _controller.Inscrire(username, motDePasse);

            if (succes)
            {
                _controller.SeConnecter(username, motDePasse);

                if (InscriptionReussie != null)
                    InscriptionReussie(username);
            }
            else
            {
                AfficherErreur(_controller.DerniereErreur);
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

        // ── Clic sur "Se connecter" ────────────────────────────────────────
        private void LienConnexion_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AllerConnexion != null)
                AllerConnexion();
        }
    }
}
