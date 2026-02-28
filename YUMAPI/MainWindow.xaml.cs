// ============================================================
//  MainWindow.xaml.cs
//  Place ListeView à gauche et DetailView à droite
//  Les deux sont visibles en même temps (pas de navigation)
// ============================================================

using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YUMAPI.Views;

namespace YUMAPI
{
    public partial class MainWindow : Window
    {
        // Toutes les pages
        private LoginView loginView;
        private RegisterView registerView;
        private ListeView listeView;
        private DetailView detailView;

        public MainWindow()
        {
            InitializeComponent();

            // Créer les pages
            loginView = new LoginView();
            registerView = new RegisterView();
            listeView = new ListeView();
            detailView = new DetailView();

            // ── Abonnements aux événements ──────────────────────────────
            // LoginView
            loginView.ConnexionReussie += OnConnexionReussie;
            loginView.AllerInscription += AfficherInscription;

            // RegisterView
            registerView.InscriptionReussie += OnConnexionReussie;
            registerView.AllerConnexion += AfficherLogin;

            // ListeView : quand une recette est cliquée
            listeView.RecetteCliquee += OnRecetteCliquee;

            // Afficher la page de connexion au démarrage
            AfficherLogin();
        }

        // ── Afficher la page de connexion ─────────────────────────────────
        private void AfficherLogin()
        {
            ContainerPrincipal.Children.Clear();
            ContainerPrincipal.Children.Add(loginView);
        }

        // ── Afficher la page d'inscription ────────────────────────────────
        private void AfficherInscription()
        {
            ContainerPrincipal.Children.Clear();
            ContainerPrincipal.Children.Add(registerView);
        }

        // ── Connexion ou inscription réussie → afficher les recettes ──────
        private void OnConnexionReussie(string username)
        {
            ContainerPrincipal.Children.Clear();

            // Créer la mise en page 2 colonnes pour les recettes
            Grid grille = new Grid();
            grille.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360) });
            grille.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(listeView, 0);
            Grid.SetColumn(detailView, 1);

            grille.Children.Add(listeView);
            grille.Children.Add(detailView);

            ContainerPrincipal.Children.Add(grille);
        }

        // ── Recette cliquée dans ListeView ────────────────────────────────
        private void OnRecetteCliquee(string id)
        {
            detailView.ChargerDetail(id);
        }
    }
}