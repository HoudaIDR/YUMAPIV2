// ============================================================
//  MainWindow.xaml.cs
//  Place ListeView à gauche et DetailView à droite
//  Les deux sont visibles en même temps (pas de navigation)
// ============================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YUMAPI.Views;

namespace YUMAPI
{
    public partial class MainWindow : Window
    {
        private LoginView loginView;
        private RegisterView registerView;

        public MainWindow()
        {
            InitializeComponent();

            loginView = new LoginView();
            registerView = new RegisterView();

            loginView.ConnexionReussie += OnConnexionReussie;
            loginView.AllerInscription += AfficherInscription;
            registerView.InscriptionReussie += OnConnexionReussie;
            registerView.AllerConnexion += AfficherLogin;

            AfficherLogin();
        }

        // ── Afficher la page de connexion ─────────────────────────────────
        private void AfficherLogin()
        {
            loginView.Vider();
            ContainerPrincipal.Children.Clear();
            ContainerPrincipal.Children.Add(loginView);
        }

        // ── Afficher la page d'inscription ────────────────────────────────
        private void AfficherInscription()
        {
            registerView.Vider();
            ContainerPrincipal.Children.Clear();
            ContainerPrincipal.Children.Add(registerView);
        }

        // ── Connexion réussie → afficher les recettes ─────────────────────
        private void OnConnexionReussie(string username)
        {
            ContainerPrincipal.Children.Clear();

            // Nouvelles instances à chaque connexion (évite les conflits de parent)
            ListeView listeView = new ListeView();
            DetailView detailView = new DetailView();

            listeView.RecetteCliquee += (id) => detailView.ChargerDetail(id);
            listeView.Deconnexion += AfficherLogin;

            Grid grille = new Grid();
            grille.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360) });
            grille.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(listeView, 0);
            Grid.SetColumn(detailView, 1);

            grille.Children.Add(listeView);
            grille.Children.Add(detailView);

            ContainerPrincipal.Children.Add(grille);
        }
    }
}