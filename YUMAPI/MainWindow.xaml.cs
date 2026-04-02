// ============================================================
//  MainWindow.xaml.cs
//  Navigation + bouton chat flottant IA
// ============================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YUMAPI.Controllers;
using YUMAPI.Views;
using YUMAPI.Models;

namespace YUMAPI
{
    public partial class MainWindow : Window
    {
        private LoginView loginView;
        private RegisterView registerView;
        private ListeView listeViewCourante;
        private ChatView chatView;
        private DetailView detailViewCourante;

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
            // Cacher le bouton chat et le panneau
            BoutonChat.Visibility = Visibility.Collapsed;
            PanneauChat.Visibility = Visibility.Collapsed;

            // Réinitialiser la couleur à orange (neutre) sur la page de connexion
            // La vraie couleur du compte sera chargée après la connexion
            ThemeManager.ChangerCouleur("#FF6B35");

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
            PanneauChat.Visibility = Visibility.Collapsed;

            // Appliquer la couleur sauvegardée du compte qui vient de se connecter
            if (UserController.UtilisateurConnecte != null)
            {
                string couleur = UserController.UtilisateurConnecte.CouleurTheme ?? "#FF6B35";
                ThemeManager.ChangerCouleur(couleur);
            }

            ListeView listeView = new ListeView();
            DetailView detailView = new DetailView();

            listeViewCourante = listeView;
            detailViewCourante = detailView;

            listeView.RecetteCliquee += (id) => detailView.ChargerDetail(id);
            listeView.Deconnexion += AfficherLogin;
            listeView.OuvrirProfil += AfficherProfil;

            Grid grille = new Grid();
            grille.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360) });
            grille.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(listeView, 0);
            Grid.SetColumn(detailView, 1);

            grille.Children.Add(listeView);
            grille.Children.Add(detailView);

            ContainerPrincipal.Children.Add(grille);

            BoutonChat.Visibility = Visibility.Visible;

            // Afficher la page de bienvenue
            AfficherBienvenue(username);

            // Préparer le ChatView
            chatView = new ChatView();
            chatView.Fermer += FermerChat;
            chatView.RechercheRecette += (motCle) =>
            {
                // Quand l'IA suggère une recette → lancer la recherche dans ListeView
                listeViewCourante?.LancerRecherche(motCle);
                FermerChat();
            };

            ContainerChat.Children.Clear();
            ContainerChat.Children.Add(chatView);
        }

        // ── Page de bienvenue ─────────────────────────────────────────────
        private void AfficherBienvenue(string username)
        {
            BienvenueView bienvenueView = new BienvenueView();
            bienvenueView.OuiClique += (idPlat) =>
            {
                FermerBienvenue();
                Dispatcher.InvokeAsync(() =>
                    detailViewCourante.ChargerDetail(idPlat),
                    System.Windows.Threading.DispatcherPriority.Loaded);
            };
            bienvenueView.NonClique += () => FermerBienvenue();
            ContainerBienvenue.Children.Clear();
            ContainerBienvenue.Children.Add(bienvenueView);
            ContainerBienvenue.Visibility = Visibility.Visible;
            bienvenueView.Initialiser(username);
        }

        private void FermerBienvenue()
        {
            ContainerBienvenue.Visibility = Visibility.Collapsed;
            ContainerBienvenue.Children.Clear();
        }

        // ── Page de profil ─────────────────────────────────────────────────
        private void AfficherProfil()
        {
            ProfilView profilView = new ProfilView();
            profilView.Fermer += () => FermerProfil();
            ContainerProfil.Children.Clear();
            ContainerProfil.Children.Add(profilView);
            ContainerProfil.Visibility = Visibility.Visible;
            profilView.Initialiser();
        }

        private void FermerProfil()
        {
            ContainerProfil.Visibility = Visibility.Collapsed;
            ContainerProfil.Children.Clear();
        }

        // ── Clic bouton chat flottant ─────────────────────────────────────────
        private void BoutonChat_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PanneauChat.Visibility == Visibility.Visible)
                FermerChat();
            else
                OuvrirChat();
        }

        // ── Ouvrir le panneau chat ────────────────────────────────────────
        private void OuvrirChat()
        {
            // Réinitialiser le chat avec la langue actuelle
            chatView.ReinitialiserEtOuvrir();

            PanneauChat.Visibility = Visibility.Visible;
            BoutonChat.Child = new TextBlock
            {
                Text = "✕",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private void FermerChat()
        {
            PanneauChat.Visibility = Visibility.Collapsed;

            BoutonChat.Child = new TextBlock
            {
                Text = "💬",
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
    }
}