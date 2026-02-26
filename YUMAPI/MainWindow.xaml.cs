// ============================================================
//  MainWindow.xaml.cs
//  Place ListeView à gauche et DetailView à droite
//  Les deux sont visibles en même temps (pas de navigation)
// ============================================================

using System.Windows;
using YUMAPI.Views;

namespace YUMAPI
{
    public partial class MainWindow : Window
    {
        // Les deux UserControls (comme MeteoHouda)
        private ListeView listeView;
        private DetailView detailView;

        public MainWindow()
        {
            InitializeComponent();

            // Créer les deux views
            listeView = new ListeView();
            detailView = new DetailView();

            // Quand l'utilisateur clique sur une recette dans ListeView,
            // on charge le détail dans DetailView
            listeView.RecetteCliquee += OnRecetteCliquee;

            // Placer les views dans les deux colonnes
            ContainerListe.Children.Add(listeView);
            ContainerDetail.Children.Add(detailView);
        }

        // ── Appelé quand une recette est cliquée dans ListeView ───────────
        private void OnRecetteCliquee(string id)
        {
            // (comme meteoPage.ChargerMeteo(ville) dans MeteoHouda)
            detailView.ChargerDetail(id);
        }
    }
}
