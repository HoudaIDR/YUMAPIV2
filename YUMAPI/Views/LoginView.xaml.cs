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
using System.Windows.Shapes;
using YUMAPI.Controllers;

namespace YUMAPI.Views
{
    public partial class LoginView : UserControl
    {
        private UserController _controller = new UserController();

        public delegate void ConnexionReussieHandler(string username);
        public event ConnexionReussieHandler ConnexionReussie;

        public delegate void AllerInscriptionHandler();
        public event AllerInscriptionHandler AllerInscription;

        public LoginView()
        {
            InitializeComponent();
        }

        // ── Clic sur "Se connecter" ────────────────────────────────────────
        private void BtnConnecter_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string motDePasse = PasswordBox.Password;

            // Vérification champs vides
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(motDePasse))
            {
                ErreurLogin.Text = "Veuillez remplir tous les champs.";
                ErreurLogin.Visibility = Visibility.Visible;
                return;
            }

            // Tentative de connexion
            bool succes = _controller.SeConnecter(username, motDePasse);

            if (succes)
            {
                ErreurLogin.Visibility = Visibility.Collapsed;

                if (ConnexionReussie != null)
                    ConnexionReussie(_controller.UtilisateurConnecte.Username);
            }
            else
            {
                // "Nom d'utilisateur ou mot de passe incorrect."
                // ou "Impossible de se connecter à la base de données."
                ErreurLogin.Text = _controller.DerniereErreur;
                ErreurLogin.Visibility = Visibility.Visible;
            }
        }

        // ── Clic sur "Créer un compte" ─────────────────────────────────────
        private void LienInscription_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AllerInscription != null)
                AllerInscription();
        }
    }
}