// ============================================================
//  Views/ChatView.xaml.cs
//  Chat IA + Reconnaissance vocale (System.Speech)
// ============================================================

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Speech.Recognition;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace YUMAPI.Views
{
    public partial class ChatView : UserControl
    {
        private const string TOKEN = "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";
        private const string MODELE = "gpt-4o-mini";
        private const string API_URL = "https://models.inference.ai.azure.com/chat/completions";

        private HttpClient _client = new HttpClient();
        private List<object> _historique = new List<object>();
        private SpeechRecognitionEngine _micro;
        private bool _microActif = false;
        private Storyboard _microPulse;

        public delegate void RechercheRecetteHandler(string motCle);
        public event RechercheRecetteHandler RechercheRecette;

        public delegate void FermerHandler();
        public event FermerHandler Fermer;

        public ChatView()
        {
            InitializeComponent();

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", TOKEN);

            _microPulse = (Storyboard)Resources["MicroPulse"];

            // Initialiser le micro
            InitialiserMicro();

            Loaded += (s, e) => AjouterMessageIA(
                "Bonjour ! 👋 Je suis votre assistant culinaire Yum!\n\n" +
                "Je peux vous aider à :\n" +
                "• Trouver une recette avec vos ingrédients\n" +
                "• Suggérer un plat selon vos envies\n\n" +
                "Dites-moi ce que vous voulez manger ! 🍽️\n" +
                "Vous pouvez aussi cliquer sur 🎤 pour parler !");
        }

        // ════════════════════════════════════════════════════════════
        //  RECONNAISSANCE VOCALE
        // ════════════════════════════════════════════════════════════
        private void InitialiserMicro()
        {
            try
            {
                _micro = new SpeechRecognitionEngine(
                    new System.Globalization.CultureInfo("fr-FR"));

                // Reconnaissance libre (pas de grammaire fixe)
                _micro.LoadGrammar(new DictationGrammar());

                _micro.SpeechRecognized += OnParoleReconnue;
                _micro.SpeechRecognitionRejected += OnParoleRejetee;
                _micro.RecognizeCompleted += OnReconnaissanceTerminee;

                _micro.SetInputToDefaultAudioDevice();
            }
            catch
            {
                // Micro non disponible → on désactive le bouton
                Dispatcher.InvokeAsync(() =>
                {
                    BoutonMicro.IsEnabled = false;
                    BoutonMicro.Opacity = 0.3;
                    BoutonMicro.ToolTip = "Micro non disponible";
                });
            }
        }

        // ── Clic bouton micro ─────────────────────────────────────────────
        private void BtnMicro_Click(object sender, MouseButtonEventArgs e)
        {
            if (_micro == null) return;

            if (_microActif)
                ArreterMicro();
            else
                DemarrerMicro();
        }

        private void DemarrerMicro()
        {
            try
            {
                _microActif = true;

                // Bouton rouge pulsant
                BoutonMicro.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#CC0000"));
                IconeMicro.Text = "⏹";
                BoutonMicro.ToolTip = "Cliquer pour arrêter";
                _microPulse.Begin();

                TxtStatut.Text = "● Écoute en cours...";
                TxtStatut.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#CC0000"));

                _micro.RecognizeAsync(RecognizeMode.Single);
            }
            catch { ArreterMicro(); }
        }

        private void ArreterMicro()
        {
            try { _micro?.RecognizeAsyncStop(); } catch { }

            _microActif = false;
            _microPulse.Stop();

            Dispatcher.InvokeAsync(() =>
            {
                BoutonMicro.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#222222"));
                IconeMicro.Text = "🎤";
                BoutonMicro.Opacity = 1.0;
                BoutonMicro.ToolTip = "Appuyer pour parler";

                TxtStatut.Text = "● En ligne";
                TxtStatut.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#4CAF50"));
            });
        }

        // ── Parole reconnue → met le texte dans la zone de saisie ─────────
        private void OnParoleReconnue(object sender, SpeechRecognizedEventArgs e)
        {
            string texte = e.Result.Text;

            Dispatcher.InvokeAsync(() =>
            {
                InputMessage.Text = texte;
                InputMessage.CaretIndex = texte.Length;
                ArreterMicro();

                // Envoyer automatiquement si confiance > 80%
                if (e.Result.Confidence > 0.80)
                    EnvoyerMessage();
            });
        }

        private void OnParoleRejetee(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                ArreterMicro();
                AjouterMessageIA("Je n'ai pas compris. Pouvez-vous répéter ou écrire votre message ? 🎤");
            });
        }

        private void OnReconnaissanceTerminee(object sender, RecognizeCompletedEventArgs e)
        {
            Dispatcher.InvokeAsync(() => ArreterMicro());
        }

        // ════════════════════════════════════════════════════════════
        //  ENVOI MESSAGE
        // ════════════════════════════════════════════════════════════
        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !e.KeyboardDevice.IsKeyDown(Key.LeftShift))
            {
                e.Handled = true;
                EnvoyerMessage();
            }
        }

        private void BtnEnvoyer_Click(object sender, MouseButtonEventArgs e)
        {
            EnvoyerMessage();
        }

        private async void EnvoyerMessage()
        {
            string texte = InputMessage.Text.Trim();
            if (string.IsNullOrEmpty(texte)) return;

            AjouterMessageUtilisateur(texte);
            InputMessage.Text = "";

            Border indicateur = AjouterIndicateurEcriture();
            string reponse = await AppelerIA(texte);

            PanneauMessages.Children.Remove(indicateur);
            AjouterMessageIA(reponse);

            string motCle = ExtraireMotCleRecette(reponse);
            if (!string.IsNullOrEmpty(motCle) && RechercheRecette != null)
                RechercheRecette(motCle);
        }

        // ════════════════════════════════════════════════════════════
        //  API IA
        // ════════════════════════════════════════════════════════════
        private async Task<string> AppelerIA(string messageUtilisateur)
        {
            try
            {
                TxtStatut.Text = "● En train d'écrire...";
                TxtStatut.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FFA500"));

                _historique.Add(new { role = "user", content = messageUtilisateur });

                var corps = new
                {
                    model = MODELE,
                    messages = BuildMessages(),
                    max_tokens = 400,
                    temperature = 0.7
                };

                string json = JsonSerializer.Serialize(corps);
                var contenu = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage rep = await _client.PostAsync(API_URL, contenu);
                string repJson = await rep.Content.ReadAsStringAsync();

                using JsonDocument doc = JsonDocument.Parse(repJson);
                string texteIA = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                _historique.Add(new { role = "assistant", content = texteIA });

                TxtStatut.Text = "● En ligne";
                TxtStatut.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#4CAF50"));

                return texteIA;
            }
            catch
            {
                TxtStatut.Text = "● En ligne";
                TxtStatut.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#4CAF50"));
                return "Désolé, je n'arrive pas à me connecter. Vérifiez votre connexion.";
            }
        }

        private object[] BuildMessages()
        {
            var messages = new List<object>();
            messages.Add(new
            {
                role = "system",
                content = "Tu es un assistant culinaire expert intégré dans l'application Yum! Gourmet Studio. " +
                          "Ton rôle est d'aider les utilisateurs à trouver des recettes. " +
                          "Tu DOIS TOUJOURS inclure un tag [RECETTE:...] dans CHAQUE réponse, sans exception. " +
                          "CUISINES DISPONIBLES dans notre base (UNIQUEMENT ces origines existent) : " +
                          "American, British, Canadian, Chinese, Croatian, Dutch, Egyptian, Filipino, " +
                          "French, Greek, Indian, Irish, Italian, Jamaican, Japanese, Kenyan, Malaysian, " +
                          "Mexican, Moroccan, Polish, Portuguese, Russian, Spanish, Thai, Tunisian, " +
                          "Turkish, Ukrainian, Vietnamese. " +
                          "RÈGLES STRICTES : " +
                          "1) Si l'utilisateur demande une cuisine DISPONIBLE → tag [RECETTE:NomEnAnglais] " +
                          "   Exemples: marocain→[RECETTE:Moroccan], japonais→[RECETTE:Japanese], " +
                          "   italien→[RECETTE:Italian], indien→[RECETTE:Indian], français→[RECETTE:French], " +
                          "   chinois→[RECETTE:Chinese], mexicain→[RECETTE:Mexican], grec→[RECETTE:Greek], " +
                          "   américain→[RECETTE:American], thaï→[RECETTE:Thai], tunisien→[RECETTE:Tunisian] " +
                          "2) Si l'utilisateur demande une cuisine NON DISPONIBLE (andalou, algérien, libanais, persan, etc.) " +
                          "   → NE PAS mettre de tag [RECETTE:...], et lui dire gentiment que cette cuisine " +
                          "   n'est pas disponible dans notre application, et lui proposer une cuisine similaire disponible. " +
                          "   Exemple: andalou → dire que ce n'est pas disponible, proposer Spanish ou Moroccan à la place. " +
                          "3) Si l'utilisateur demande un ingrédient/plat → tag en anglais : " +
                          "   poulet→[RECETTE:chicken], boeuf→[RECETTE:beef], pâtes→[RECETTE:pasta], " +
                          "   poisson→[RECETTE:fish], agneau→[RECETTE:lamb], chocolat→[RECETTE:chocolate] " +
                          "Réponds toujours en français, de façon chaleureuse. Sois concis (max 3-4 phrases)."
            });
            messages.AddRange(_historique);
            return messages.ToArray();
        }

        private string ExtraireMotCleRecette(string reponse)
        {
            int debut = reponse.IndexOf("[RECETTE:");
            if (debut == -1) return null;
            int fin = reponse.IndexOf("]", debut);
            if (fin == -1) return null;
            return reponse.Substring(debut + 9, fin - debut - 9).Trim();
        }

        // ════════════════════════════════════════════════════════════
        //  AFFICHAGE MESSAGES
        // ════════════════════════════════════════════════════════════
        private void AjouterMessageUtilisateur(string texte)
        {
            Border bulle = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B35")),
                CornerRadius = new CornerRadius(18, 18, 4, 18),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(60, 0, 0, 12),
                HorizontalAlignment = HorizontalAlignment.Right,
                MaxWidth = 280
            };
            bulle.Child = new TextBlock
            {
                Text = texte,
                Foreground = Brushes.White,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap
            };
            PanneauMessages.Children.Add(bulle);
            ScrollToBottom();
        }

        private void AjouterMessageIA(string texte)
        {
            string textePropre = Regex.Replace(texte, @"\[RECETTE:[^\]]*\]", "").Trim();

            Grid ligne = new Grid { Margin = new Thickness(0, 0, 60, 12) };
            ligne.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            ligne.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border avatar = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B35")),
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(16),
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 8, 0)
            };
            avatar.Child = new TextBlock
            {
                Text = "🤖",
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(avatar, 0);

            Border bulle = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")),
                CornerRadius = new CornerRadius(18, 18, 18, 4),
                Padding = new Thickness(14, 10, 14, 10),
                MaxWidth = 280
            };
            bulle.Child = new TextBlock
            {
                Text = textePropre,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEEEEE")),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            };
            Grid.SetColumn(bulle, 1);

            ligne.Children.Add(avatar);
            ligne.Children.Add(bulle);
            PanneauMessages.Children.Add(ligne);
            ScrollToBottom();
        }

        private Border AjouterIndicateurEcriture()
        {
            Border bulle = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")),
                CornerRadius = new CornerRadius(18, 18, 18, 4),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(40, 0, 60, 12),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            bulle.Child = new TextBlock
            {
                Text = "⏳ En train d'écrire...",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888888")),
                FontSize = 12,
                FontStyle = FontStyles.Italic
            };
            PanneauMessages.Children.Add(bulle);
            ScrollToBottom();
            return bulle;
        }

        private void ScrollToBottom()
        {
            Dispatcher.InvokeAsync(() => ScrollMessages.ScrollToEnd(),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void BtnFermer_Click(object sender, MouseButtonEventArgs e)
        {
            ArreterMicro();
            if (Fermer != null) Fermer();
        }
    }
}
