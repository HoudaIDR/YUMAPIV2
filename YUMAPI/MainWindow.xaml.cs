// ============================================================
//  MainWindow.xaml.cs
//  Navigation + bouton chat flottant IA
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
        private ListeView listeViewCourante;
        private ChatView chatView;

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

            ListeView listeView = new ListeView();
            DetailView detailView = new DetailView();

            listeViewCourante = listeView;

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

            // Afficher le bouton chat flottant
            BoutonChat.Visibility = Visibility.Visible;

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

        // ── Clic bouton chat flottant ─────────────────────────────────────
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
