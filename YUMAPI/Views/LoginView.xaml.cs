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
    public partial class LoginView : UserControl
    {
        public delegate void ConnexionReussieHandler(string username);
        public event ConnexionReussieHandler ConnexionReussie;

        public delegate void AllerInscriptionHandler();
        public event AllerInscriptionHandler AllerInscription;

        public LoginView()
        {
            InitializeComponent();
        }

        // ── Vide les champs (appelé à chaque affichage) ───────────────────
        public void Vider()
        {
            UsernameBox.Text = "";
            PasswordBox.Password = "";
            ErreurLogin.Visibility = Visibility.Collapsed;
        }

        // ── Clic "Se connecter" ───────────────────────────────────────────
        private void BtnConnecter_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string motDePasse = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(motDePasse))
            {
                ErreurLogin.Text = "Veuillez remplir tous les champs.";
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

        // ── Clic "Créer un compte" ─────────────────────────────────────────
        private void LienInscription_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AllerInscription != null)
                AllerInscription();
        }
    }
}