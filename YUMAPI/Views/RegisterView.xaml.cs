// ============================================================
//  Views/RegisterView.xaml.cs
//  + Traduction automatique selon la langue choisie au login
// ============================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YUMAPI.Controllers;
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
            Loaded += (s, e) => AppliquerLangue();
        }

        // ── Vide les champs ───────────────────────────────────────────────
        public void Vider()
        {
            UsernameBox.Text = "";
            PasswordBox.Password = "";
            ConfirmPasswordBox.Password = "";
            MessageRetour.Visibility = Visibility.Collapsed;
            AppliquerLangue();
        }

        // ── Traduction de l'interface ─────────────────────────────────────
        private void AppliquerLangue()
        {
            string langue = TraductionService.LangueActuelle;

            switch (langue)
            {
                case "es":
                    TxtTitre.Text = "Crear una cuenta";
                    TxtSousTitre.Text = "¡Únete a la comunidad Yum! 🎉";
                    TxtSousTitreGauche.Text = "Descubre miles de recetas";
                    TxtLabelUsername.Text = "Nombre de usuario";
                    TxtLabelPassword.Text = "Contraseña";
                    TxtLabelConfirm.Text = "Confirmar contraseña";
                    BtnInscrire.Content = "Crear mi cuenta";
                    TxtDejaCompte.Text = "¿Ya tienes cuenta? ";
                    LienConnexion.Text = "Iniciar sesión";
                    break;

                case "en":
                    TxtTitre.Text = "Create an account";
                    TxtSousTitre.Text = "Join the Yum! community 🎉";
                    TxtSousTitreGauche.Text = "Discover thousands of recipes";
                    TxtLabelUsername.Text = "Username";
                    TxtLabelPassword.Text = "Password";
                    TxtLabelConfirm.Text = "Confirm password";
                    BtnInscrire.Content = "Create my account";
                    TxtDejaCompte.Text = "Already have an account? ";
                    LienConnexion.Text = "Sign in";
                    break;

                default: // "fr"
                    TxtTitre.Text = "Créer un compte";
                    TxtSousTitre.Text = "Rejoignez la communauté Yum! 🎉";
                    TxtSousTitreGauche.Text = "Découvrez des milliers de recettes";
                    TxtLabelUsername.Text = "Nom d'utilisateur";
                    TxtLabelPassword.Text = "Mot de passe";
                    TxtLabelConfirm.Text = "Confirmer le mot de passe";
                    BtnInscrire.Content = "Créer mon compte";
                    TxtDejaCompte.Text = "Déjà un compte ? ";
                    LienConnexion.Text = "Se connecter";
                    break;
            }
        }

        // ── Inscription ───────────────────────────────────────────────────
        private void BtnInscrire_Click(object sender, RoutedEventArgs e)
        {
            string langue = TraductionService.LangueActuelle;
            string username = UsernameBox.Text.Trim();
            string motDePasse = PasswordBox.Password;
            string confirmation = ConfirmPasswordBox.Password;

            // Champs vides
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(motDePasse) ||
                string.IsNullOrEmpty(confirmation))
            {
                AfficherErreur(langue == "es" ? "Por favor, completa todos los campos."
                             : langue == "en" ? "Please fill in all fields."
                             : "Veuillez remplir tous les champs.");
                return;
            }

            // Pseudo trop court
            if (username.Length < 3)
            {
                AfficherErreur(langue == "es" ? "El nombre debe tener al menos 3 caracteres."
                             : langue == "en" ? "Username must be at least 3 characters."
                             : "Le nom doit contenir au moins 3 caractères.");
                return;
            }

            // Mot de passe trop court
            if (motDePasse.Length < 8)
            {
                AfficherErreur(langue == "es" ? "La contraseña debe tener al menos 8 caracteres."
                             : langue == "en" ? "Password must be at least 8 characters."
                             : "Mot de passe trop court. Minimum 8 caractères.");
                return;
            }

            // Confirmation
            if (motDePasse != confirmation)
            {
                AfficherErreur(langue == "es" ? "Las contraseñas no coinciden."
                             : langue == "en" ? "Passwords do not match."
                             : "Les mots de passe ne correspondent pas.");
                return;
            }

            // Inscription
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

        private void AfficherErreur(string message)
        {
            MessageRetour.Text = message;
            MessageRetour.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#FF5555"));
            MessageRetour.Visibility = Visibility.Visible;
        }

        private void LienConnexion_Click(object sender, MouseButtonEventArgs e)
        {
            if (AllerConnexion != null)
                AllerConnexion();
        }
    }
}
