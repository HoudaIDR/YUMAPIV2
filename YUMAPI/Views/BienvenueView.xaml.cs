// ============================================================
//  Views/BienvenueView.xaml.cs
//  Page de bienvenue affichée juste après la connexion
// ============================================================

using System.Windows;
using System.Windows.Controls;
using YUMAPI.Controllers;

namespace YUMAPI.Views
{
    public partial class BienvenueView : UserControl
    {
        // L'id du plat proposé par l'API (on le garde en mémoire)
        private string _idPlatSuggere = null;

        // Événement déclenché quand l'utilisateur clique "Oui"
        // On envoie l'id du plat à MainWindow
        public delegate void OuiHandler(string idPlat);
        public event OuiHandler OuiClique;

        // Événement déclenché quand l'utilisateur clique "Non"
        public delegate void NonHandler();
        public event NonHandler NonClique;

        public BienvenueView()
        {
            InitializeComponent();
        }

        // ── Appelée depuis MainWindow pour démarrer la page ──────────────
        public async void Initialiser(string username)
        {
            // Afficher le prénom dans le titre
            string l = TraductionService.LangueActuelle;
            TxtBienvenue.Text = l == "es" ? "¡Bienvenido " + username + "!"
                              : l == "fr" ? "Bienvenue " + username + " !"
                              : "Welcome " + username + "!";

            // Traduire les boutons
            TxtBtnOui.Text = l == "es" ? "¡Sí! Vamos 🚀"
                           : l == "fr" ? "Oui ! J'y vais 🚀"
                           : "Yes! Let's go 🚀";
            TxtBtnNon.Text = l == "es" ? "No gracias"
                           : l == "fr" ? "Non merci"
                           : "No thanks";

            // Appeler l'API pour choisir un plat au hasard
            string[] resultat = await TraductionService.ProposerUnPlat();

            if (resultat != null)
            {
                // resultat[0] = la phrase à afficher
                // resultat[1] = l'id du plat dans TheMealDB
                TxtProposition.Text = resultat[0];
                _idPlatSuggere = resultat[1];
            }
            else
            {
                // Si l'API ne répond pas → message de secours
                string l2 = TraductionService.LangueActuelle;
                TxtProposition.Text = l2 == "es" ? "¡Hoy podríamos cocinar algo delicioso! 🍴"
                                    : l2 == "fr" ? "Aujourd'hui on pourrait cuisiner... quelque chose de délicieux ! 🍴"
                                    : "Today we could cook something delicious! 🍴";
                _idPlatSuggere = null;
            }
        }

        // ── Clic sur "Oui ! J'y vais" ────────────────────────────────────
        private void BtnOui_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_idPlatSuggere != null)
            {
                // On envoie l'id du plat à MainWindow
                if (OuiClique != null)
                    OuiClique(_idPlatSuggere);
            }
            else
            {
                // Pas d'id disponible → on ferme comme un "Non"
                if (NonClique != null)
                    NonClique();
            }
        }

        // ── Clic sur "Non merci" ─────────────────────────────────────────
        private void BtnNon_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (NonClique != null)
                NonClique();
        }
    }
}