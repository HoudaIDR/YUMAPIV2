// ============================================================
//  Views/LoginView.xaml.cs
//  + Sélecteur de langue qui traduit l'interface
// ============================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YUMAPI.Controllers;
using YUMAPI.Models;

namespace YUMAPI.Views
{
    public partial class LoginView : UserControl
    {
        public delegate void ConnexionReussieHandler(string username);
        public event ConnexionReussieHandler ConnexionReussie;

        public delegate void AllerInscriptionHandler();
        public event AllerInscriptionHandler AllerInscription;

        public LoginView()
        {
            InitializeComponent();
            // Appliquer la langue courante au chargement
            Loaded += (s, e) => AppliquerLangue(TraductionService.LangueActuelle);
        }

        public void Vider()
        {
            UsernameBox.Text = "";
            PasswordBox.Password = "";
            ErreurLogin.Visibility = Visibility.Collapsed;
        }

        // ════════════════════════════════════════════════════════════
        //  SÉLECTEUR DE LANGUE
        // ════════════════════════════════════════════════════════════
        private void BtnLangEN_Click(object sender, MouseButtonEventArgs e)
        {
            TraductionService.LangueActuelle = "en";
            AppliquerLangue("en");
        }

        private void BtnLangFR_Click(object sender, MouseButtonEventArgs e)
        {
            TraductionService.LangueActuelle = "fr";
            AppliquerLangue("fr");
        }

        private void BtnLangES_Click(object sender, MouseButtonEventArgs e)
        {
            TraductionService.LangueActuelle = "es";
            AppliquerLangue("es");
        }

        private void AppliquerLangue(string langue)
        {
            // Mettre à jour les boutons (actif = orange, inactif = transparent)
            SolidColorBrush orange = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B35"));
            SolidColorBrush transparent = new SolidColorBrush(Colors.Transparent);

            BtnLangEN.Background = langue == "en" ? orange : transparent;
            BtnLangFR.Background = langue == "fr" ? orange : transparent;
            BtnLangES.Background = langue == "es" ? orange : transparent;

            // Changer les couleurs du texte
            SolidColorBrush blanc = new SolidColorBrush(Colors.White);
            SolidColorBrush gris = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888888"));

            ((StackPanel)BtnLangEN.Child).Children[1].SetValue(TextBlock.ForegroundProperty, langue == "en" ? blanc : gris);
            ((StackPanel)BtnLangFR.Child).Children[1].SetValue(TextBlock.ForegroundProperty, langue == "fr" ? blanc : gris);
            ((StackPanel)BtnLangES.Child).Children[1].SetValue(TextBlock.ForegroundProperty, langue == "es" ? blanc : gris);

            // Traduire l'interface selon la langue
            switch (langue)
            {
                case "fr":
                    TxtTitre.Text = "Connexion";
                    TxtSousTitre.Text = "Bon retour parmi nous 👋";
                    TxtLabelUsername.Text = "Nom d'utilisateur";
                    TxtLabelPassword.Text = "Mot de passe";
                    BtnConnecter.Content = "Se connecter";
                    TxtPasDeCompte.Text = "Pas encore de compte ? ";
                    LienInscription.Text = "Créer un compte";
                    break;

                case "es":
                    TxtTitre.Text = "Iniciar sesión";
                    TxtSousTitre.Text = "Bienvenido de nuevo 👋";
                    TxtLabelUsername.Text = "Nombre de usuario";
                    TxtLabelPassword.Text = "Contraseña";
                    BtnConnecter.Content = "Iniciar sesión";
                    TxtPasDeCompte.Text = "¿No tienes cuenta? ";
                    LienInscription.Text = "Crear una cuenta";
                    break;

                default: // "en"
                    TxtTitre.Text = "Sign In";
                    TxtSousTitre.Text = "Welcome back 👋";
                    TxtLabelUsername.Text = "Username";
                    TxtLabelPassword.Text = "Password";
                    BtnConnecter.Content = "Sign In";
                    TxtPasDeCompte.Text = "Don't have an account? ";
                    LienInscription.Text = "Create account";
                    break;
            }
        }

        // ════════════════════════════════════════════════════════════
        //  CONNEXION
        // ════════════════════════════════════════════════════════════
        private void BtnConnecter_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string motDePasse = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(motDePasse))
            {
                ErreurLogin.Text = TraductionService.LangueActuelle == "es"
                    ? "Por favor, completa todos los campos."
                    : TraductionService.LangueActuelle == "fr"
                        ? "Veuillez remplir tous les champs."
                        : "Please fill in all fields.";
                ErreurLogin.Visibility = Visibility.Visible;
                return;
            }

            bool succes = UserController.SeConnecter(username, motDePasse);

            if (succes)
            {
                ErreurLogin.Visibility = Visibility.Collapsed;
                if (ConnexionReussie != null)
                    ConnexionReussie(UserController.UtilisateurConnecte.Username);
            }
            else
            {
                ErreurLogin.Text = UserController.DerniereErreur;
                ErreurLogin.Visibility = Visibility.Visible;
            }
        }

        private void LienInscription_Click(object sender, MouseButtonEventArgs e)
        {
            if (AllerInscription != null)
                AllerInscription();
        }
    }
}
